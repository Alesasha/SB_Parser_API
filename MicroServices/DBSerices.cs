using SB_Parser_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using static SB_Parser_API.MicroServices.Utils;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using static SB_Parser_API.MicroServices.DBSerices;
using AngleSharp.Dom;
using static SB_Parser_API.MicroServices.SearchViaBarcode;
using AngleSharp.Common;
//using System.Data.Entity;

namespace SB_Parser_API.MicroServices
{
    public static class DBSerices
    {
        public static List<ProductName> productNames { get; set; } = new();

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
            return db.Stores.ToList();
        }

        static int totProd = 0;
        static Dictionary<long, Product_SB_V2>? sProducts;
        static Dictionary<(long,int), Price_SB_V2>? sPrices;
        static Dictionary<string, int>? sBarcodes;
        static Dictionary<long, List<SB_barcode_product>>? sBarProds;
        static Dictionary<string, int>? sImages;
        static Dictionary<long, List<SB_image_product>>? sImProds;

        public static bool Init_AddOrUpdateProductsBatch()
        {
            if (GetDBName(typeof(Product_SB_V2)) != "PISB" || GetDBName(typeof(Price_SB_V2)) != "PISB" || GetDBName(typeof(SB_Barcode)) != "PISB" || 
                GetDBName(typeof(SB_barcode_product)) != "PISB" || GetDBName(typeof(SB_Image)) != "PISB" || GetDBName(typeof(SB_image_product)) != "PISB")
                return false;

            var db = new PISBContext();

            sProducts = db.Products.AsNoTracking().ToDictionary(x=>x.sku ?? 0);
            sPrices = db.Prices.AsNoTracking().ToDictionary(x => ((x.product_sku ?? 0), (x.retailer ?? 0)));
            sBarcodes = db.Barcodes.AsNoTracking().ToDictionary(x => x.barcode ?? "", x=>x.id);
            sImages = db.Images.AsNoTracking().ToDictionary(x =>x.url, x => x.id);
            sBarProds = db.BarProds.AsNoTracking().ToLookup(x=>x.product_sku).ToDictionary(x=>x.Key,x=>x.ToList());
            sImProds = db.ImProds.AsNoTracking().ToLookup(x => x.product_sku).ToDictionary(x => x.Key, x => x.ToList());


            return true;
        }
        public static bool AddOrUpdateProductsBatch(List<Product_SB_V2>? prodBatch, bool saveAllAtOnce = false)
        {
            if ((prodBatch is null) || (prodBatch.Count <= 0))
                return false;
            if (GetDBName(typeof(Product_SB_V2)) != "PISB")
                return false;

            var db = new PISBContext();

            var priceIds = GetFreeIdList(db.Prices.AsNoTracking().Select(x => x.id).ToList());
            var imageIds = GetFreeIdList(db.Images.AsNoTracking().Select(x => x.id).ToList());
            var imageProdIds = GetFreeIdList(db.ImProds.AsNoTracking().Select(x => x.id).ToList());
            var barcodeIds = GetFreeIdList(db.Barcodes.AsNoTracking().Select(x => x.id).ToList());
            var barcodeProdIds = GetFreeIdList(db.BarProds.AsNoTracking().Select(x => x.id).ToList());
            
            Dictionary<(long, long), SB_barcode_product> jgh = new();
            





            List<Product_SB_V2> newProductList = new();
            List<Price_SB_V2> newPricesList = new();
            List<SB_Barcode> newBarcodeList = new();
            List<SB_barcode_product> newBarcodeProductList = new();
            List<SB_Image> newImageList = new();
            List<SB_image_product> newImageProductList = new();

            while (true)
            {
                try
                {
                    var prCount = 0;
                    foreach (var pr in prodBatch!)
                    {
                        pr.dt_updated = pr.dt_created = DateTime.Now;
                        pr.eans = BarCodeCheckList(pr.eans);
                        lock (LSB_Products)
                        {
                            prCount++;
                            totProd++;
                            Console.WriteLine($"[{totProd}]Ret={pr.retailer}_Store={pr.store}_({prCount})_Product name: '{pr.name}'");
                            if (!saveAllAtOnce)
                            {
                                db?.Dispose();
                                db = new PISBContext();
                            }
                            //var db = new PISBContext();
                            var dbpr = db.Products.FirstOrDefault(x => x.id == pr.id) ?? newProductList.FirstOrDefault(x => x.id == pr.id);
                            if (dbpr is not null)
                            {
                                pr.dt_created = dbpr.dt_created;
                                db.Products.Remove(dbpr);
                                while (newProductList.Remove(dbpr)) ;
                                db.Products.Add(pr);
                                newProductList.Add(pr);
                            }
                            else if ((db.Products.FirstOrDefault(x => x.sku == pr.sku) ?? newProductList.FirstOrDefault(x => x.sku == pr.sku)) is null)
                            {
                                db.Products.Add(pr);
                                newProductList.Add(pr);
                            }

                            //while (!saveAllAtOnce)
                            //{
                            //    try
                            //    {
                            //        db.SaveChanges();
                            //        db.Dispose();
                            //        break;
                            //    }
                            //    catch (Exception e) { Console.WriteLine($"{nameof(AddOrUpdateProductsBatch)}: {e.Message}"); Task.Delay(3000).Wait(); continue; }
                            //}
                        }
                        //if (saveAllAtOnce)
                        //{
                            //--start-- Addition of the new Price
                            AddProductPrice(pr, db, priceIds, newPricesList);
                            //--end-- Addition of the new Price
                            //--start-- Addition new images and links to them
                            AddProductImages(pr, db, imageIds, imageProdIds, newImageList, newImageProductList);
                            //--end-- Addition new images and links to them
                            //--start-- Addition new barcodes and links to them
                            AddProductBarcodes(pr, db, barcodeIds, barcodeProdIds, newBarcodeList, newBarcodeProductList);
                            //--end-- Addition new barcodes and links to them
                        //}
                        //else
                        //{
                        //    //--start-- Addition of the new Price
                        //    AddProductPrice(pr, db, priceIds, newPricesList);
                        //    //--end-- Addition of the new Price
                        //    //--start-- Addition new images and links to them
                        //    AddProductImages(pr, db, imageIds, imageProdIds, newImageList, newImageProductList);
                        //    //--end-- Addition new images and links to them
                        //    //--start-- Addition new barcodes and links to them
                        //    AddProductBarcodes(pr, db, barcodeIds, barcodeProdIds, newBarcodeList, newBarcodeProductList);
                        //    //--end-- Addition new barcodes and links to them
                        //}
                    }
                    while (true) //saveAllAtOnce
                    {
                        lock (LSB_Products)
                        {
                            try
                            {
                                db.SaveChanges();
                                db.Dispose();
                                break;
                            }
                            catch (Exception e) { Console.WriteLine($"{nameof(AddOrUpdateProductsBatch)}(All): {e.Message}"); Task.Delay(3000).Wait(); continue; }
                        }
                    }
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

        public static bool AddProductPrice(Product_SB_V2 pr, PISBContext extDB, List<int> ids, List<Price_SB_V2> npl)
        {
            if (GetDBName(typeof(Price_SB_V2)) != "PISB")
                return false;

            //--start-- Addition of the new Price
            lock (LSB_Prices)
            {
                var db = extDB ?? new PISBContext();
                //var db = new PISBContext();

                var nid = getIDforNewRecordFromFreeIdList(ids);

                var newPrice = new Price_SB_V2()
                {
                    id = nid,
                    product_id = pr.id,
                    product_sku = pr.sku,
                    dt = DateTime.Now,
                    price = pr.price,
                    original_price = pr.original_price,
                    discount = pr.discount,
                    discount_ends_at = pr.discount_ends_at,
                    unit_price = pr.unit_price,
                    original_unit_price = pr.original_unit_price,
                    stock = pr.stock,
                    retailer = pr.retailer,
                    store = pr.store
                };
                var psp = db.Prices.Where(x => x.product_sku == pr.sku && x.store == pr.store).ToList()
                    .Union(npl.Where(x => x.product_sku == pr.sku && x.store == pr.store))
                    .OrderByDescending(x => x.dt).ToList();

                for (var i = 1; i < psp.Count; i++)
                {
                    if (psp[i].price != pr.price)
                        break;
                    if (psp[i - 1].price == pr.price)
                    {
                        while (db.Prices.Local.Remove(psp[i - 1]));
                        while (npl.Remove(psp[i - 1]));
                    }
                }
                db.Prices.Add(newPrice);
                npl.Add(newPrice);

                //while (extDB is null)
                //{
                //    try
                //    {
                //        db.SaveChanges();
                //        break;
                //    }
                //    catch (Exception e) { Console.WriteLine($"{nameof(AddProductPrice)}: {e.Message}"); Task.Delay(3000).Wait(); continue; }
                //}
            }
            //--end-- Addition of the new Price
            return true;
        }
        public static bool AddProductBarcodes(Product_SB_V2 pr, PISBContext extDB, List<int> barIds, List<int> barprIds, List<SB_Barcode> nbl, List<SB_barcode_product> nbpl)
        {
            //if (GetDBName(typeof(SB_Barcode)) != "PISB" || GetDBName(typeof(SB_image_product)) != "PISB")
            //    return false;

            //--start-- Addition of the new Price
            lock (LSB_Barcodes) lock (LSB_barcode_product)
                {
                    var db = extDB ?? new PISBContext();

                    //var db = new PISBContext();
                    if ((pr.eans?.Count ?? 0) > 0)
                    {
                        //--start-- Addition new barcode and links to them
                        foreach (var bar in pr.eans!) //.Where(x => BarCodeCheck(x).Length > 0)
                        {
                            var dbbar = db.Barcodes.FirstOrDefault(x => x.barcode == bar) ?? nbl.FirstOrDefault(x => x.barcode == bar);

                            if (dbbar is null)
                            {
                                var nid = getIDforNewRecordFromFreeIdList(barIds);
                                dbbar = new() { barcode = bar, id = nid };
                                db.Barcodes.Add(dbbar);
                                nbl.Add(dbbar);
                            }
                            if ((db.BarProds.FirstOrDefault(x => x.product_sku == pr.sku && x.barcode_id == dbbar.id) ??
                                nbpl.FirstOrDefault(x => x.product_sku == pr.sku && x.barcode_id == dbbar.id)) is null)
                            {
                                var nid = getIDforNewRecordFromFreeIdList(barprIds);
                                SB_barcode_product nbp = new() { barcode_id = dbbar.id, product_id = pr.id, product_sku = pr.sku ?? 0, id = nid };
                                db.BarProds.Add(nbp);
                                nbpl.Add(nbp);
                            }

                            //while (extDB is null)
                            //{
                            //    try
                            //    {
                            //        db.SaveChanges();
                            //        break;
                            //    }
                            //    catch (Exception e) { Console.WriteLine($"{nameof(AddProductBarcodes)}/add/: {e.Message}"); Task.Delay(3000).Wait(); continue; }
                            //}
                        }
                        //--end-- Addition new barcode and links to them

                        //--start-- Deletion obsolete barcode and links to them
                        var barprs = db.BarProds.Where(x => x.product_id == pr.id).ToList().Union(nbpl.Where(x => x.product_id == pr.id)).ToList();
                        foreach (var barpr in barprs)  // Deletion obsolete barcode and links to them
                        {
                            var dbbar = db.Barcodes.FirstOrDefault(x => x.id == barpr.barcode_id) ?? nbl.FirstOrDefault(x => x.id == barpr.barcode_id);
                            if (dbbar is not null)
                            {
                                if (pr.eans.FirstOrDefault(x => x == dbbar.barcode) is null)
                                {
                                    if ((db.BarProds.Where(x => x.barcode_id == dbbar.id && x.product_id != pr.id).ToList()
                                    .Union(nbpl.Where(x => x.barcode_id == dbbar.id && x.product_id != pr.id))).Count() <= 0)
                                    {
                                        db.Barcodes.Remove(dbbar);
                                        nbl.Remove(dbbar);
                                        addFreeIdToList(barIds, dbbar.id);
                                        var bpToDel = db.BarProds.Where(x => x.barcode_id == dbbar.id).ToList().Union(nbpl.Where(x => x.barcode_id == dbbar.id)).ToList();
                                        var idsRange = bpToDel.Select(x => x.id).ToList();
                                        addFreeIdsToList(barprIds, idsRange);
                                        db.BarProds.RemoveRange(bpToDel);
                                        bpToDel.ForEach(x => nbpl.Remove(x));
                                    }
                                }
                            }
                            else
                            {
                                var bpToDel = db.BarProds.Where(x => x.barcode_id == barpr.barcode_id).ToList().Union(nbpl.Where(x => x.barcode_id == barpr.barcode_id)).ToList();
                                db.BarProds.RemoveRange(bpToDel);
                                bpToDel.ForEach(x => nbpl.Remove(x));
                                addFreeIdsToList(barprIds, bpToDel.Select(x => x.id).ToList());
                            }
                        }
                        //while (extDB is null)
                        //{
                        //    try
                        //    {
                        //        db.SaveChanges();
                        //        break;
                        //    }
                        //    catch (Exception e) { Console.WriteLine($"{nameof(AddProductBarcodes)}/del/: {e.Message}"); Task.Delay(3000).Wait(); continue; }
                        //}
                    //--end-- Deletion obsolete barcode and links to them
                }
                }
            return true;
        }
        public static bool AddProductImages(Product_SB_V2 pr, PISBContext extDB, List<int> imIds, List<int> imprIds, List<SB_Image> nil, List<SB_image_product> nipl)
        {
            if (GetDBName(typeof(SB_Image)) != "PISB" || GetDBName(typeof(SB_image_product)) != "PISB")
                return false;

            //--start-- Addition of the new Price
            lock (LSB_Images) lock (LSB_image_product)
                {
                    var db = extDB ?? new PISBContext();
                    //var db = new PISBContext();
                    if ((pr.images?.Count ?? 0) > 0)
                    {
                        //--start-- Addition new images and links to them

                        foreach (var im in pr.images!)
                        {
                            var dbim = db.Images.FirstOrDefault(x => x.url == im.url) ?? nil.FirstOrDefault(x => x.url == im.url);

                            if (dbim is null)
                            {
                                var nid = getIDforNewRecordFromFreeIdList(imIds);
                                dbim = new() { url = im.url, id = nid };
                                db.Images.Add(dbim);
                                nil.Add(dbim);
                            }
                            if ((db.ImProds.FirstOrDefault(x => x.product_sku == pr.sku && x.image_id == dbim.id) ?? nipl.FirstOrDefault(x => x.product_sku == pr.sku && x.image_id == dbim.id)) is null)
                            {
                                var nid = getIDforNewRecordFromFreeIdList(imprIds);
                                SB_image_product nip = new() { image_id = dbim.id, product_id = pr.id, product_sku = pr.sku ?? 0, id = nid };
                                db.ImProds.Add(nip);
                                nipl.Add(nip);
                            }
                            while (extDB is null)
                            {
                                try
                                {
                                    db.SaveChanges();
                                    break;
                                }
                                catch (Exception e) { Console.WriteLine($"{nameof(AddProductImages)}/add/: {e.Message}"); Task.Delay(3000).Wait(); continue; }
                            }
                        }
                        //--end-- Addition new images and links to them

                        //--start-- Deletion obsolete images and links to them
                        var imprs = db.ImProds.Where(x => x.product_id == pr.id).ToList().Union(nipl.Where(x => x.product_id == pr.id)).ToList();
                        foreach (var impr in imprs)  // Deletion obsolete images and links to them
                        {
                            var dbim = db.Images.FirstOrDefault(x => x.id == impr.image_id) ?? nil.FirstOrDefault(x => x.id == impr.image_id);
                            if (dbim is not null)
                            {
                                if (pr.images.FirstOrDefault(x => x.url == dbim.url) is null)
                                {
                                    if ((db.ImProds.Where(x => x.image_id == dbim.id && x.product_id != pr.id).ToList().Union(nipl.Where(x => x.image_id == dbim.id && x.product_id != pr.id)).ToList().Count() <= 0))
                                    {
                                        db.Images.Remove(dbim);
                                        nil.Remove(dbim);
                                        addFreeIdToList(imIds, dbim.id);
                                        var ipToDel = db.ImProds.Where(x => x.image_id == dbim.id).ToList().Union(nipl.Where(x => x.image_id == dbim.id)).ToList();
                                        var idsRange = ipToDel.Select(x => x.id).ToList();
                                        addFreeIdsToList(imIds, idsRange);
                                        db.ImProds.RemoveRange(ipToDel);
                                        ipToDel.ForEach(x => nipl.Remove(x));
                                    }
                                }
                            }
                            else
                            {
                                var ipToDel = db.ImProds.Where(x => x.image_id == impr.image_id).ToList().Union(nipl.Where(x => x.image_id == impr.image_id)).ToList();
                                db.ImProds.RemoveRange(ipToDel);
                                ipToDel.ForEach(x => nipl.Remove(x));
                                addFreeIdsToList(imprIds, ipToDel.Select(x => x.id).ToList());
                            }
                        }
                        while (extDB is null)
                        {
                            try
                            {
                                db.SaveChanges();
                                break;
                            }
                            catch (Exception e) { Console.WriteLine($"{nameof(AddProductImages)}/del/: {e.Message}"); Task.Delay(3000).Wait(); continue; }
                        }
                        //--end-- Deletion obsolete images and links to them
                    }
                }
            return true;
        }
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
            if (GetDBName(typeof(SB_Barcode)) != "PISB")
                return new List<int>();
            using var db = new PISBContext();

            var bid = db.Barcodes.AsNoTracking().Where(x => x.barcode == barcode).Select(x=>x.id).ToList();
            return bid;
        }
        public static Product_SB_V2? GetProductFromBarcodeId(int barcode_id)
        {
            if (GetDBName(typeof(SB_barcode_product)) != "PISB" || GetDBName(typeof(Product_SB_V2)) != "PISB")
                return null;
            using var db = new PISBContext();

            var sku = db.BarProds.FirstOrDefault(x => x.barcode_id == barcode_id)?.product_sku;
            var product = db.Products.FirstOrDefault(x => x.sku == sku);
            return product;
        }

        public static Product_SB_V2? GetProductFromSKU(long sku)
        {
            if (GetDBName(typeof(Product_SB_V2)) != "PISB")
                return null;
            using var db = new PISBContext();
            var product = db.Products.FirstOrDefault(x => x.sku == sku);
            return product;
        }

        public record PriceWithInfo(Price_SB_V2 price, double lat, double lon, string? retailer_name, string? retailer_logo_image, string? retailer_mini_logo_image);
        public static List<PriceWithInfo>? GetPricesFromSKU_WithRetailerAndStoreInfo(long sku)
        {
            if (GetDBName(typeof(Price_SB_V2)) != "PISB")
                return null;
            using var db = new PISBContext();
            var prices = (from p in db.Prices.AsNoTracking().Where(x => x.product_sku == sku)
                         join r in db.Retailers.AsNoTracking() on p.retailer equals r.id
                         join s in db.Stores.AsNoTracking() on p.store equals s.id
                         select new PriceWithInfo(p, s.lat, s.lon, r.name, r.logo_image, r.mini_logo_image)).ToList().OrderBy(x=>x.price.price).ToList();
            return prices;
        }
        public static List<string>? GetBarcodesFromSKU(long sku)
        {
            if (GetDBName(typeof(SB_Barcode)) != "PISB")
                return null;
            using var db = new PISBContext();
            var barcodes = db.BarProds.AsNoTracking().Where(x => x.product_sku == sku).Join(db.Barcodes,bp=>bp.barcode_id,b=>b.id,(bp,b)=> b.barcode).ToList();
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
            var names = db.Products.Select(x => new ProductName() { Id = x.sku ?? 0, name = x.name ?? "", namepp = PreProcessString(x.name ?? "") }).ToList();
            return names;
        }
        public static List<string>? GetImagesFromSKU(long sku)
        {
            if (GetDBName(typeof(SB_image_product)) != "PISB" || GetDBName(typeof(SB_Image)) != "PISB")
                return null;
            using var db = new PISBContext();

            var images = db.ImProds.AsNoTracking().Where(x => x.product_sku == sku).Join(db.Images, ip => ip.image_id, i => i.id, (im, i) => i.url).ToList();
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
            var stores = db.Stores.ToList();
            var max_id = stores.Max(x => x.id);
            return max_id;
        }
        public static bool AddOrUpdateStoresBatch(List<Store> storeBatch)
        {
            if (GetDBName(typeof(Store)) != "PISB")
                return false;
            lock (LSB_Stores)
            {
                using var db = new PISBContext();
                foreach (var store in storeBatch)
                {
                    var ob = db.Stores.FirstOrDefault(x => x.id == store.id);
                    if (ob is not null) db.Stores.Remove(ob);
                    db.Stores.Add(store);
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
