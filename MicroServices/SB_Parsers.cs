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
using static SB_Parser_API.MicroServices.SB_API;
using static SB_Parser_API.MicroServices.ZC_API;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using RandomUserAgent;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Elastic.Clients.Elasticsearch.Tasks;
using System.Threading;

namespace SB_Parser_API.MicroServices
{
    public class SB_Parsers
    {
        public record class groupedStores(int retailer_id, int maxProductCount, int storesCount);
        public static Stopwatch? SP_Timer = null;
        public const int productUpdatePeriod = 5; // hours
        public const int productPropertyUpdateThreads = 40; // pcs
        public const int maxScanThreads = 40;
        public static List<ProductPageScanTaskInfo> pageScanTasksList = null!;
        public static List<StoreProductsToDB_TaskInfo> storeProductsToDB_TasksList = null!;
        public static Task? WriteDB_Task = null;
        public static List<Product_SB_V2>? product_to_db_list = new();

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
            Init_AddOrUpdateProductsBatch();
            SP_Timer = new();
            SP_Timer.Start();
            var storesList = MosRegStoresGet();
            var groupStores = storesList.GroupBy(p => p.retailer_id).Select(g => new groupedStores(g.Key, 
            (int)g.Max(x => x.last_product_count)!, (int)g.Count())).OrderByDescending(x => x.maxProductCount).ToList();

            pageScanTasksList = new();
            storeProductsToDB_TasksList = new();

            product_to_db_list = new();

            for (int reti = 0; reti < groupStores.Count; reti++)
            {
                var gsi = groupStores[reti];
                var curStore = storesList.FirstOrDefault(x => x.retailer_id == gsi.retailer_id && x.last_product_count >= gsi.maxProductCount) ?? new();

                lock(storeProductsToDB_TasksList)
                    storeProductsToDB_TasksList.Add(new StoreProductsToDB_TaskInfo() { store = curStore.id, retailer = curStore.retailer_id, product = new() });

                CollectStoreProducts(curStore);
                if(WriteDB_Task is null || WriteDB_Task.IsCompleted || WriteDB_Task.IsFaulted || WriteDB_Task.IsCanceled)
                    WriteDB_Task= Task.Factory.StartNew(() => WriteStoreProdutsToDB(), TaskCreationOptions.LongRunning);
            }
            
            while (pageScanTasksList.Count > 0)
            {
                ProductPageScanTaskInfo? taskInfo;
                while ((taskInfo = pageScanTasksList.FirstOrDefault(x => x.isJobDone)) is null)
                {
                    if (reqQC!.IsCompleted || reqQC.IsFaulted || reqQC.IsCanceled)
                        reqQC = Task.Factory.StartNew(() => RequestQueueController(), TaskCreationOptions.LongRunning);
                    Task.Delay(1000).Wait();
                }
                ProcessPageScanResult(taskInfo);
                taskInfo.command = CMD.Exit;
                taskInfo.checkCommand.Set();
                pageScanTasksList.Remove(taskInfo);
            }

            lock(storeProductsToDB_TasksList)
                storeProductsToDB_TasksList.ForEach(x => { x.isAllPageRequestStarted = true;x.isAllPageRequestDone = true; });

            while (storeProductsToDB_TasksList.Count > 0)
            {
                if (WriteDB_Task is null || WriteDB_Task.IsCompleted || WriteDB_Task.IsFaulted || WriteDB_Task.IsCanceled)
                    WriteDB_Task = Task.Factory.StartNew(() => WriteStoreProdutsToDB(), TaskCreationOptions.LongRunning);

                var pPerH = TimeSpan.FromHours(1) * prodCount / SP_Timer!.Elapsed;
                Console.WriteLine($"TotalStores={groupStores.Count},StoresToSave={storeProductsToDB_TasksList.Count},TotalProducts={prodCount},Speed={(int)pPerH:d4}_pph,FromStart:{SP_Timer.Elapsed:hh\\:mm\\:ss}"); //:mm\\:ss\\"); product.count=>{taskInfo.product?.Count ?? 0},
                Task.Delay (15000).Wait();
            }
        }

        public static void WriteStoreProdutsToDB()
        {
            while (storeProductsToDB_TasksList.Count > 0)
            {
                StoreProductsToDB_TaskInfo? wdbt;
                while ((wdbt=storeProductsToDB_TasksList.FirstOrDefault(x => x.isAllPageRequestDone)) is null)
                {
                    if (reqQC!.IsCompleted || reqQC.IsFaulted || reqQC.IsCanceled)
                        reqQC = Task.Factory.StartNew(() => RequestQueueController(), TaskCreationOptions.LongRunning);
                    Task.Delay(3000).Wait();
                }
                (product_to_db_list ??= new()).AddRange(wdbt.product);
                if (product_to_db_list.Count > 10000 || storeProductsToDB_TasksList.Count < 2)
                {
                    AddOrUpdateProductsBatch(product_to_db_list);
                    product_to_db_list.Clear();
                }
                lock (storeProductsToDB_TasksList)
                    storeProductsToDB_TasksList.Remove(wdbt);
            }
        }
        public class ProductPageScanTaskInfo
        {
            public Task? task = null;
            public int taskNumber;
            public int store;
            public int retailer;
            public int page;
            public SortProductSB sort_order = SortProductSB.price_asc;
            public int total_pages=0;
            public int total_count=0;
            public string cat = "";
            public List<Product_SB_V2>? product;
            public List<Price_SB_V2>? price;
            public Meta_Products_SB_V2? meta;
            public CMD command = CMD.StandBy;
            public AutoResetEvent checkCommand = new(false);
            public bool isJobDone = false;
        }
        public class StoreProductsToDB_TaskInfo
        {
            //public Task? task = null;
            //public int taskNumber;
            public int store;
            public int retailer;
            public List<Product_SB_V2> product = new();
            public bool isAllPageRequestStarted = false;
            public bool isAllPageRequestDone = false;
            //public CMD command = CMD.StandBy;
            //public AutoResetEvent checkCommand = new(false);
            //public bool isJobDone = false;
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
                        (IO_Block.product, IO_Block.price, IO_Block.meta) = SB_API.productSearch("", IO_Block.cat, IO_Block.store, Priority.MediumLow, IO_Block.page, SB_API.productsPerPage, IO_Block.sort_order);
                        IO_Block.product?.ForEach(x => { x.store = IO_Block.store; x.retailer = IO_Block.retailer; x.eans = BarCodeCheckList(x.eans); });
                        IO_Block.total_pages = IO_Block.meta?.total_pages ?? IO_Block.total_pages;
                        IO_Block.total_count = IO_Block.meta?.total_count ?? IO_Block.total_count;

                        var maxPages = SB_API.maxProductsPerRequest / SB_API.productsPerPage;
                        IO_Block.total_pages = IO_Block.total_pages > maxPages ? maxPages : IO_Block.total_pages;

                        IO_Block.command = CMD.StandBy;
                        //IO_Block.command = CMD.WriteDB;
                        //IO_Block.checkCommand.Set();
                        IO_Block.isJobDone = true;
                        continue;
                    }
                    /*if (IO_Block.command == CMD.WriteDB)
                    {
                        if (IO_Block.product is null)
                        {
                            IO_Block.command = CMD.StandBy;
                            continue;
                        }
                        AddOrUpdateProductsBatch(IO_Block.product);
                        IO_Block.product = null;
                        IO_Block.command = CMD.StandBy;
                        IO_Block!.isJobDone = true;
                        continue;
                    }
                    */
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\n\n\nProductPageScanTaskExecutor[t={IO_Block.taskNumber}]{e.Message}\n\n\n");
                    IO_Block.isJobDone = false;
                    IO_Block.checkCommand.Set();
                }
            }
        }
        public record CTS(string id, int count);
        //public static ProductPageScanTaskInfo? dbTask = null;
        public static int prodCount=0;

        public static void ProcessPageScanResult(ProductPageScanTaskInfo? taskInfo)
        {
            if (taskInfo?.product is not null && taskInfo.product.Count > 0)
            {
                StoreProductsToDB_TaskInfo? spt;
                lock (storeProductsToDB_TasksList)
                    spt = storeProductsToDB_TasksList.FirstOrDefault(x => x.store == taskInfo.store);
                if (spt is null)
                {
                    spt = new() { store = taskInfo.store, retailer = taskInfo.retailer, product = new() };
                    lock (storeProductsToDB_TasksList)
                        storeProductsToDB_TasksList.Add(spt);
                }
                spt.product = spt.product.Union(taskInfo.product).Distinct(new Product_SB_V2_IdComparer()).ToList();

                //var tp = prodCount + spt.product.Count;
                int tp = 0;
                int stc = 0;
                lock (storeProductsToDB_TasksList)
                    storeProductsToDB_TasksList.Where(x => x.isAllPageRequestStarted && !x.isAllPageRequestDone).ToList().ForEach(s => { //var storeOnScan = 
                if ((stc=pageScanTasksList.Where(x => (x.store == s.store)).Count()) <= 0) // && !x.isJobDone
                {
                    Console.WriteLine($"Scan completed of store_id={s.store}, stc={stc}, productCount={s.product.Count()}");
                    //Console.ReadKey();
                    s.isAllPageRequestDone = true;
                    prodCount += s.product.Count;
                }
            });
                lock (storeProductsToDB_TasksList)
                    storeProductsToDB_TasksList.Where(x => !x.isAllPageRequestDone).ToList().ForEach(s => { tp+= s.product.Count;});
                tp += prodCount;

                var pPerH = TimeSpan.FromHours(1) * tp / SP_Timer!.Elapsed;
                Console.WriteLine($"store={taskInfo.store},cat={taskInfo.cat},page=>{taskInfo.page} of {taskInfo.total_pages},ProductsInShop={spt.product.Count},TotalProducts={tp},Speed={(int)pPerH:d4}_pph,FromStart:{SP_Timer.Elapsed:hh\\:mm\\:ss}"); //:mm\\:ss\\"); product.count=>{taskInfo.product?.Count ?? 0},

                //taskInfo.store = 0;
                //taskInfo.product = null;
            }
        }
        public static void CollectCats(List<CTS> catsToScan, List<Category_SB_V2> catL)
        {
            foreach (var cat in catL)
            {
                if (cat.products_count > SB_API.maxProductsPerRequest && (cat.children?.Count ?? 0) > 0)
                    CollectCats(catsToScan, cat.children!);
                else
                    catsToScan.Add(new(cat.id.ToString(), cat.products_count));
            }
        }
        public static void CollectStoreProducts(Store curStore)
        {
            var store_id = curStore.id;
            List<Category_SB_V2> curStoreCat;
            List<CTS> catsToScan = new();
            if (curStore.last_product_count > SB_API.maxProductsPerRequest)
            {
                //return;
                curStoreCat = SB_API.categoryGet(curStore.id)?.Where(x => x.id != 7107 && x.id != 57272 && x.id != 80857 && x.id != 82195).ToList() ?? new();
                //curStoreCat.ForEach(x => catsToScan.Add(new (x.id.ToString(),x.products_count)));

                CollectCats(catsToScan,curStoreCat);
                //var ttcurStoreCat = curStoreCat;

            }
            else
              catsToScan.Add(new("All", curStore.last_product_count ?? 0));

            //List<ProductPageScanTaskInfo> tasksList = new();
            List<Product_SB_V2> productBuff = new();
            //if (dbTask is not null)
            //    tasksList.Add(dbTask);

            //dbTask = null;

            var takeThread = () => 
            {
                if (pageScanTasksList is null)
                    pageScanTasksList = new();
                ProductPageScanTaskInfo? taskInfo;
                while (true)
                {
                    while ((taskInfo = pageScanTasksList.FirstOrDefault(x => x.isJobDone)) is null)
                    {
                        if (pageScanTasksList.Count < maxScanThreads)
                        {
                            taskInfo = new ProductPageScanTaskInfo();
                            taskInfo.task = Task.Factory.StartNew(() => ProductPageScanTaskExecutor(taskInfo), TaskCreationOptions.LongRunning);
                            lock (pageScanTasksList)
                            {
                                taskInfo.taskNumber = getIDforNewRecord(pageScanTasksList.Select(x => x.taskNumber).ToList());
                                pageScanTasksList.Add(taskInfo);
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
                            ProcessPageScanResult(taskInfo);
                            //productBuff = productBuff.Union(taskInfo.product).Distinct(new Product_SB_V2_IdComparer()).ToList();
                            //var tp = prodCount + productBuff.Count;
                            //var pPerH = TimeSpan.FromHours(1) * tp / SP_Timer!.Elapsed;
                            //Console.WriteLine($"store={taskInfo.store},cat={taskInfo.cat},page=>{taskInfo.page} of {taskInfo.total_pages},ProductsInShop={productBuff.Count},TotalProducts={tp},Speed={(int)pPerH:d4}_pph,FromStart:{SP_Timer.Elapsed:hh\\:mm\\:ss}"); //:mm\\:ss\\"); product.count=>{taskInfo.product?.Count ?? 0},
                            //taskInfo.product = null;
                        }

                        if (taskInfo.total_pages > taskInfo.page)
                            taskInfo.page++;
                        else
                        {
                            taskInfo.page = 1;
                            if (taskInfo.total_pages * SB_API.productsPerPage < taskInfo.total_count)
                            {
                                var threshold = 1;
                                if (taskInfo.total_count > SB_API.maxProductsPerRequest * 2)
                                    threshold = 3;

                                if ((int)taskInfo.sort_order < threshold)
                                    taskInfo.sort_order++;
                                else
                                    break;
                            }
                            else
                                break;
                        }
                        taskInfo.product = null;
                        taskInfo.meta = null;
                        taskInfo.price = null;
                        taskInfo.isJobDone = false;
                        taskInfo.command = CMD.ExecuteRequest;
                        taskInfo.checkCommand.Set();
                        continue;
                    }
                    break;
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
                //for (int page = 1, totPages = pagesFromCount(cat.count, SB_API.productsPerPage); page <= totPages; page++)

                    var ti = takeThread();
                    //if (ti.cat == cat.id)
                      //  totPages = ti.total_pages;
                    ti.total_pages = pagesFromCount(cat.count, SB_API.productsPerPage);
                    ti.total_count = cat.count;
                    ti.store = store_id;
                    ti.retailer = curStore.retailer_id;
                    ti.cat = cat.id == "All" ? "" : cat.id;
                    ti.page = 1;
                    ti.product = null;
                    ti.meta = null;
                    ti.price = null;
                    ti.isJobDone = false;
                    ti.command = CMD.ExecuteRequest;
                    ti.checkCommand.Set();
            }

            StoreProductsToDB_TaskInfo? cdbwt;
            lock (storeProductsToDB_TasksList)
                cdbwt = storeProductsToDB_TasksList.FirstOrDefault(x => x.store == curStore.id);
            if (cdbwt is null)
            {
                cdbwt = new() { store = curStore.id, retailer = curStore.retailer_id, product = new() };
                storeProductsToDB_TasksList.Add(cdbwt);
            }
            lock (storeProductsToDB_TasksList)
                cdbwt.isAllPageRequestStarted = true;




            /*
            while (pageScanTasksList.FirstOrDefault(x => !x.isJobDone) is not null)
            {
                if (reqQC!.IsCompleted || reqQC.IsFaulted || reqQC.IsCanceled)
                    reqQC = Task.Factory.StartNew(() => RequestQueueController(), TaskCreationOptions.LongRunning);
                Task.Delay(1000).Wait();
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
            //dbTask = tiw;
            */
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

        public static async Task CollectShops(int numberOfTasks=30)
        {
            object el = new();
            var Timer = new Stopwatch();

            var store_batch = new List<Store>();
            var store_source = new List<Store>();
            object store_source_lock = new();
            var store_DB = StoresGet();

            var saveStores = () =>
            {
                var sb = new List<Store>();
                lock (store_batch)
                {
                    sb = store_batch.ToList();
                    store_batch.Clear();
                }
                while (!AddOrUpdateStoresBatch(sb))
                        Task.Delay(1000).Wait();
            };

            object pagesCountersLock = new();
            int curPage = 1;
            int totPages = 200;
            int perPage = 500;
            int totStores = totPages * perPage;

            var collectStoreSourceList = () =>
            {
                while (curPage <= totPages)
                {
                    StoreSet_SB? curStoreBatch = null;
                    while(curStoreBatch is null)
                        curStoreBatch = storeListInfoGet(curPage, perPage, Priority.High);
                    lock (pagesCountersLock)
                    {
                        totStores = curStoreBatch.total_count;
                        totPages = totStores / perPage + (totStores % perPage > 0 ? 1 : 0);
                        curPage++;
                    }
                    lock (store_source_lock)
                    {
                        store_source.AddRange(curStoreBatch.stores ?? new());
                        store_source = store_source.Distinct().OrderBy(x => x.id).ToList();
                    }
                }
            };

            var getFullStoreInfo = (Store? sto, int stoDone, bool getFullInfo) =>
            {
                var si = sto!.id;
                if (getFullInfo)
                    sto = SB_API.storeInfoGet(si);
                if (sto is null)
                {
                    Console.WriteLine($"emptyLink:  {si}  !!!!");
                    return;
                }
                var lpc = GetProductCount(sto.id);
                sto.last_product_count = lpc;//GetProductCount(sto.id);
                sto.lpc_dt = DateTime.Now;

                lock (store_batch)
                {
                    if (store_batch.Where(x => x.id == sto.id).Count() == 0)
                        store_batch.Add(sto);
                }

                var sPerH = TimeSpan.FromHours(1) * stoDone / Timer.Elapsed;
                Console.WriteLine($"{si}.{sto.name} - {sto.city}, lpc={sto.last_product_count}, Speed={(int)sPerH:d4}_sph,FromStart:{Timer.Elapsed:hh\\:mm\\:ss}"); //:mm\\:ss\\
            };

            var taskList = new List<Task>();
            Timer.Start();

            Task SC_Task = null!;

            Store curStore = null!;
            Store? curStoreDB;

            for (var i = 0; true;)
            {
                lock (pagesCountersLock)
                    if (SC_Task is null || (curPage < totPages && (SC_Task.IsCanceled || SC_Task.IsFaulted || SC_Task.IsCompleted)))
                        SC_Task = Task.Factory.StartNew(() => collectStoreSourceList(), TaskCreationOptions.LongRunning);

                while (taskList.Count() >= numberOfTasks)
                {
                    taskList.RemoveAll(x => x.IsCompleted);
                    await Task.Delay(300);
                }

                lock (store_batch)
                    if (store_batch.Count > 10)
                        saveStores();

                bool theEnd = false;
                lock (pagesCountersLock)
                    if (i >= totStores)
                        theEnd = true;
                if (!theEnd)
                {
                    bool cont = false;
                    lock (store_source_lock)
                    {
                        if (store_source.Count > 0)
                        {
                            i++;
                            while (true)
                            {
                                if (store_source.Count <= 0)
                                {
                                    cont = true;
                                    break;
                                }
                                curStore = store_source[0];
                                store_source.RemoveAll(x => x.id == curStore.id);
                                lock (store_batch)
                                    if (store_batch.Where(x => (x?.id ?? 0) == curStore.id).Count() <= 0)
                                        break;
                            }
                        }
                        else
                            cont = true;
                    }
                    if (cont)
                    {
                        await Task.Delay(300);
                        continue;
                    }

                    curStoreDB = store_DB.FirstOrDefault(x => (x?.id ?? 0) == curStore.id);
                    bool getFullInfo = true;
                    if (curStoreDB is not null)
                    {
                        getFullInfo = false;
                        curStore.phone = curStoreDB.phone;
                        curStore.city_name = curStoreDB.city_name;
                        curStore.city_slug = curStoreDB.city_slug;
                        curStore.pharmacy_license = curStoreDB.pharmacy_license;
                        curStore.last_product_count = curStoreDB.last_product_count;
                        curStore.lpc_dt = curStoreDB.lpc_dt;
                        var zid = curStore.operational_zone_id;
                        if (!curStore.active || (zid != 0 && zid != 1 && zid != 17))
                        {
                            curStore.point0 = true;
                            lock (store_batch)
                                if (store_batch.Where(x => x.id == curStore.id).Count() == 0)
                                    store_batch.Add(curStore);
                            var sPerH = TimeSpan.FromHours(1) * i / Timer.Elapsed;
                            Console.WriteLine($"Out of Moscow Region or not active:{curStore!.id}.{curStore.name} - {curStore.city}, lpc={curStore.last_product_count}, Speed={(int)sPerH:d4}_sph,FromStart:{Timer.Elapsed:hh\\:mm\\:ss}"); //:mm\\:ss\\
                            continue;
                        }
                    }
                    taskList.Add(Task.Factory.StartNew(() => getFullStoreInfo(curStore,i,getFullInfo), TaskCreationOptions.LongRunning));
                }
                else
                    break;

                await Task.Delay(100);
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
            int per_page = 500;

            var ret = new List<Retailer>();
            for (int i = 1; ; i++)
            {
                var text = SB_API.Retailers(i, per_page);//response.Content.ReadAsStringAsync().Result;
                var reti = JObject.Parse(text!).SelectToken("$.retailers")?.ToObject<List<Retailer>>();
                if ((reti?.Count ?? 0) <= 0)
                    break;
                ret.AddRange(reti!);
            }

#pragma warning disable CA1416 // Проверка совместимости платформы
            Console.BufferHeight = 20000;
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

        public record ppuDt(long id, long sku, DateTime dt);
        public static void CollectProductProperties(bool newProdutsOnly = false)
        {
            Init_AddOrUpdateProductProperties(newProdutsOnly);
            var DbWriter = Task.Factory.StartNew(() => AddOrUpdateProductProperties(), TaskCreationOptions.LongRunning);

            var skuProdPropToUpdate = sProducts!.Where(x => !newProdutsOnly || x.Value.dt_property_updated is null).Select(x => new 
            ppuDt(x.Value.id, x.Value.sku ?? 0, x.Value.dt_property_updated ?? DateTime.Parse("Jan 1, 2023"))).OrderBy(x => x.dt).ToList();

            Dictionary<AutoResetEvent, WebRequestOrder> requestPool = new();
            bool endingFlag = false;

            var addRequest = (int i) => 
            {
                var wro_tmp = productInfoSendRequest(skuProdPropToUpdate[i].id);
                requestPool[wro_tmp.completed] = wro_tmp;
            };

            SP_Timer = new();
            SP_Timer.Start();

            for (var i = 0; i < skuProdPropToUpdate.Count;)
            {
                if(DbWriter.IsCanceled || DbWriter.IsFaulted || DbWriter.IsCompleted)
                    DbWriter = Task.Factory.StartNew(() => AddOrUpdateProductProperties(), TaskCreationOptions.LongRunning);

                if (!endingFlag && (requestPool.Count < productPropertyUpdateThreads))
                {
                    addRequest(i++);
                    continue;
                }
                var wh_array = requestPool.Select(x => x.Key).ToArray();
                int reqPointer = WaitHandle.WaitAny(wh_array);
                var wro = requestPool[wh_array[reqPointer]];

                requestPool.Remove(wh_array[reqPointer]);
                if (!endingFlag)
                    addRequest(i);
                
                var productInfo = productInfoFrom_WRO(wro);
                if (productInfo is not null)
                {
                    var productPropertyList = productPropertyFromProductInfo(productInfo);

                    lock (productPropertiesListToUpdateDB)
                    {
                        productPropertiesListToUpdateDB.AddRange(productPropertyList);
                    }
                }

                if (requestPool.Count <= 0)
                    break;

                if (i + 1 >= skuProdPropToUpdate.Count)
                    endingFlag = true;
                else
                    i++;
            }
        }
    }
}
