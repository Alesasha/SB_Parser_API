using SB_Parser_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using static SB_Parser_API.MicroServices.Utils;
using static SB_Parser_API.MicroServices.SB_Parsers;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using static SB_Parser_API.MicroServices.DBSerices;
using AngleSharp.Dom;
using static SB_Parser_API.MicroServices.SearchViaBarcode;
using AngleSharp.Common;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch.Tasks;

//using System.Data.Entity;

namespace SB_Parser_API.MicroServices
{
    public record BarSku(long sku, string barcode);
    public class PISB_DB_Cache
    {
        List<Product_SB_V2> Products = new();
        Dictionary<long,List<PriceWithInfo>> Prices = new();
        List<Property_Product_DB> ProductProperties = new();
        
        //List<SB_Barcode> Barcodes = new();
        Dictionary<long, List<string>> Barcodes = new();

        List<SB_Image> Images = new();
        List<SB_barcode_product> BarProds=new();
        List<SB_image_product> ImProds = new();
        public bool FillCache(List<long> skul)
        {
            if (GetDBName(typeof(Product_SB_V2)) != "PISB" || GetDBName(typeof(Price_SB_V2)) != "PISB" || GetDBName(typeof(SB_Barcode)) != "PISB" ||
                GetDBName(typeof(SB_barcode_product)) != "PISB" || GetDBName(typeof(SB_Image)) != "PISB" || GetDBName(typeof(SB_image_product)) != "PISB")
                return false;
            
            var db = new PISBContext();

            Products = db.Products.AsNoTracking().AsEnumerable().Join(skul, p => p.sku, s => s, (p, s) => p).ToList();
            Products = skul.Join(db.Products.AsNoTracking(),s => s, p => p.sku, (s, p) => p).ToList(); //db.Products.AsNoTracking().AsEnumerable()
            Products = new();
            skul.ForEach(x => { var a = db.Products.AsNoTracking().FirstOrDefault(y => y.sku == x); if (a is not null) Products.Add(a); });


            var prices = (from p in db.Prices.AsNoTracking().AsEnumerable().Join(skul, p => p.product_sku, s => s, (p, s) => p)
                          join r in db.Retailers.AsNoTracking() on p.retailer equals r.id
                          join s in db.Stores.AsNoTracking() on p.store equals s.id
                          select new PriceWithInfo(p, s.lat, s.lon, s.full_address, r.name, r.logo_image, r.mini_logo_image)).ToList().OrderBy(x => x.price.price).ToList();
            
            Prices = prices.ToLookup(x => x.price.product_sku).ToDictionary(x => x.Key ?? 0, x => x.ToList());

            var Barcodes1 = db.BarProds.AsNoTracking().AsEnumerable().Join(skul, bp => bp.product_sku, s => s, (bp, s) => bp).AsEnumerable().Join(db.Barcodes.AsNoTracking(), 
                bp => bp.barcode_id, b => b.id,(bp, b) => new BarSku(bp.product_sku, b.barcode)).ToLookup(x=>x.sku).ToDictionary(x => x.Key, x => x.Select(z=>z.barcode).ToList());


            return true;
        }
    }
    public static class DBSerices
    {
        public static List<ProductName> productNames { get; set; } = new();
        public static List<SB_Barcode> productBarcodes { get; set; } = new();
        public static List<SB_Parser_API.Models.Store> RegsStoresList { get; set; } = new();

        public enum DBParam : int
        {
            TotalPriceScanners = 2, HideMyNameProxy = 11
        }

        static object Lproxy = new();
        static object LdeadProxy = new();
        static object LproxyToDomain = new();

        static object LSB_Products = new();
        static object LSB_Prices = new();
        static object LSB_Barcodes = new();
        static object LSB_Stores = new();
        static object LSB_Images = new();
        static object LSB_barcode_product = new();
        static object LSB_image_product = new();
        static object LSB_Property_Product = new();
        static object LSB_User = new();
        static object LSB_Query = new();

        //Microsoft.EntityFrameworkCore.Internal.InternalDbSet`1[[SB_Parser_API.Models.Retailer, SB_Parser_API, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null]]
        public static Dictionary<string, string> DB_Entity_Relation = new();
        static readonly Regex getFName = new(@"[^[]+\[+([^,]+),.+");//\[\[  [[]{2}
        static readonly Regex getName = new(@".+\.([^\.]+)");
        public static void InitDBSerices()
        {
            Microsoft.EntityFrameworkCore.DbContext db = null!;

            var fill_DB_Entity_Relation = (string dbn) =>
            {
                var dbSets = db.GetType().GetProperties().Select(x => x.GetValue(db)).Where(x => x!.GetType().FullName!.Contains("InternalDbSet"))!;
                foreach (var dbs in dbSets)
                {
                    string fnl = dbs?.GetType().FullName ?? "";
                    string fn = getFName.Replace(fnl, "$1");
                    string sn = getName.Replace(fn, "$1");
                    DB_Entity_Relation.TryAdd(fn, dbn);
                    DB_Entity_Relation.TryAdd(sn, dbn);
                }
            };

            DB_Entity_Relation.Clear();

            db = new PISBContext();
            fill_DB_Entity_Relation("PISB");
            db.Dispose();

            db = new PIPContext();
            fill_DB_Entity_Relation("PIP");
            db.Dispose();
        }
        public static Microsoft.EntityFrameworkCore.DbContext GetDBContext(Type eType)
        {
            var dbn = DB_Entity_Relation[eType.Name];
            switch (dbn)
            {
                case "PISB":
                    return new PISBContext();
                case "PIP":
                    return new PIPContext();
            }
            return null!;
        }
        public static string? GetDBName(Type eType)
        {
            return DB_Entity_Relation[eType.Name];
        }
        public static List<Proxy> GetProxyDB_List()
        {
            if (GetDBName(typeof(Proxy)) != "PIP")
                return null!;
            using var db = new PIPContext();
            return db.Proxies.ToList();
        }
        public static List<Proxy> GetProxyDB_partList()
        {
            if (GetDBName(typeof(Proxy)) != "PIP")
                return null!;
            using var db = new PIPContext();
            return db.Proxies.Select(x => new Proxy() { id = x.id, ip = x.ip, port = x.port, protocol = x.protocol }).ToList();
        }
        public static List<DeadProxy> GetDeadProxyDB_List()
        {
            if (GetDBName(typeof(DeadProxy)) != "PIP")
                return null!;
            using var db = new PIPContext();
            return db.DeadProxies.ToList();
        }
        public static List<DeadProxy> GetDeadProxyDB_partList()
        {
            if (GetDBName(typeof(DeadProxy)) != "PIP")
                return null!;
            using var db = new PIPContext();
            return db.DeadProxies.Select(x => new DeadProxy() { id = x.id, ip = x.ip, port = x.port, protocol = x.protocol }).ToList();
        }
        public static int? IsInProxyDB(Proxy p)
        {
            if (GetDBName(p.GetType()) != "PIP")
                return null;
            using var db = new PIPContext();
            var dp = db.Proxies.FirstOrDefault(x => x.ip == p.ip && x.port == p.port && x.protocol == p.protocol);
            return dp?.id;
        }
        public static int? IsInDeadProxyDB(Proxy p)
        {
            if (GetDBName(p.GetType()) != "PIP")
                return null;
            using var db = new PIPContext();
            var dp = db.DeadProxies.FirstOrDefault(x => x.ip == p.ip && x.port == p.port && x.protocol == p.protocol);
            return dp?.id;
        }

        public static int? ProxyAddNew(Proxy pr)
        {
            while (true)
            {
                try
                {
                    if (GetDBName(pr.GetType()) != "PIP")
                        return null;
                    using var db = new PIPContext();
                    lock (Lproxy)
                    {
                        db.DeadProxies.RemoveRange(db.DeadProxies.Where(x => x.ip == pr.ip && x.port == pr.port && x.protocol == pr.protocol));
                        db.Proxies.RemoveRange(db.Proxies.Where(x => x.ip == pr.ip && x.port == pr.port && x.protocol == pr.protocol));
                        pr.id = getIDforNewRecord(db.Proxies.Select(x => x.id).ToList());
                        db.Proxies.Add(pr);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DB ERROR_Proxy!!!! -- {ex.Message}");
                    continue;
                }
                break;
            }
            return pr.id;
        }
        public static int? DeadProxyAddNew(DeadProxy pr)
        {
            while (true)
            {
                try
                {
                    if (GetDBName(pr.GetType()) != "PIP")
                        return null;
                    using var db = new PIPContext();
                    lock (LdeadProxy)
                    {
                        db.DeadProxies.RemoveRange(db.DeadProxies.Where(x => x.ip == pr.ip && x.port == pr.port && x.protocol == pr.protocol));
                        db.Proxies.RemoveRange(db.Proxies.Where(x => x.ip == pr.ip && x.port == pr.port && x.protocol == pr.protocol));
                        pr.id = getIDforNewRecord(db.DeadProxies.Select(x => x.id).ToList());
                        db.DeadProxies.Add(pr);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DB ERROR_DeadProxy!!!! -- {ex.Message}");
                    continue;
                }
                break;
            }
            return pr.id;
        }
        public static List<Proxy_to_Domain> ProxyToDomainGet(Proxy_to_Domain ptd = null!)
        {
            ptd ??= new Proxy_to_Domain();
            while (true)
            {
                try
                {
                    if (GetDBName(ptd!.GetType()) != "PIP")
                        return new List<Proxy_to_Domain>();
                    using var db = new PIPContext();
                    if (ptd.ip.Length > 0)
                        return db.Proxy_to_Domain.Where(x => x.ip == ptd.ip && x.port == ptd.port && x.protocol == ptd.protocol &&
                        (ptd.domain.Length == 0 ? true : x.domain == ptd.domain)).ToList();
                    else if ((ptd.domain?.Length ?? 0) > 0)
                        return db.Proxy_to_Domain.Where(x => x.domain == ptd.domain).ToList();
                    else
                        return db.Proxy_to_Domain.ToList();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DB ERROR_ProxyToDomain (GET) !!!! -- {ex.Message}");
                    Task.Delay(1000).Wait();
                    continue;
                }
            }
        }
        public static void ProxyToDomainAddOrUpdate(Proxy_to_Domain ptd)
        {
            while (true)
            {
                try
                {
                    if (GetDBName(ptd.GetType()) != "PIP")
                        return;
                    using var db = new PIPContext();
                    lock (LproxyToDomain)
                    {
                        db.Proxy_to_Domain.RemoveRange(db.Proxy_to_Domain.Where(x => x.ip == ptd.ip &&
                        x.port == ptd.port && x.protocol == ptd.protocol && x.domain == ptd.domain).ToArray());
                        //var ptdInDB = db.Proxy_to_Domain.FirstOrDefault(x => x.ip == ptd.ip && x.port == ptd.port && x.protocol == ptd.protocol && x.domain == ptd.domain);
                        //if (ptdInDB is not null)
                        //    db.Proxy_to_Domain.Remove(ptdInDB);
                        db.Proxy_to_Domain.Add(ptd);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DB ERROR_ProxyToDomain!!!! -- {ex.Message}");
                    Task.Delay(1000).Wait();
                    continue;
                }
                break;
            }
            return;
        }

        public static void ProxyToDomainAddOrUpdateList(List<Proxy_to_Domain> ptds)
        {
            if (ptds is null || ptds.Count <= 0)
                return;
            while (true)
            {
                try
                {
                    if (GetDBName(ptds[0].GetType()) != "PIP")
                        return;
                    using var db = new PIPContext();
                    lock (LproxyToDomain)
                    {
                        foreach (var ptd in ptds)
                        {
                            db.Proxy_to_Domain.RemoveRange(db.Proxy_to_Domain.Where(x => x.ip == ptd.ip &&
                            x.port == ptd.port && x.protocol == ptd.protocol && x.domain == ptd.domain).ToList());
                            db.Proxy_to_Domain.Add(ptd);
                        }
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"DB ERROR_ProxyToDomain!!!! -- {ex.Message}");
                    Task.Delay(1000).Wait();
                    continue;
                }
                break;
            }
            return;
        }
        public static List<Store> StoresGet()
        {
            if (GetDBName(typeof(Store)) != "PISB")
                return new List<Store>();
            using var db = new PISBContext();
            //db.Database.SetCommandTimeout(180);
            return db.Stores.AsNoTracking().ToList();
        }
        public static List<Store> MosRegStoresGet()
        {
            if (GetDBName(typeof(Store)) != "PISB")
                return new List<Store>();
            using var db = new PISBContext();
            db.Database.SetCommandTimeout(180);
            return db.Stores.AsNoTracking().Where(x => x.last_product_count > 0 && x.active == true && (x.operational_zone_id == 0 || x.operational_zone_id == 1 || x.operational_zone_id == 17)).ToList();
        }

        static int totProd = 0;
        public static Dictionary<long, Product_SB_V2>? sProducts;
        public static Dictionary<long, long>? iProducts;
        static Dictionary<(long,int), Price_SB_V2>? sPrices;
        static Dictionary<string, int>? sBarcodes;
        static Dictionary<int, string>? sBarcodesr;
        static Dictionary<int, int>? barodesLinks;
        static Dictionary<long, List<SB_barcode_product>>? sBarProds;
        static Dictionary<string, int>? sImages;
        static Dictionary<int, string>? sImagesr;
        static Dictionary<int, int>? imagesLinks;
        static Dictionary<long, List<SB_image_product>>? sImProds;

        static List<int>? productPropertiesIds;
        static Dictionary<long, List<int>>? gProdPropIds;
        public static List<Property_Product_DB> productPropertiesListToUpdateDB = new();

        public static bool Init_AddOrUpdateProductProperties(bool newProdutsOnly = false)
        {
            if (GetDBName(typeof(Product_SB_V2)) != "PISB")
                return false;

            var db = new PISBContext();
            db.Database.SetCommandTimeout(180);

            sProducts = db.Products.AsNoTracking().Where(x=> !newProdutsOnly || x.dt_property_updated == null) .ToDictionary(x => x.sku ?? 0);
            iProducts = sProducts.ToDictionary(x => x.Value.id, x => x.Key);
            productPropertiesIds = GetFreeIdList(db.ProductProperties.AsNoTracking().Select(x => x.id).ToList());
            gProdPropIds = db.ProductProperties.AsNoTracking().ToLookup(x => x.product_sku).ToDictionary(x => x.Key, x => x.Select(p=>p.id).ToList());

            return true;
        }
        public static void AddOrUpdateProductProperties()
        {
            if (GetDBName(typeof(Product_SB_V2)) != "PISB")
                return;
            PISBContext db=null!;
            var saveChangesInDB = () =>
            {
                while (true)
                {
                    try
                    {
                        db!.SaveChanges();
                        break;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"{nameof(AddOrUpdateProductProperties)}: {e.Message}");
                        Task.Delay(3000).Wait(); continue;
                    }
                }
            };

            List<Product_SB_V2> productListToUpdate = new();
            List<Property_Product_DB> PP_List_To_Del = new();
            List<Property_Product_DB> cur_PP_List = new();

            int prodCount = 0;
            try
            {
                while (true)
                {
                    cur_PP_List?.Clear();
                    PP_List_To_Del.Clear();
                    productListToUpdate.Clear();
                    var dt = DateTime.Now;

                    lock (productPropertiesListToUpdateDB)
                    {
                        if (productPropertiesListToUpdateDB.Count > 0)
                        {
                            cur_PP_List = productPropertiesListToUpdateDB;
                            productPropertiesListToUpdateDB = new();
                        }
                    }
                    if ((cur_PP_List?.Count ?? 0) <= 0)
                    {
                        Task.Delay(5000).Wait();
                        continue;
                    }
                    cur_PP_List?.RemoveAll(x => x.product_id == 0 || x.product_sku == 0);///////  ????

                    var cur_SKU_Lookup = cur_PP_List!.ToLookup(x => x.product_sku).ToDictionary(x => x.Key, x => x.ToList());
                    //var cur_SKU_List = cur_PP_List.GroupBy(p => p.product_sku).Select(g => g.Key).ToList();
                    foreach (var sku in cur_SKU_Lookup.Keys)
                    {
                        if (gProdPropIds?.TryGetValue(sku, out var pp_id_List) ?? false)
                        {
                            pp_id_List.ForEach(x => { PP_List_To_Del.Add(new() { id = x }); productPropertiesIds?.Add(x); });
                            gProdPropIds[sku].Clear();
                        }
                        if (sProducts!.TryGetValue(sku, out var prod))
                        {
                            prod.dt_property_updated = dt;
                            productListToUpdate.Add(prod);
                            prodCount++;
                            if (prodCount % 1 == 0)
                            {
                                var ppPerH = TimeSpan.FromHours(1) * prodCount / SP_Timer!.Elapsed;
                                Console.WriteLine($"PRODUCT[{prodCount}]Ret={prod.retailer}_Store={prod.store}_Speed={(int)ppPerH:d4}_ppPH,FromStart:{SP_Timer.Elapsed:hh\\:mm\\:ss}_Product name: '{prod.name}'");
                            }
                        }
                        productPropertiesIds!.OrderByDescending(x => x).ToList();
                        cur_SKU_Lookup[sku].ForEach(x => { x.id = getIDforNewRecordFromFreeIdList(productPropertiesIds!); });
                        gProdPropIds![sku] = cur_SKU_Lookup[sku].Select(x => x.id).ToList();
                    }

                    db = new PISBContext();
                    try
                    {
                        db.ProductProperties.RemoveRange(PP_List_To_Del);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"1{nameof(AddOrUpdateProductProperties)}: {e.Message}");
                        Task.Delay(3000).Wait();
                    }

                    saveChangesInDB();
                    try
                    {
                        db.ProductProperties.AddRange(cur_PP_List ?? new());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"2{nameof(AddOrUpdateProductProperties)}: {e.Message}");
                        Task.Delay(3000).Wait();
                    }

                    saveChangesInDB();

                    try
                    {
                        db.Products.UpdateRange(productListToUpdate);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"3{nameof(AddOrUpdateProductProperties)}: {e.Message}");
                        Task.Delay(3000).Wait();
                    }
                    saveChangesInDB();
                    db.Dispose();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"4{nameof(AddOrUpdateProductProperties)}: {e.Message}");
                Task.Delay(3000).Wait();
            }
        }

        public static bool Init_AddOrUpdateProductsBatch()
        {
            if (GetDBName(typeof(Product_SB_V2)) != "PISB" || GetDBName(typeof(Price_SB_V2)) != "PISB" || GetDBName(typeof(SB_Barcode)) != "PISB" || 
                GetDBName(typeof(SB_barcode_product)) != "PISB" || GetDBName(typeof(SB_Image)) != "PISB" || GetDBName(typeof(SB_image_product)) != "PISB")
                return false;

            var db = new PISBContext();
            db.Database.SetCommandTimeout(180);

            sProducts = db.Products.AsNoTracking().ToDictionary(x=>x.sku ?? 0);
            iProducts = sProducts.ToDictionary(x=>x.Value.id,x=>x.Key);
            sPrices = db.Prices.AsNoTracking().ToDictionary(x => ((x.product_sku ?? 0), (x.retailer ?? 0)));
            sBarcodes = db.Barcodes.AsNoTracking().ToDictionary(x => x.barcode ?? "", x=>x.id);
            sImages = db.Images.AsNoTracking().ToDictionary(x =>x.url, x => x.id);
            sBarcodesr = sBarcodes.ToDictionary(x => x.Value, x => x.Key);
            sImagesr = sImages.ToDictionary(x => x.Value, x => x.Key);
            sBarProds = db.BarProds.AsNoTracking().ToLookup(x=>x.product_sku).ToDictionary(x=>x.Key,x=>x.ToList());
            barodesLinks = db.BarProds.AsNoTracking().ToLookup(x => x.barcode_id).ToDictionary(x => x.Key, x => x.Count());
            sImProds = db.ImProds.AsNoTracking().ToLookup(x => x.product_sku).ToDictionary(x => x.Key, x => x.ToList());
            imagesLinks = db.ImProds.AsNoTracking().ToLookup(x => x.image_id).ToDictionary(x => x.Key, x => x.Count());

            //var npa = new Price_SB_V2_Archive(sPrices.Last().Value);
            //db.Prices_Archive.RemoveRange(db.Prices_Archive);
            ////db.Prices_Archive.Add(npa);
            //db.SaveChanges();

            return true;
        }
        public static bool AddOrUpdateProductsBatch(List<Product_SB_V2>? prodBatch)
        {
            if (prodBatch is null || prodBatch.Count <=0)
                return false;
            while (true)
            {
                List<Task> tl = new();
                DateTime dt = DateTime.Now;
                List<Product_SB_V2> prodBatchShort = new();

                try
                {
                    tl.Add(Task.Factory.StartNew(() => AddProductPricesList(prodBatch, dt), TaskCreationOptions.LongRunning));
                    AddProductsList(prodBatch, dt, prodBatchShort);
                    
                    tl.Add(Task.Factory.StartNew(() => AddProductBarcodesList(prodBatchShort), TaskCreationOptions.LongRunning));
                    tl.Add(Task.Factory.StartNew(() => AddProductImagesList(prodBatchShort), TaskCreationOptions.LongRunning));

                    Task.WaitAll(tl.ToArray());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{nameof(AddOrUpdateProductsBatch)}!!!! -- {ex.Message},InnerException:'{ex.InnerException}'");
                    Task.Delay(2000).Wait();
                    continue;
                }
                break;
            }
            return true;
        }

/*
        public static Product_SB_V2? FindIidenticalProduct(Product_SB_V2 pr)
        {
            var dbpr = new Product_SB_V2();
            if (iProducts?.TryGetValue(pr.id, out var sk) ?? false && (sProducts?.TryGetValue(sk, out dbpr) ?? false))
                return dbpr!;
            if(!(sProducts?.TryGetValue(pr.sku ?? 0, out dbpr) ?? false))
                return null;
            if (pr.grams_per_unit == dbpr.grams_per_unit && pr.volume == dbpr.volume && pr.volume_type == dbpr.volume_type && pr.human_volume == dbpr.human_volume)
                return dbpr;

            return null;
        }
*/
        public static bool AddProductsList(List<Product_SB_V2> pB, DateTime dt, List<Product_SB_V2> pB_out)
        {
            if (GetDBName(typeof(Product_SB_V2)) != "PISB")
                return false;

            var db = new PISBContext();
            db.Database.SetCommandTimeout(180);

            List<Product_SB_V2> newProductList = new();
            List<Product_SB_V2> updateProductList = new();
            List<Product_SB_V2> delProductList = new();

            var prCount = 0;

            lock (LSB_Products)
            { 
                foreach (var pr in pB!) //
                {
                    pr.dt_updated = pr.dt_created = dt;
                    pr.dt_property_updated = null;

                    totProd++;
                    prCount++;
                    if (prCount%300 == 0)
                        Console.WriteLine($"PRODUCT[{totProd}]Ret={pr.retailer}_Store={pr.store}_({prCount})_Product name: '{pr.name}'");

                    Product_SB_V2? dbpr = null!;
                    if (sProducts?.TryGetValue(pr?.sku ?? 0, out dbpr) ?? false)
                    {
                        if (dbpr.dt_updated?.AddHours(productUpdatePeriod) > dt)
                            continue;

                        pr!.dt_created = dbpr.dt_created;
                        pr.dt_property_updated = dbpr.dt_property_updated;

                        if (pr.id != dbpr.id)
                        {
                            iProducts?.Remove(dbpr.id);
                            sProducts?.Remove(dbpr.sku ?? 0);
                            delProductList.Add(dbpr);
                        }
                        /*
                        if (pr.id == dbpr.id)
                            updateProductList.Add(pr);
                        else
                        {
                            iProducts?.Remove(dbpr.id);
                            delProductList.Add(dbpr);
                            newProductList.Add(pr);
                        }
                        */
                    }

                    if(iProducts?.TryGetValue(pr!.id, out _) ?? false)
                        updateProductList.Add(pr);
                    else
                        newProductList.Add(pr!);

                    iProducts![pr!.id] = pr.sku ?? 0;
                    sProducts![pr.sku ?? 0] = pr;
                    pB_out.Add(pr);
                }

                var saveChangesInDB = () =>
                {
                    while (true)
                    {
                        try
                        {
                            //Task.Delay(2000);
                            //Console.WriteLine($"{db.Products.FirstOrDefault(x => x.id == 29810523239)?.name} id={db.Products.FirstOrDefault(x => x.id == 29810523239)?.id}");
                            db.SaveChanges();
                            break;
                        }
                        catch (Exception e) { 
                            Console.WriteLine($"{nameof(AddProductsList)}: {e.Message}");
                            //break; 
                            Task.Delay(3000).Wait(); continue; 
                        }
                    }
                };

                Console.WriteLine($"Saving changes of {pB.Count} processed products...");

                if (delProductList.Count() > 0)
                    db.Products.RemoveRange(delProductList);

                saveChangesInDB();

                if (updateProductList.Count() > 0)
                    db.Products.UpdateRange(updateProductList);

                saveChangesInDB();

                if (newProductList.Count() > 0)
                    db.Products.AddRange(newProductList);

                saveChangesInDB();
                
                Console.WriteLine($"Save changes is DONE. Products processed total {pB.Count}.");
            }
            return true;
        }

        public static bool AddProductPricesList(List<Product_SB_V2> pB, DateTime ndt)
        {
            if (GetDBName(typeof(Price_SB_V2)) != "PISB")
                return false;
            var db = new PISBContext();
            db.Database.SetCommandTimeout(180);
            int prCount = 0;
            var NaNCheck = (double? d) => double.IsNaN(d ?? double.NaN) ? null : d;
            //--start-- Addition of the new Price
            lock (LSB_Prices)
            {
                var priceIds = GetFreeIdList(db.Prices.AsNoTracking().Select(x => x.id).ToList());
                var priceArchiveIds = GetFreeIdList(db.Prices_Archive.AsNoTracking().Select(x => x.id).ToList());
                List<Price_SB_V2> newPricesList = new();
                List<Price_SB_V2_Archive> newPricesArchiveList = new();
                List<Price_SB_V2> updatePricesList = new();
                foreach (var pr in pB!)
                {
                    Price_SB_V2? existingPrice = null;
                    sPrices?.TryGetValue((pr.sku ?? 0, pr.retailer ?? 0), out existingPrice);

                    var newPrice = new Price_SB_V2()
                    {
                        id = 0,
                        product_id = pr.id,
                        product_sku = pr.sku,
                        dt = ndt,
                        dt_created = ndt,
                        price = NaNCheck(pr.price),
                        original_price = NaNCheck(pr.original_price),
                        discount = NaNCheck(pr.discount),
                        discount_ends_at = pr.discount_ends_at,
                        unit_price = NaNCheck(pr.unit_price),
                        original_unit_price = NaNCheck(pr.original_unit_price),
                        stock = NaNCheck(pr.stock) ?? 0,
                        retailer = pr.retailer,
                        store = pr.store
                    };

                    prCount++;
                    if (prCount % 300 == 0)
                        Console.WriteLine($"PRICE[{totProd}]Ret={pr.retailer}_Store={pr.store}_({prCount})_Product name: '{pr.name}' Price={newPrice.price}");

                    if (existingPrice is not null)
                    {
                        newPrice.id = existingPrice.id;
                        if (existingPrice.price == newPrice.price)
                        {
                            newPrice.dt_created = existingPrice.dt_created;
                            newPrice.update_counter = existingPrice.update_counter + 1;
                        }
                        else
                        {
                            var archivePrice = new Price_SB_V2_Archive(existingPrice);
                            archivePrice.id = getIDforNewRecordFromFreeIdList(priceArchiveIds);
                            newPricesArchiveList.Add(archivePrice);
                        }
                        updatePricesList.Add(newPrice);
                    }
                    else
                    {
                        newPrice.id = getIDforNewRecordFromFreeIdList(priceIds);
                        newPricesList.Add(newPrice);
                    }
                    sPrices![(pr.sku ?? 0, pr.retailer ?? 0)] = newPrice;
                }
                if (newPricesList.Count()>0)
                    db.Prices.AddRange(newPricesList);
                if (newPricesArchiveList.Count() > 0)
                    db.Prices_Archive.AddRange(newPricesArchiveList);
                if (updatePricesList.Count() > 0)
                    db.Prices.UpdateRange(updatePricesList);
                Console.WriteLine($"Saving changes of {pB.Count} processed prices...");
                while (true)
                {
                    try
                    {
                        db.SaveChanges();
                        break;
                    }
                    catch (Exception e) { db.Prices.Remove(new() {id=450963}); Console.WriteLine($"{nameof(AddProductPricesList)}: {e.Message}, CommandTimeOut={db.Database.GetCommandTimeout()} ''''''''''"); Task.Delay(3000).Wait(); continue; }
                }
                Console.WriteLine($"Save changes is DONE. Prices processed total {pB.Count}.");
            }
            //--end-- Addition of the new Price
            return true;
        }
        public static bool AddProductBarcodesList(List<Product_SB_V2> pB)
        {
            if (GetDBName(typeof(SB_Barcode)) != "PISB" || GetDBName(typeof(SB_barcode_product)) != "PISB")
                return false;
            var db = new PISBContext();
            var barcodeIds = GetFreeIdList(db.Barcodes.AsNoTracking().Select(x => x.id).ToList());
            var barcodeProdIds = GetFreeIdList(db.BarProds.AsNoTracking().Select(x => x.id).ToList());
            List<SB_Barcode> newBarcodeList = new();
            List<SB_barcode_product> newBarcodeProductList = new();
            List<SB_Barcode> delBarcodeList = new();
            List<SB_barcode_product> delBarcodeProductList = new();

            int barCounter = 0;
            int prCount=0;
            lock (LSB_Barcodes) //lock (LSB_barcode_product)
            {
                foreach (var pr in pB!)
                {
                    List<SB_barcode_product>? barprsInDb = null;
                    sBarProds?.TryGetValue(pr.sku ?? 0, out barprsInDb);
                    barprsInDb = (barprsInDb ?? new()).ToList();

                    pr.eans = BarCodeCheckList(pr.eans) ?? new();
                    barCounter += pr.eans?.Count ?? 0;
                    var eans = pr.eans!.ToList();

                    prCount++;
                    if (prCount % 300 == 0)
                        Console.WriteLine($"BARCODE[{totProd}]Ret={pr.retailer}_Store={pr.store}_({prCount})_Product name: '{pr.name}' BarcodeCount={barCounter}");

                    foreach (var bp in barprsInDb)
                    {
                        if (sBarcodesr?.TryGetValue(bp.barcode_id, out var bar) ?? false)
                        {
                            if (eans.Contains(bar))
                            {
                                eans.Remove(bar);
                                continue;
                            }
                            if (barodesLinks?.TryGetValue(bp.barcode_id, out var lCount) ?? false)
                            {
                                if (lCount > 1)
                                    barodesLinks[bp.barcode_id]--;
                                else
                                {
                                    barodesLinks.Remove(bp.barcode_id);
                                    delBarcodeList.Add(new() { id = bp.barcode_id, barcode = bar });
                                    sBarcodes!.Remove(bar);
                                    sBarcodesr!.Remove(bp.barcode_id);
                                }
                            }
                            sBarProds![pr.sku ?? 0].Remove(bp);
                            delBarcodeProductList.Add(bp);
                        }
                    }
                    foreach (var bar in eans)
                    {
                        int dbbarid;
                        if (!sBarcodes!.TryGetValue(bar, out dbbarid))
                        {
                            var nid = dbbarid = getIDforNewRecordFromFreeIdList(barcodeIds);
                            SB_Barcode dbbar = new() { barcode = bar, id = nid };
                            newBarcodeList.Add(dbbar);
                            sBarcodes[bar] = nid;
                            sBarcodesr![nid] = bar;
                        }
                        try
                        {
                            SB_barcode_product nbp = new() { id = getIDforNewRecordFromFreeIdList(barcodeProdIds), product_id = pr.id, product_sku = pr.sku ?? 0, barcode_id = dbbarid };
                            newBarcodeProductList.Add(nbp);
                            if (!sBarProds!.TryGetValue(pr.sku ?? 0, out _))
                                sBarProds![pr.sku ?? 0] = new();
                            sBarProds![pr.sku ?? 0].Add(nbp);
                            int lCount = 0;
                            if (!(barodesLinks?.TryGetValue(dbbarid, out lCount) ?? false))
                                lCount = 0;

                            barodesLinks![dbbarid] = lCount+1;
                        }
                        catch (Exception e) { Console.WriteLine($"ERROR!!! - {e.Message}");Task.Delay(10000).Wait(); }
                    }
                }
                if (delBarcodeProductList.Count > 0)
                    db.BarProds.RemoveRange(delBarcodeProductList);
                if (delBarcodeList.Count > 0)
                    db.Barcodes.RemoveRange(delBarcodeList);
                if (newBarcodeProductList.Count > 0)
                    db.BarProds.AddRange(newBarcodeProductList);
                if (newBarcodeList.Count > 0)
                    db.Barcodes.AddRange(newBarcodeList);
                Console.WriteLine($"Saving changes of {barCounter} processed barcodes...");
                while (true)
                {
                    try
                    {
                        db.SaveChanges();
                        break;
                    }
                    catch (Exception e) { Console.WriteLine($"{nameof(AddProductBarcodesList)}: {e.Message}"); Task.Delay(3000).Wait(); continue; }
                }
                Console.WriteLine($"Save changes is DONE. Barcodes processed total {barCounter}.");
            }
            return true;
        }

        public static bool AddProductImagesList(List<Product_SB_V2> pB)
        {
            if (GetDBName(typeof(SB_Image)) != "PISB" || GetDBName(typeof(SB_image_product)) != "PISB")
                return false;
            var db = new PISBContext();
            var imageIds = GetFreeIdList(db.Images.AsNoTracking().Select(x => x.id).ToList());
            var imageProdIds = GetFreeIdList(db.ImProds.AsNoTracking().Select(x => x.id).ToList());
            List<SB_Image> newImageList = new();
            List<SB_image_product> newImageProductList = new();
            List<SB_Image> delImageList = new();
            List<SB_image_product> delImageProductList = new();

            int imCounter = 0;
            int prCount = 0;
            lock (LSB_Images) //lock (LSB_image_product)
            {
                foreach (var pr in pB!)
                {
                    List<SB_image_product>? imprsInDb = null;
                    sImProds?.TryGetValue(pr.sku ?? 0, out imprsInDb);
                    imprsInDb = (imprsInDb ?? new()).ToList();

                    imCounter += pr.images?.Count ?? 0;
                    var ims = pr.images!.Select(x=>x.url).ToList();

                    prCount++;
                    if (prCount % 300 == 0)
                        Console.WriteLine($"IMAGE[{totProd}]Ret={pr.retailer}_Store={pr.store}_({prCount})_Product name: '{pr.name}' ImageCount={imCounter}");

                    foreach (var ip in imprsInDb)
                    {
                        if (sImagesr?.TryGetValue(ip.image_id, out var im) ?? false)
                        {
                            if (ims.Contains(im))
                            {
                                ims.Remove(im);
                                continue;
                            }
                            if (imagesLinks?.TryGetValue(ip.image_id, out var lCount) ?? false)
                            {
                                if (lCount > 1)
                                    imagesLinks[ip.image_id]--;
                                else
                                {
                                    imagesLinks.Remove(ip.image_id);
                                    delImageList.Add(new() { id = ip.image_id, url = im });
                                    sImages!.Remove(im);
                                    sImagesr!.Remove(ip.image_id);
                                }
                            }
                            sImProds![pr.sku ?? 0].Remove(ip);
                            delImageProductList.Add(ip);
                        }
                    }
                    foreach (var im in ims)
                    {
                        int dbimid;
                        if (!sImages!.TryGetValue(im, out dbimid))
                        {
                            var nid = dbimid = getIDforNewRecordFromFreeIdList(imageIds);
                            SB_Image dbim = new() { url = im, id = nid };
                            newImageList.Add(dbim);
                            sImages[im] = nid;
                            sImagesr![nid] = im;
                        }
                        SB_image_product nip = new() { id = getIDforNewRecordFromFreeIdList(imageProdIds), product_id = pr.id, product_sku = pr.sku ?? 0, image_id = dbimid };
                        newImageProductList.Add(nip);
                        if (!sImProds!.TryGetValue(pr.sku ?? 0, out _))
                            sImProds![pr.sku ?? 0] = new();
                        sImProds![pr.sku ?? 0].Add(nip);
                        int lCount = 0;
                        if (!(imagesLinks?.TryGetValue(dbimid, out lCount) ?? false))
                            lCount = 0;

                        imagesLinks![dbimid] = lCount+1;
                    }
                }
                if (delImageProductList.Count > 0)
                    db.ImProds.RemoveRange(delImageProductList);
                if (delImageList.Count > 0)
                    db.Images.RemoveRange(delImageList);
                if (newImageProductList.Count > 0)
                    db.ImProds.AddRange(newImageProductList);
                if (newImageList.Count > 0)
                    db.Images.AddRange(newImageList);
                Console.WriteLine($"Saving changes of {imCounter} processed images...");
                while (true)
                {
                    try
                    {
                        db.SaveChanges();
                        break;
                    }
                    catch (Exception e) { Console.WriteLine($"{nameof(AddProductImagesList)}: {e.Message}"); Task.Delay(3000).Wait(); continue; }
                }
                Console.WriteLine($"Save changes is DONE. Images processed total {imCounter}.");
            }
            return true;
        }

        /*
                 public static bool AddProductBarcodesList(List<Product_SB_V2> pB)
                {
                    if (GetDBName(typeof(SB_Barcode)) != "PISB" || GetDBName(typeof(SB_barcode_product)) != "PISB")
                        return false;
                    var db = new PISBContext();
                    var barcodeIds = GetFreeIdList(db.Barcodes.AsNoTracking().Select(x => x.id).ToList());
                    var barcodeProdIds = GetFreeIdList(db.BarProds.AsNoTracking().Select(x => x.id).ToList());
                    List<SB_Barcode> newBarcodeList = new();
                    List<SB_barcode_product> newBarcodeProductList = new();
                    List<SB_Barcode> delBarcodeList = new();
                    List<SB_barcode_product> delBarcodeProductList = new();

                    int barCounter = 0;
                    int prCount=0;
                    lock (LSB_Barcodes) //lock (LSB_barcode_product)
                    {
                        foreach (var pr in pB!)
                        {
                            List<SB_barcode_product>? barprsToDel=null;
                            sBarProds?.TryGetValue(pr.sku ?? 0, out barprsToDel);
                            barprsToDel = (barprsToDel ?? new());

                            pr.eans = BarCodeCheckList(pr.eans);
                            barCounter += pr.eans?.Count ?? 0;

                            prCount++;
                            if (prCount % 300 == 0)
                                Console.WriteLine($"BARCODE[{totProd}]Ret={pr.retailer}_Store={pr.store}_({prCount})_Product name: '{pr.name}' BarcodeCount={barCounter}");

                            if ((pr.eans?.Count ?? 0) > 0)
                            {
                                //--start-- Addition new barcode and links to them
                                foreach (var bar in pr.eans!) //.Where(x => BarCodeCheck(x).Length > 0)
                                {
                                    int dbbarid;
                                    SB_Barcode? dbbar = null;

                                    if (!sBarcodes!.TryGetValue(bar, out dbbarid))
                                    {
                                        var nid = getIDforNewRecordFromFreeIdList(barcodeIds);
                                        dbbar = new() { barcode = bar, id = nid };
                                        newBarcodeList.Add(dbbar);
                                        sBarcodes[bar] = nid;
                                        sBarcodesr![nid] = bar;
                                    }
                                    else
                                        dbbar = new() { barcode = bar, id = dbbarid };

                                    if (barprsToDel.FirstOrDefault(x => x.barcode_id == dbbar.id) is null)
                                    {
                                        var nid = getIDforNewRecordFromFreeIdList(barcodeProdIds);
                                        SB_barcode_product nbp = new() { barcode_id = dbbar.id, product_id = pr.id, product_sku = pr.sku ?? 0, id = nid };
                                        newBarcodeProductList.Add(nbp);
                                        if (!sBarProds!.TryGetValue(nbp.product_sku, out _))
                                            sBarProds![nbp.product_sku] = new();

                                        sBarProds![nbp.product_sku].Add(nbp);
                                    }
                                    //else
                                    //    barprsToDel.RemoveAll(x => x.barcode_id == dbbarid);
                                }
                                //--end-- Addition new barcode and links to them

                                //--start-- Deletion obsolete barcode and links to them
                                //barprsToDel = barprsToDel.Where(x => x.product_id == pr.id).ToList();
                                //foreach (var barpr in barprsToDel)  // Deletion obsolete barcode and links to them
                                //{
                                //    string? bar = null;
                                //    if (sBarcodesr!.TryGetValue(barpr.barcode_id, out bar))
                                //    {
                                //        delBarcodeList.Add(new() { id = barpr.barcode_id, barcode = bar });
                                //        sBarcodes!.Remove(bar);
                                //        sBarcodesr!.Remove(barpr.barcode_id);
                                //    }
                                //    delBarcodeProductList.Add(barpr);
                                //    sBarProds![barpr.product_sku].RemoveAll(x=>x.id== barpr.id);
                                //}
                                //--end-- Deletion obsolete barcode and links to them
                            }
                        }
                        //if (delBarcodeProductList.Count > 0)
                        //    db.BarProds.RemoveRange(delBarcodeProductList);
                        //if (delBarcodeList.Count > 0)
                        //    db.Barcodes.RemoveRange(delBarcodeList);
                        if (newBarcodeProductList.Count > 0)
                            db.BarProds.AddRange(newBarcodeProductList);
                        if (newBarcodeList.Count > 0)
                            db.Barcodes.AddRange(newBarcodeList);
                        Console.WriteLine($"Saving changes of {barCounter} processed barcodes...");
                        while (true)
                        {
                            try
                            {
                                db.SaveChanges();
                                break;
                            }
                            catch (Exception e) { Console.WriteLine($"{nameof(AddProductBarcodesList)}: {e.Message}"); Task.Delay(3000).Wait(); continue; }
                        }
                        Console.WriteLine($"Save changes is DONE. Barcodes processed total {barCounter}.");

                    }
                    return true;
                }


                public static bool AddProductImagesList(List<Product_SB_V2> pB)
                {
                    if (GetDBName(typeof(SB_Image)) != "PISB" || GetDBName(typeof(SB_image_product)) != "PISB")
                        return false;
                    var db = new PISBContext();
                    var imageIds = GetFreeIdList(db.Images.AsNoTracking().Select(x => x.id).ToList());
                    var imageProdIds = GetFreeIdList(db.ImProds.AsNoTracking().Select(x => x.id).ToList());
                    List<SB_Image> newImageList = new();
                    List<SB_image_product> newImageProductList = new();
                    List<SB_Image> delImageList = new();
                    List<SB_image_product> delImageProductList = new();

                    int imCounter = 0;
                    int prCount=0;
                    lock (LSB_Images) //lock (LSB_image_product)
                    {
                        foreach (var pr in pB!)
                        {
                            List<SB_image_product>? imprsToDel = null;
                            sImProds?.TryGetValue(pr.sku ?? 0, out imprsToDel);
                            imprsToDel = (imprsToDel ?? new());

                            imCounter += pr.images?.Count ?? 0;

                            prCount++;
                            if (prCount % 300 == 0)
                                Console.WriteLine($"IMAGE[{totProd}]Ret={pr.retailer}_Store={pr.store}_({prCount})_Product name: '{pr.name}' BarcodeCount={imCounter}");

                            if ((pr.images?.Count ?? 0) > 0)
                            {
                                //--start-- Addition new image and links to them
                                foreach (var im in (pr?.images ?? new()))
                                {
                                    int dbimid;
                                    SB_Image? dbim = null;

                                    if (!sImages!.TryGetValue(im.url, out dbimid))
                                    {
                                        var nid = getIDforNewRecordFromFreeIdList(imageIds);
                                        dbim = new() { url = im.url, id = nid };
                                        newImageList.Add(dbim);
                                        sImages[im.url] = nid;
                                        sImagesr![nid] = im.url;
                                    }
                                    else
                                        dbim = new() { url = im.url, id = dbimid };

                                    if (imprsToDel.FirstOrDefault(x => x.image_id == dbim.id) is null)
                                    {
                                        var nid = getIDforNewRecordFromFreeIdList(imageProdIds);
                                        SB_image_product nip = new() { image_id = dbim.id, product_id = (pr?.id ?? 0), product_sku = (pr?.sku ?? 0), id = nid };
                                        newImageProductList.Add(nip);
                                        if(!sImProds!.TryGetValue(nip.product_sku, out _))
                                            sImProds![nip.product_sku]=new();
                                        sImProds![nip.product_sku].Add(nip);
                                    }
                                    //else
                                    //    imprsToDel.RemoveAll(x => x.image_id == dbimid);
                                }
                                //--end-- Addition new barcode and links to them

                                //--start-- Deletion obsolete barcode and links to them
                                //imprsToDel = imprsToDel.Where(x => x.product_id == pr?.id).ToList();
                                //foreach (var impr in imprsToDel)  // Deletion obsolete barcode and links to them
                                //{
                                //    string? im = null;
                                //    if (sBarcodesr!.TryGetValue(impr.image_id, out im))
                                //    {
                                //        delImageList.Add(new() { id = impr.image_id, url = im });
                                //        sImages!.Remove(im);
                                //        sImagesr!.Remove(impr.image_id);
                                //    }
                                //    delImageProductList.Add(impr);
                                //    sImProds![impr.product_sku].RemoveAll(x => x.id == impr.id);
                                //}
                                //--end-- Deletion obsolete barcode and links to them
                            }
                        }
                        //if (delImageProductList.Count > 0)
                        //    db.ImProds.RemoveRange(delImageProductList);
                        //if (delImageList.Count > 0)
                        //    db.Images.RemoveRange(delImageList);
                        if (newImageProductList.Count > 0)
                            db.ImProds.AddRange(newImageProductList);
                        if (newImageList.Count > 0)
                            db.Images.AddRange(newImageList);
                        Console.WriteLine($"Saving changes of {imCounter} processed images...");
                        while (true)
                        {
                            try
                            {
                                db.SaveChanges();
                                break;
                            }
                            catch (Exception e) { Console.WriteLine($"{nameof(AddProductImagesList)}: {e.Message}"); Task.Delay(3000).Wait(); break; }
                        }
                        Console.WriteLine($"Save changes is DONE. Images processed total {imCounter}.");

                    }
                    return true;
                }
        */
        public static (int?, string) GetDBParam(DBParam par)
        {
            PI_Parameter? p;
            if (GetDBName(typeof(PI_Parameter)) != "PIP")
                return (0, "");
            using var db = new PIPContext();

            p = db.PI_Parameters.FirstOrDefault(x => x.id == (int)par);
            //var pi = new PI_Parameter() { id = 1000, note = "Test", value = 1111, valueS = "1111--" };
            //db.PI_Parameters.Add(pi);
            //var pig = db.PI_Parameters.FirstOrDefault(x => x.id == 1000);
            //var pil = db.PI_Parameters.Local.FirstOrDefault(x => x.id == 1000);
            return (p!.value ?? 0, p.valueS ?? "");
        }
        public static int? GetRetailerId(int store_id)
        {
            if (GetDBName(typeof(Store)) != "PISB")
                return null;
            using var db = new PISBContext();
            var rid = db.Stores.FirstOrDefault(x => x.id == store_id)?.retailer_id;
            return rid;
        }
        public static Retailer? GetRetailer(int retailer_id)
        {
            if (GetDBName(typeof(Retailer)) != "PISB")
                return null;
            using var db = new PISBContext();

            var retailer = db.Retailers.FirstOrDefault(x => x.id == retailer_id);
            return retailer;
        }
        public static bool AddNewUser(ZC_User user)
        {
            if (GetDBName(typeof(ZC_User)) != "PISB")
                return false;
            lock (LSB_User)
            {
                using var db = new PISBContext();
                var userIds = GetFreeIdList(db.Users.AsNoTracking().Select(x => x.id).ToList());
                user.id = getIDforNewRecordFromFreeIdList(userIds);

                db.Users.Add(user);

                while (true)
                {
                    try
                    {
                        db.SaveChanges();
                        break;
                    }
                    catch (Exception e) { Console.WriteLine($"{nameof(AddProductProperties)}: {e.Message}"); Task.Delay(3000).Wait(); continue; }
                }
            }
            return true;
        }
        public static bool AddNewQuery(Query_ZC query)
        {
            if (GetDBName(typeof(Query_ZC)) != "PISB")
                return false;
            lock (LSB_Query)
            {
                using var db = new PISBContext();
                var queryIds = GetFreeIdList(db.Queries.AsNoTracking().Select(x => x.id).ToList());
                query.id = getIDforNewRecordFromFreeIdList(queryIds);

                db.Queries.Add(query);

                while (true)
                {
                    try
                    {
                        db.SaveChanges();
                        break;
                    }
                    catch (Exception e) { Console.WriteLine($"{nameof(AddProductProperties)}: {e.Message}"); Task.Delay(3000).Wait(); continue; }
                }
            }
            return true;
        }
        public static List<int> GetBarcodeIdList(string barcode)
        {
            //if (GetDBName(typeof(SB_Barcode)) != "PISB")
            //    return new List<int>();
            //using var db = new PISBContext();

            var bid = productBarcodes.Where(x => x.barcode == barcode).Select(x => x.id).ToList();
            if (barcode[0] == '2' && bid.Count <= 0) // && barcode.Length > 5
            {
                barcode = barcode.Remove(6); //barcode.Length - 1
                bid = productBarcodes.Where(x => x.barcode.StartsWith(barcode)).Select(x => x.id).ToList();
            }
            return bid.Distinct().ToList();
        }
        public static List<Product_SB_V2?> GetProductsFromBarcodeId(int barcode_id)
        {
            if (GetDBName(typeof(SB_barcode_product)) != "PISB" || GetDBName(typeof(Product_SB_V2)) != "PISB")
                return new();
            using var db = new PISBContext();

            var skul = db.BarProds.Where(x => x.barcode_id == barcode_id)?.Select(x=>x.product_sku).ToList() ?? new();
            var products = new List<Product_SB_V2?>();
            
            //var products = db.Products.AsNoTracking().ToList().Join(skul, p => p.sku, s => s, (p, s) => p).ToList() ?? new();

            foreach (var s in skul ?? new())
                products.AddRange(db.Products.Where(x => x.sku == s).ToList());

            return products!;
        }

        public static Product_SB_V2? GetProductFromSKU(long sku)
        {
            if (GetDBName(typeof(Product_SB_V2)) != "PISB")
                return null;
            using var db = new PISBContext();
            var product = db.Products.FirstOrDefault(x => x.sku == sku);
            return product;
        }

        public record PriceWithInfo(Price_SB_V2 price, double lat, double lon, string? address, string? retailer_name, string? retailer_logo_image, string? retailer_mini_logo_image);
        public static List<PriceWithInfo>? GetPricesFromSKU_WithRetailerAndStoreInfo(long sku)
        {
            if (GetDBName(typeof(Price_SB_V2)) != "PISB")
                return null;
            using var db = new PISBContext();
            /*
            var prices = (from p in db.Prices.AsNoTracking().Where(x => x.product_sku == sku)
                         join r in db.Retailers.AsNoTracking() on p.retailer equals r.id
                         join s in db.Stores.AsNoTracking() on p.store equals s.id
                         select new PriceWithInfo(p, s.lat, s.lon, r.name, r.logo_image, r.mini_logo_image)).ToList().OrderBy(x=>x.price.price).ToList();
            */
            var prices_SB_V2 = db.Prices.AsNoTracking().Where(x => x.product_sku == sku).ToList();
            List<PriceWithInfo> prices = new();
            prices_SB_V2.ForEach(p => {
            var ret = db.Retailers.AsNoTracking().FirstOrDefault(r => r.id == p.retailer);
            var sto = db.Stores.AsNoTracking().FirstOrDefault(s => s.id == p.store);
            prices.Add(new PriceWithInfo(p, sto?.lat ?? 0, sto?.lon ?? 0, sto?.full_address , ret?.name, ret?.logo_image, ret?.mini_logo_image));
        });

            return prices;
        }
        public static List<string>? GetBarcodesFromSKU(long sku)
        {
            if (GetDBName(typeof(SB_Barcode)) != "PISB")
                return null;
            using var db = new PISBContext();

            //var barcodes = db.BarProds.AsNoTracking().Where(x => x.product_sku == sku).Join(db.Barcodes.AsNoTracking(),bp=>bp.barcode_id,b=>b.id,(bp,b)=> b.barcode).ToList();

            var bar_prod = db.BarProds.AsNoTracking().Where(x => x.product_sku == sku).ToList();
            List<string> barcodes = new();
            bar_prod.ForEach(x => { var bar = db.Barcodes.AsNoTracking().FirstOrDefault(b => b.id == x.barcode_id)?.barcode; if (bar is not null) barcodes.Add(bar); });

            return barcodes;
        }
        public static List<Price_SB_V2>? GetPricesFromSKU(long sku)
        {
            if (GetDBName(typeof(Price_SB_V2)) != "PISB")
                return null;
            using var db = new PISBContext();
            var prices = db.Prices.AsNoTracking().Where(x => x.product_sku == sku).OrderBy(x => x.price).ToList();
            return prices;
        }
        public static List<ProductName>? GetProductNamesForSearch()
        {
            if (GetDBName(typeof(Product_SB_V2)) != "PISB")
                return null;
            using var db = new PISBContext();
            db.Database.SetCommandTimeout(180);
            var names = db.Products.Select(x => new ProductName() { Id = x.sku ?? 0, name = x.name ?? "", namepp = PreProcessString(x.name ?? "") }).ToList();
            return names;
        }
        public static List<SB_Barcode>? GetProductBarcodesForSearch()
        {
            if (GetDBName(typeof(Product_SB_V2)) != "PISB")
                return null;
            using var db = new PISBContext();
            db.Database.SetCommandTimeout(180);
            var bars = db.Barcodes.ToList();
            return bars;
        }
        public static List<string>? GetImagesFromSKU(long sku)
        {
            if (GetDBName(typeof(SB_image_product)) != "PISB" || GetDBName(typeof(SB_Image)) != "PISB")
                return null;
            using var db = new PISBContext();

            //var images = db.ImProds.AsNoTracking().Where(x => x.product_sku == sku).Join(db.Images.AsNoTracking(), ip => ip.image_id, i => i.id, (im, i) => i.url).ToList();

            var im_prod = db.ImProds.AsNoTracking().Where(x => x.product_sku == sku).ToList();
            List<string> images = new();
            im_prod.ForEach(ip => {var im = db.Images.AsNoTracking().FirstOrDefault(i => i.id == ip.image_id)?.url;if (im is not null) images.Add(im);});

            return images;
        }
        public static List<Property_Product_DB>? GetProductPropertiesFromSKU(long sku)
        {
            if (GetDBName(typeof(Property_Product_DB)) != "PISB")
                return null;
            using var db = new PISBContext();

            var properties = db.ProductProperties.AsNoTracking().Where(x => x.product_sku == sku).ToList();
            return properties;
        }
        public static int? GetMaxStoreNumber()
        {
            if (GetDBName(typeof(Store)) != "PISB")
                return null;
            using var db = new PISBContext();
            //var stores = db.Stores.AsNoTracking().Max(x => x.id);// .ToList();
            //var max_id = stores.Max(x => x.id);
            return db.Stores.AsNoTracking().Max(x => x.id);
            //max_id;
        }
        public static bool AddOrUpdateStoresBatch(List<Store> storeBatch)
        {
            if (GetDBName(typeof(Store)) != "PISB")
                return false;
            var l0 = storeBatch.Count;
            var sb = storeBatch;

            //for ()
            storeBatch = storeBatch.Distinct().ToList();
            var l1 = storeBatch.Count;

            lock (LSB_Stores)
            {   
                using var db = new PISBContext();
                foreach (var store in storeBatch)
                {
                    var ob = db.Stores.FirstOrDefault(x => x.id == store.id);
                    if (ob is not null) db.Stores.Remove(ob);
                    //try { db.Stores.Remove(store); }
                    //catch  { }   

                    try { db.Stores.Add(store); }
                    catch { }
                }
                while (true)
                {
                    try
                    {
                        db.SaveChanges();
                        break;
                    }
                    catch (Exception e) { Console.WriteLine($"{nameof(AddOrUpdateStoresBatch)}: {e.Message}"); Task.Delay(3000).Wait(); continue; }
                }
            }
            return true;
        }
        public static bool AddProductProperties(List<Property_Product_DB> propList)
        {
            if (GetDBName(typeof(Property_Product_DB)) != "PISB")
                return false;
            lock (LSB_Property_Product)
            {
                using var db = new PISBContext();
                var propIds = GetFreeIdList(db.ProductProperties.AsNoTracking().Select(x => x.id).ToList());
                propList.ForEach(x => x.id=getIDforNewRecordFromFreeIdList(propIds));

                db.ProductProperties.AddRange(propList);

                while (true)
                {
                    try
                    {
                        db.SaveChanges();
                        break;
                    }
                    catch (Exception e) { Console.WriteLine($"{nameof(AddProductProperties)}: {e.Message}"); Task.Delay(3000).Wait(); continue; }
                }
            }
            return true;
        }
    }
}
