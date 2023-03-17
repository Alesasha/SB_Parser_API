using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SB_Parser_API.Models;
using static SB_Parser_API.Program;
using System.Linq;
using System.Net;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Data.Entity.Migrations;
using static SB_Parser_API.MicroServices.Utils;
using static SB_Parser_API.MicroServices.WebAccessUtils;
using static SB_Parser_API.MicroServices.DBSerices;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using RandomUserAgent;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Threading.Tasks;

namespace SB_Parser_API.MicroServices
{
    public class SB_Parsers
    {
        public record class groupedStores(int retailer_id, int maxProductCount, int storesCount);
        public static Stopwatch? SP_Timer = null;
        public static void CollectProducts()
        {
            /*
            var cats = SB_API.categoryGet(245) ?? new List<Category_SB_V2>();
            int productsCount = 0;
            List<string> taxonList = new();
            Action<Category_SB_V2> taxonCount=null!;
            taxonCount = (Category_SB_V2 curCat) => {
                if (curCat.children is null)
                {
                    if (taxonList.Where(x => x.Contains(curCat.id.ToString())).Count() <= 0)
                    {
                        productsCount += curCat.products_count;
                        taxonList.Add($"tid={curCat.id} ({curCat.name}/{curCat.depth}/)__Count=[{curCat.products_count}]___cType='{curCat.type}'");
                    }
                }
                else
                    foreach (var cat in curCat.children)
                        taxonCount(cat);
            };
            cats.ForEach(x => taxonCount(x));
            */

            SP_Timer = new();
            SP_Timer.Start();
            var storesList = StoresGet().Where(x => x.last_product_count > 0 && x.active == true && x.city_id == 28).ToList();
            var groupStores = storesList.GroupBy(p => p.retailer_id).Select(g => new groupedStores(g.Key, 
            (int)g.Max(x => x.last_product_count)!, (int)g.Count())).OrderByDescending(x => x.maxProductCount).ToList();

            for (int reti = 0; reti < groupStores.Count; reti++)
            {
                var gsi = groupStores[reti];
                var curStore = storesList.FirstOrDefault(x => x.retailer_id == gsi.retailer_id && x.last_product_count >= gsi.maxProductCount) ?? new();
                CollectStoreProducts(curStore);
            }
        }
        public class ProductPageScanTaskInfo
        {
            public Task? task = null;
            public int taskNumber;
            public int store;
            public int retailer;
            public int page;
            public int total_pages=0;
            public string cat = "";
            public List<Product_SB_V2>? product;
            public List<Price_SB_V2>? price;
            public Meta_Products_SB_V2? meta;
            public CMD command = CMD.StandBy;
            public AutoResetEvent checkCommand = new(false);
            public bool isJobDone = false;
        }
        public static void ProductPageScanTaskExecutor(ProductPageScanTaskInfo IO_Block)
        {
            while (true)
            {
                try
                {
                    IO_Block.checkCommand.WaitOne(20000);
                    if (IO_Block.command == CMD.StandBy)
                        continue;

                    if (IO_Block.command == CMD.Exit)
                    {
                        IO_Block.command = CMD.Exited;
                        return;
                    }
                    
                    if (IO_Block.command == CMD.ExecuteRequest)
                    {
                        IO_Block.isJobDone = false;
                        (IO_Block.product, IO_Block.price, IO_Block.meta) = SB_API.productSearch("", IO_Block.cat, IO_Block.store, Priority.MediumLow, IO_Block.page, SB_API.productsPerPage);
                        IO_Block.product?.ForEach(x => { x.store = IO_Block.store; x.retailer = IO_Block.retailer; x.eans = BarCodeCheckList(x.eans); });
                        IO_Block.total_pages = IO_Block.meta?.total_pages ?? IO_Block.total_pages;
                        var maxPages = SB_API.maxProductsPerRequest / SB_API.productsPerPage;
                        IO_Block.total_pages = IO_Block.total_pages > maxPages ? maxPages : IO_Block.total_pages;

                        IO_Block.command = CMD.StandBy;
                        //IO_Block.command = CMD.WriteDB;
                        //IO_Block.checkCommand.Set();
                        IO_Block.isJobDone = true;
                        continue;
                    }
                    if (IO_Block.command == CMD.WriteDB)
                    {
                        if (IO_Block.product is null)
                        {
                            IO_Block.command = CMD.StandBy;
                            continue;
                        }
                        AddOrUpdateProductsBatch(IO_Block.product,true);
                        IO_Block.product = null;
                        IO_Block.command = CMD.StandBy;
                        IO_Block!.isJobDone = true;
                        continue;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\n\n\nProductPageScanTaskExecutor[t={IO_Block.taskNumber}]{e.Message}\n\n\n");
                    IO_Block.isJobDone = false;
                    IO_Block.checkCommand.Set();
                }
            }
        }
        record CTS(string id, int count);
        public static ProductPageScanTaskInfo? dbTask = null;
        public static int prodCount=0;

        public static void CollectStoreProducts(Store curStore)
        {
            var store_id = curStore.id;
            List<Category_SB_V2> curStoreCat;
            List<CTS> catsToScan = new();
            if (curStore.last_product_count > 10000)
            {
                //return;
                curStoreCat = SB_API.categoryGet(curStore.id)?.Where(x => x.id != 7107 && x.id != 57272 && x.id != 80857 && x.id != 82195).ToList() ?? new();
                curStoreCat.ForEach(x => catsToScan.Add(new (x.id.ToString(),x.products_count)));
            }
            else
                catsToScan.Add(new("All", curStore.last_product_count ?? 0));

            const int maxScanThreads = 40;
            List<ProductPageScanTaskInfo> tasksList = new();
            List<Product_SB_V2> productBuff = new();
            if (dbTask is not null)
                tasksList.Add(dbTask);

            dbTask = null;

            var takeThread = () => 
            {
                if (tasksList is null)
                    tasksList = new();
                ProductPageScanTaskInfo? taskInfo;
                while ((taskInfo = tasksList.FirstOrDefault(x => x.isJobDone)) is null)
                {
                    if (tasksList.Count < maxScanThreads)
                    {
                        taskInfo = new ProductPageScanTaskInfo();
                        taskInfo.task = Task.Factory.StartNew(() => ProductPageScanTaskExecutor(taskInfo), TaskCreationOptions.LongRunning);
                        lock (tasksList)
                        {
                            taskInfo.taskNumber = getIDforNewRecord(tasksList.Select(x => x.taskNumber).ToList());
                            tasksList.Add(taskInfo);
                        }
                        break;
                    }
                    else
                    {
                        if (reqQC!.IsCompleted || reqQC.IsFaulted || reqQC.IsCanceled)
                            reqQC = Task.Factory.StartNew(() => RequestQueueController(), TaskCreationOptions.LongRunning);
                        Task.Delay(1000).Wait();
                    }
                }
                //Console.WriteLine($"store={taskInfo.store},cat={taskInfo.cat},page=>{taskInfo.page} of {taskInfo.total_pages},product.count=>{taskInfo.product?.Count ?? 0}");
                if (taskInfo.isJobDone)
                {
                    if (taskInfo.product is not null && taskInfo.product.Count > 0)
                    {
                        productBuff = productBuff.Union(taskInfo.product).Distinct(new Product_SB_V2_IdComparer()).ToList();
                        var tp = prodCount + productBuff.Count;
                        var pPerH = TimeSpan.FromHours(1) * tp / SP_Timer!.Elapsed;
                        Console.WriteLine($"store={taskInfo.store},cat={taskInfo.cat},page=>{taskInfo.page} of {taskInfo.total_pages},ProductsInShop={productBuff.Count},TotalProducts={tp},Speed={(int)pPerH:d4}_pph,FromStart:{SP_Timer.Elapsed:hh\\:mm\\:ss}"); //:mm\\:ss\\"); product.count=>{taskInfo.product?.Count ?? 0},
                        taskInfo.product = null;
                    }
                }
                return taskInfo;
            };

            foreach (var cat in catsToScan)
            {
                var pagesFromCount = (int count, int perPage) => {
                    var pages = count / perPage + (count % perPage == 0 ? 0 : 1);
                    var maxPages = SB_API.maxProductsPerRequest / SB_API.productsPerPage;
                    return pages > maxPages ? maxPages : pages;
                };
                for (int page = 1, totPages = pagesFromCount(cat.count, SB_API.productsPerPage); page <= totPages; page++)
                {
                    var ti = takeThread();
                    if (ti.cat == cat.id)
                        totPages = ti.total_pages;
                    ti.total_pages = totPages;
                    ti.store = store_id;
                    ti.retailer = curStore.retailer_id;
                    ti.cat = cat.id == "All" ? "" : cat.id;
                    ti.page = page;
                    ti.product = null;
                    ti.meta = null;
                    ti.price = null;
                    ti.isJobDone = false;
                    ti.command = CMD.ExecuteRequest;
                    ti.checkCommand.Set();
                }
            }

            while (tasksList.FirstOrDefault(x => !x.isJobDone) is not null)
            {
                if (reqQC!.IsCompleted || reqQC.IsFaulted || reqQC.IsCanceled)
                    reqQC = Task.Factory.StartNew(() => RequestQueueController(), TaskCreationOptions.LongRunning);
                Task.Delay(3000).Wait();
            }
            Console.WriteLine($"Scan of store={curStore.id} has done,TotalProducts={productBuff.Count}");
            prodCount += productBuff.Count;
            var tiw = takeThread();
            tiw.total_pages = 0;
            tiw.store = store_id;
            tiw.retailer = curStore.retailer_id;
            tiw.cat = "";
            tiw.page = 0;
            tiw.product = productBuff;
            tiw.meta = null;
            tiw.price = null;
            tiw.isJobDone = false;
            tiw.command = CMD.WriteDB;
            tiw.checkCommand.Set();
            dbTask = tiw;

            //while (tasksList.FirstOrDefault(x => !x.isJobDone) is not null)
              //  Task.Delay(3000).Wait();
            //Console.WriteLine($"Save all info of store={curStore.id} has done,TotalProducts={productBuff.Count}");

            /*
                        //text=SB_API.productSearch("",26399,1,24);
                        var cats = SB_API.categoryGet(store_id);
                        var tot = cats?.Where(x => x.id != 7107 && x.id != 57272).Sum(x => x.products_count);


                        var cpage = 1;
                        var (products, prices, meta) = SB_API.productSearch("", "", store_id, Priority.MediumLow, cpage);
                        if (products?.Count > 0)
                            foreach (var p in products)
                            {
                                p.store = store_id;
                                p.retailer = GetRetailerId(store_id);
                                if (p.images?.Count > 0)
                                    foreach (var i in p.images)
                                        i.product_id = p.id;
                            }
                        AddOrUpdateProductsBatch(products!);
            */
        }
        public static int GetProductCount(int store_id)
        {
            var (products, prices, meta) = SB_API.productSearch("", "", store_id);
            var total_products= meta?.total_count ?? 0;
            if (total_products > 9999)
            {
                var cats = SB_API.categoryGet(store_id);
                total_products = cats?.Where(x => x.id != 7107 && x.id != 57272 && x.id != 80857).Sum(x => x.products_count) ?? 0;
            }
            return total_products;
        }

        public static async Task CollectShops(string proxyURL, int smin, int smax, int numberOfTasks=30)
        {
            /*
            HttpResponseMessage? response = null;
            HttpRequestMessage? request = null;
            var handler = new HttpClientHandler()
            {
                Proxy = new WebProxy(new Uri(proxyURL)), //$"HTTP://185.15.172.212:3128"
                UseProxy = true
            };

            HttpClient client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromMilliseconds(40000);
            */
            //var db = new PISBContext();
            //int imax = GetMaxStoreNumber() ?? 0;

            int emptyLinks = 0;
            object el = new();
            var Timer = new Stopwatch();

            var store_batch = new List<Store>();
            var saveStores = () =>
            {
                while (true)
                    if (AddOrUpdateStoresBatch(store_batch))
                    {
                        store_batch.Clear();
                        break;
                    }
            };
            /*
            var getWebText = (string url) =>
            {
                string txt;
                while (true)
                {
                    try
                    {
                        request = new HttpRequestMessage()
                        {
                            RequestUri = new Uri(url),
                            Method = HttpMethod.Get,
                        };
                        var respTask = client.SendAsync(request);
                        respTask.Wait();
                        response = respTask.Result;
                        IEnumerable<string> cookies = response.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;
                        txt = response.Content.ReadAsStringAsync().Result;
                        break;
                    }
                    catch (Exception e) { Console.WriteLine($"Call Point 001: {e.Message}"); request?.Dispose(); Task.Delay(3000).Wait(); continue; }
                }
                return txt;
            };
            */
            var getFullStoreInfo = (int st) =>
            {
                Store? sto = SB_API.storeInfoGet(st);
                if (sto is null)
                {
                    Console.WriteLine($"emptyLink:  {st}  emptyLinksInArow = {emptyLinks}");
                    lock (el)
                        emptyLinks++;
                    return;
                }
                if (sto is not null)
                {
                    var lpc = GetProductCount(sto.id);
                    sto.last_product_count = lpc;//GetProductCount(sto.id);
                    sto.lpc_dt = DateTime.Now;
                    lock(store_batch)
                        store_batch.Add(sto);
                }
                var sPerH = TimeSpan.FromHours(1) * (st - smin) / Timer.Elapsed;
                Console.WriteLine($"{sto!.id}.{sto.name} - {sto.city}, lpc={sto.last_product_count}, Speed={(int)sPerH:d4}_sph,FromStart:{Timer.Elapsed:hh\\:mm\\:ss}"); //:mm\\:ss\\

                lock (el)
                    emptyLinks = 0;
            };

            var taskList = new List<Task>();
            Timer.Start();
            for (var i = smin; emptyLinks < 250 && i < smax; i++)
            {
                while (taskList.Count() >= numberOfTasks)
                {
                    taskList.RemoveAll(x => x.IsCompleted);
                    await Task.Delay(300);
                }
                lock (store_batch)
                    if (store_batch.Count > 10)
                        saveStores();
                var stn = i;
                taskList.Add(Task.Factory.StartNew(() => getFullStoreInfo(stn), TaskCreationOptions.LongRunning));


                await Task.Delay(100);
                var temp = """
                Store? sto = SB_API.storeInfoGet(i);
                if (sto is null)
                {
                    Console.WriteLine($"emptyLink:  {i}");
                    emptyLinks++;
                    continue;
                }
                /*
                string rUrl = $"https://sbermarket.ru/api/stores/{i}";
                var text = getWebText(rUrl);

                if (text.Contains("error"))
                {
                    Console.WriteLine($"emptyLink:  {i}");
                    emptyLinks++;
                    continue;
                }
                try
                {
                    sto = JObject.Parse(text).SelectToken("$.store")?.ToObject<Store>();
                }
                catch (Exception e)
                {
                    Console.WriteLine(text); Console.WriteLine(e.Message);
                }
                */



                //using (var db = new PISBContext())
                {
                    //var ob = db.Stores.FirstOrDefault(x => x.id == sto!.id);
                    //var stores = db.Stores.ToList();
                    // добавляем их в бд

                    //var ob = stores.FirstOrDefault(x => x.id == sto!.id)!;

                    //if (ob is not null) db.Stores.Remove(ob);
                    if (sto is not null)
                    {
                        //var txt = getWebText($"https://sbermarket.ru/api/v2/products?q=&sid={sto.id}&page=1&per_page=1");
                        //var meta = JObject.Parse(txt!).SelectToken("$.meta")?.ToObject<Meta_Products_SB_V2>();//JsonConvert.DeserializeObject<List<Category>>(txt);
                        //(_, _, var meta) = SB_API.productSearch("", sto.id);

                        /*var lpc = meta?.total_count ?? 0;
                        if (lpc > 9999)
                        {
                            //txt = getWebText($"https://sbermarket.ru/api/v2/categories?depth=1&reset_cache=true&sid={sto.id}");
                            //var cats = JObject.Parse(txt!).SelectToken("$.categories")?.ToObject<List<Category_SB_V2>>();//JsonConvert.DeserializeObject<List<Category>>(txt);
                            var cats = SB_API.categoryGet(sto.id);
                            lpc = cats?.Where(x => x.id != 7107 && x.id != 57272).Sum(x => x.products_count) ?? 0;
                        }
                        */
                        var lpc = GetProductCount(sto.id);
                        sto.last_product_count = lpc;//GetProductCount(sto.id);
                        sto.lpc_dt = DateTime.Now;
                        store_batch.Add(sto);
                    }

                    //db.Stores.Add(sto!);
                    if (i % 10 == 0)
                        saveStores();

                    Console.WriteLine($"{sto!.id}.{sto.name} - {sto.city}, lpc={sto.last_product_count}");
                    //Console.Clear();
                    //Console.WriteLine("Объекты успешно сохранены");

                    // получаем объекты из бд и выводим на консоль
                    //stores = db.Stores.OrderBy(x => x.id).ToList();
                    //Console.WriteLine("Список объектов:");
                    //foreach (var s in stores)
                    //{
                    //   Console.WriteLine($"{s.id}.{s.name} - {s.city}");
                    //}
                }
                emptyLinks = 0;
                await Task.Delay(300); 
                """;
            }
            while (taskList.Count() > 0)
            {
                taskList.RemoveAll(x => x.IsCompleted);
                await Task.Delay(300);
            }
            saveStores();
        }
        public static void CollectRetailers() //async Task
        {

            var text = SB_API.Retailers();//response.Content.ReadAsStringAsync().Result;
            var ret = JObject.Parse(text!).SelectToken("$.retailers")?.ToObject<List<Retailer>>();

#pragma warning disable CA1416 // Проверка совместимости платформы
            Console.BufferHeight = 10000;
#pragma warning restore CA1416 // Проверка совместимости платформы

            using var db = new PISBContext();
            //db.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
            //var sql = ((dynamic)flooringStoresProducts).Sql;
            //db.Database?.Log = Console.Write;

            var rets = db.Retailers.ToList();
            //var et=db.Model.GetEntityTypes();
            // добавляем их в бд

            foreach (var r in ret!)
            {
                var ob = rets.FirstOrDefault(x => x.id == r.id)!;
                //int ind = rets.FindIndex(x => x.id == r.id);
                if (ob != null) db.Retailers.Remove(ob);
                db.Retailers.Add(r); //ob = r; AddOrUpdate
                                     //db.Entry(r).State = Microsoft.EntityFrameworkCore.EntityState.Modified; //System.Data.Entity.EntityState.Modified;
            }

            db.SaveChanges();

            Console.WriteLine("Объекты успешно сохранены");

            // получаем объекты из бд и выводим на консоль
            rets = db.Retailers.OrderBy(x => x.id).ToList();
            Console.WriteLine("Список объектов:");
            foreach (var r in rets)
            {
                Console.WriteLine($"{r.id}.{r.name} - {r.slug}");
            }
        }
    }
}
