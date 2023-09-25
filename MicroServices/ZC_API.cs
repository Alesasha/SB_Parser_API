using SB_Parser_API.Models;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Web;
using static System.Web.HttpUtility;
using static SB_Parser_API.MicroServices.WebAccessUtils;
using static SB_Parser_API.MicroServices.DBSerices;
using static SB_Parser_API.MicroServices.Utils;
using static SB_Parser_API.MicroServices.SB_API;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System.Reflection.Metadata;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using AutoMapper;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch;
using System.Linq;

//using AutoMapperApp.Models;

namespace SB_Parser_API.MicroServices
{
    public class ZC_API
    {
        public const int PeriodInDaysWhenPriceIsValid = 30;
        public static ActiveRequest CreateActiveRequest(HttpRequest Request, HttpResponse Response)
        {
            var req_info_url = Request.RouteValues;
            var query_info_url = Request.Query;
            Dictionary<string, string> req_info_dict;
            Dictionary<string, string> user_dict = new();

            if (Request.HasJsonContentType())
            {
                var stream = new StreamReader(Request.Body);
                var req_info_json = stream.ReadToEnd();
                req_info_dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(req_info_json) ?? new();
            }
            else
                req_info_dict = new();

            foreach (var o in req_info_url.Where(x => x.Value is not null))
            {
                if (!req_info_dict.TryGetValue(o.Key, out var val) || val is null)
                    req_info_dict[o.Key] = UrlDecode((string)o.Value!);
                if (o.Key.Contains("user") && !o.Key.Contains("user_id"))
                    user_dict[o.Key] = UrlDecode((string)o.Value!);
            }
            foreach (var o in query_info_url)
            {
                if (!req_info_dict.TryGetValue(o.Key, out var val) || val is null)
                    req_info_dict[o.Key] = UrlDecode((string)o.Value!);
                if (o.Key.Contains("user") && !o.Key.Contains("user_id"))
                    user_dict[o.Key] = UrlDecode((string)o.Value!);
            }
            var nrs = JsonConvert.SerializeObject(req_info_dict);

            var newRequest = JsonConvert.DeserializeObject<ActiveRequest>(JsonConvert.SerializeObject(req_info_dict)) ?? new();
            newRequest.user = new ZC_User() { id = newRequest?.user_id ?? 0, info = JsonConvert.SerializeObject(user_dict) };
            return newRequest!;
        }
        public static Products_List_From_Barcode_ZC productInfoListFromBarcode(ActiveRequest req)
        {
            string barcode = req.barcode ?? "";
            ZC_User user = req.user ?? new() { id = 0, info = "" };
            long query_id = req.query_id ?? 0;

            var orbar = barcode;
            barcode = BarCodeCheck(barcode);

            string error = "";
            List<int>? bidl = new();
            if (barcode.Length <= 0) error = $"{{Error:'Invalid barcode -> [{orbar}]'}}";
            else
            {
                //var bar = barcode;
                bidl = GetBarcodeIdList(barcode);
                //while (bar[0] == '2' && bidl.Count() <= 0 && bar.Length > 2)
                //{
                //    bar = bar.Remove(bar.Length - 1);
                //    bidl = GetBarcodeIdList(bar);
                //}
            }
            if (error.Length <= 0 && bidl.Count() <= 0) error = $"{{Error:'Product not found (barcode:[{orbar}])'}}";
            if (error.Length > 0)
                return new Products_List_From_Barcode_ZC(query_id, user.id, true, new(), error);

            if (user.id <= 0)
                AddNewUser(user);
            if (query_id <= 0)
            {
                var query = new Query_ZC() { id = query_id, user_id = user.id, query = barcode, query_type = QueryType.BarcodeSearch, dt = DateTime.Now };
                AddNewQuery(query);
                query_id = query.id;
            }

            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Product_SB_V2, Product_From_Barcode_ZC>().ReverseMap();
            }).CreateMapper();


            var products = new List<Product_From_Barcode_ZC>();

            foreach (var bid in bidl)
            {
                var prodL = GetProductsFromBarcodeId(bid);
                foreach (var p in prodL)
                {
                    var prod = mapper.Map<Product_From_Barcode_ZC>(p);
                    prod.prices = prod.prices?.OrderBy(x => x.price).ToList();
                    prod.barcodes = BarCodeCheckList(GetBarcodesFromSKU(prod.sku ?? 0));
                    products.Add(productInfoFromProductID(prod, req.properties ?? true));
                }
            }
            products = ImproveProdutList(products, req);

            products = products.OrderBy(x => (x.price_rate = x.sort_rate = (x.prices?[0].price ?? 1000000) / (x.grams_per_unit ?? 1))).ToList();
            Products_List_From_Barcode_ZC productList = new(query_id, user.id, true, products, "");
            return productList;
        }

        public static List<Product_From_Barcode_ZC> ImproveProdutList(List<Product_From_Barcode_ZC> products, ActiveRequest req)
        {
            var NaNCheck = (double d) => double.IsNaN(d) ? 0 : d;

            List<Store>? localStoreList = null;
            if (req.radius is not null && req.lat is not null && req.lon is not null)
                localStoreList = RegsStoresList.Where(x => (x.distanceToMyPoint = CalcDistance((double)req.lat, (double)req.lon, x.lat, x.lon)) <= req.radius).ToList();

            products.ForEach(p => { p.prices?.RemoveAll(x => x.dt is null || (DateTime.Now - (DateTime)x.dt).Days >= PeriodInDaysWhenPriceIsValid); });
            products.RemoveAll(x => (x.prices?.Count ?? 0) <= 0);
            products.ForEach(p => {
                p.prices ??= new();
                if (p.prices.Count <= 0) p.prices.Add(new() { price = 1000000 });
                p.prices[0].price ??= 1000000;
                p.price_rate = NaNCheck(p.price_rate) * (p.prices[0].price ?? 1000000);
                p.match_rate = NaNCheck(p.match_rate);
                p.sort_rate = NaNCheck(p.sort_rate);
                p.prices?.ForEach(x =>
                {
                    if ((DateTime.Now - (x.discount_ends_at ?? DateTime.Parse("01.01.1971"))).Days > 1)
                    {
                        x.price = x.original_price;
                        x.unit_price = x.original_unit_price;
                    }
                });
                p.prices = p.prices?.OrderBy(x => x.price).ToList();
                p.price_rate /= (p.prices?[0].price ?? 1000000);
                p.sort_rate = p.price_rate * p.match_rate;
            });
            products = products.OrderByDescending(x => x.sort_rate).ToList();
            var ProdutsComparer = new Product_From_Barcode_ZC_Comparer();
            for (var i = 0; i < products.Count; i++)
            {
                products[i].prices ??= new();
                products[i].image_urls ??= new();
                for (var j = i + 1; j < products.Count; j++)
                {
                    if (ProdutsComparer.Equals(products[i], products[j]))
                    {
                        products[j].prices ??= new();
                        products[j].image_urls ??= new();
                        //products[i].prices!.AddRange(products[j].prices!);
                        products[j].prices!.ForEach(x => { if (products[i].prices!.Where(y => y.price == x.price && y.retailer == x.retailer).Count() <= 0) products[i].prices!.Add(x); });
                        products[i].image_urls!.AddRange(products[j].image_urls!);
                        products[i].image_urls = products[i].image_urls!.Distinct().ToList();
                        products.RemoveAt(j--);
                    }
                }
                var localPrices = new List<Price_ZC>();
                products[i].prices?.ForEach(x => { 
                    x.is_flagman = true;
                    if (localStoreList is not null)
                    {
                        var lsrl = localStoreList.Where(y => x.retailer == y.retailer_id).ToList();
                        lsrl.ForEach(z => {
                            if (x.store != z.id)
                            {
                                localPrices.Add(x with
                                {
                                    store = z.id,
                                    is_local = true,
                                    is_flagman = false,
                                    lat = z.lat,
                                    lon = z.lon,
                                    distanceToMyPoint = z.distanceToMyPoint,
                                    address = z.full_address,
                                    stock = 0
                                }); ;
                            }
                            else
                            {
                                x.is_local = true;
                                x.distanceToMyPoint = z.distanceToMyPoint;
                            }

                        });
                    }
                });
                products[i].prices ??= new();
                products[i].prices!.AddRange(localPrices);
                products[i].prices = products[i].prices!.OrderBy(x => x.price).ThenBy(x => x.retailer).ThenByDescending(x => x.is_flagman).ThenBy(x=> x.distanceToMyPoint ?? 0).ToList();
            }
            return products;
        }

        public static Products_List_From_Barcode_ZC productInfoListFromTextQuery(ActiveRequest req)
        {

            string text_query = req.query ?? "";
            req.take ??= 30;

            if (BarCodeCheck(text_query).Length > 0)
            {
                req.barcode = BarCodeCheck(text_query);
                return productInfoListFromBarcode(req);
            }

            ZC_User user = req.user ?? new() { id = 0, info = "" };
            long query_id = req.query_id ?? 0;

            if(text_query.Replace(" ","").Length<=2)
                return new Products_List_From_Barcode_ZC(query_id, user.id, true, new(), $"The query '{text_query}' is too short");

            var howManyTimes = text_query.Length <= 25 ? 4 : 1;

            var frn = new FindRelevantNames();
            frn.SetSample(text_query, text_query.Length <= 25 ? req.take * 4 : req.take);
            Parallel.ForEach(productNames, frn.FindBestProductNames);
            frn.TopNames = frn.TopNames.OrderByDescending(x => x.rate).ToList();
            frn.TopNames = frn.TopNames[(int)req.take - 1].rate <= 9000 ? frn.TopNames.Take((int)req.take).ToList() : frn.TopNames.Where(x => x.rate > 9000).OrderByDescending(x => x.rate).ToList();

            if (user.id <= 0)
                AddNewUser(user);
            if (query_id <= 0)
            {
                var query = new Query_ZC() { id = query_id, user_id = user.id, query = text_query, query_type = QueryType.TextSearch, dt = DateTime.Now };
                AddNewQuery(query);
                query_id = query.id;
            }

            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Product_SB_V2, Product_From_Barcode_ZC>().ReverseMap();
            }).CreateMapper();

            var products = new List<Product_From_Barcode_ZC>();

            double avgSearchRate = frn.TopNames.Select(x => x.rate).Average();
            double avgPriceRate = 0;
            double pCount = 0;

            //PISB_DB_Cache cache = new();
            //cache.FillCache(frn.TopNames.Select(x => x.Id).ToList());

            foreach (var nam in frn.TopNames)
            {
                var prod = mapper.Map<Product_From_Barcode_ZC>(GetProductFromSKU(nam.Id));
                prod.barcodes = GetBarcodesFromSKU(prod.sku ?? 0);
                prod.sort_rate = prod.match_rate = (double)nam.rate / avgSearchRate;
                var pi = productInfoFromProductID(prod,req.properties ?? true);
                if (pi is null) continue;
                pi.prices ??= new();
                if (pi.prices.Count() == 0) { pi.prices.Add(new() { price = 1000000 }); }
                //if (pi.prices?[0]?.price is not null)
                //{
                double gpu =  (double) (pi.grams_per_unit ?? 1);
                gpu = gpu == 0 ? 1 : gpu;
                pi.price_rate = (pi.prices[0].price ?? 0) * 1000 / gpu;
                pi.price_rate = pi.price_rate == 0 ? 1 : pi.price_rate;
                avgPriceRate += pi.price_rate;
                pCount += 1;
                //}
                //avgPriceRate += (pi?.prices?[0].price ?? 1000000);
                products.Add(pi!);
            }
            pCount = pCount == 0 ? 1 : pCount;
            avgPriceRate /= pCount;
            products.ForEach(x => {
                //double gpu = (double)(x.grams_per_unit ?? 1);
                //gpu = gpu == 0 ? 1 : gpu;
                //double PricePU = (x.prices?[0].price ?? 0) * 1000 / gpu;
                //PricePU = PricePU == 0 ? 1 : PricePU;
                x.price_rate = avgPriceRate / x.price_rate; //PricePU;
                x.sort_rate *= x.price_rate;
            });
            products = products.OrderByDescending(x => x.sort_rate).ToList();
            products = ImproveProdutList(products, req);
            products = products.OrderByDescending(x => x.sort_rate).Take((int)req.take).ToList();
            Products_List_From_Barcode_ZC productList = new(query_id, user.id, true, products, "");
            return productList;
        }

        public static Product_From_Barcode_ZC productInfoFromProductID(Product_From_Barcode_ZC product, bool needProperties = true)
        {
            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Price_SB_V2, Price_ZC>().ReverseMap();
                cfg.CreateMap<Property_Product_SB_V2, Property_Product_DB>().ReverseMap();
            }).CreateMapper();

            var pricesWI = GetPricesFromSKU_WithRetailerAndStoreInfo(product.sku ?? 0);
            List<Price_ZC> prices = new();

            pricesWI?.ForEach(x => {
                var p = mapper.Map<Price_ZC>(x.price); p.lat = x.lat; p.lon = x.lon; p.address = x.address; p.retailer_name = x.retailer_name;
                p.retailer_logo_url = x.retailer_logo_image; p.retailer_mini_logo_url = x.retailer_mini_logo_image; prices.Add(p);
            });
            prices = prices.OrderBy(x => x.price).ToList();

            product.prices = prices;
            product.image_urls = GetImagesFromSKU(product.sku ?? 0);
            product.properties = null;
            if (needProperties)
            {
                var propDB_List = GetProductPropertiesFromSKU(product.sku ?? 0) ?? new();
                List<Property_Product_SB_V2> properties;
                properties = new();
                propDB_List?.ForEach(x => properties.Add(mapper.Map<Property_Product_SB_V2>(x)));
                product.properties = properties;
            }
            return product;
        }
        public static List<Property_Product_DB> productPropertyFromProductInfo(Product_Details_SB_V2? productInfo)
        {
            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Property_Product_SB_V2, Property_Product_DB>().ReverseMap();
            }).CreateMapper();

            var properties = productInfo?.properties ?? new();
            var mtaxon = JsonConvert.SerializeObject(productInfo?.main_taxon).Replace("\"", "'");
            properties.Add(new Property_Product_SB_V2() { name = "main_taxon", presentation = "Категория", value = mtaxon });
            properties.Add(new Property_Product_SB_V2() { name = "brand", presentation = "Brand", value = productInfo?.brand_name });
            properties.Add(new Property_Product_SB_V2() { name = "brand_id", presentation = "Brand_ID", value = productInfo?.brand_id.ToString() });
            properties.Add(new Property_Product_SB_V2() { name = "description", presentation = "Описание товара", value = productInfo?.description });
            List<Property_Product_DB> propDB_List = new();
            properties.ForEach((x => propDB_List.Add(mapper.Map<Property_Product_DB>(x))));
            propDB_List.ForEach(x => { x.product_id = productInfo?.id ?? 0; x.product_sku = productInfo?.sku ?? 0; });
            return propDB_List;
        }

        //______________________For Dima___________
        public static Products_List_From_Barcode_Table_ZC convert_PLFB_to_table(Products_List_From_Barcode_ZC productInfo)
        {
            var columns = new List<string>() {
                "id",
                "sku",
                "name",
                "human_volume",
                "volume",
                "volume_type",
                "items_per_pack",
                "price_type",
                "grams_per_unit",
                "score",
                "sort_rate",
                "match_rate",
                "price_rate",
                "image_urls",
                "barcodes",

                "price_datetime",
                "retailer_id",
                "retailer_name",
                "retailer_logo_url",
                "retailer_mini_logo_url",
                "store_id",
                "store_lat",
                "store_lon",
                "store_address",
                "price",
                "original_price",
                "discount",
                "discount_ends_at",
                "unit_price",
                "original_unit_price",
                "stock"
            };
            var propertiesStartPosition = columns.Count;
            var rows = new List<List<object>>();

            foreach (var product in productInfo.products) 
            {
                var rowBlank= new List<object>()
                {
                    product.id,
                    product.sku!,
                    product.name!,
                    product.human_volume!,
                    product.volume!,
                    product.volume_type!,
                    product.items_per_pack!,
                    product.price_type!,
                    product.grams_per_unit!,
                    product.score!,
                    product.sort_rate!,
                    product.match_rate!,
                    product.price_rate!,
                    string.Join(";", product.image_urls!),
                    string.Join(";", product.barcodes!)
                };
                var pisp = rowBlank.Count;
                while (rowBlank.Count < propertiesStartPosition) rowBlank.Add(null!);
                while (rowBlank.Count < columns.Count) rowBlank.Add(null!);
                foreach (var prop in product.properties!) 
                {
                    if (prop.presentation is null)
                        continue;
                    int ind = columns.IndexOf(prop.presentation);
                    if (ind >= 0)
                    {
                        rowBlank[ind] = prop.value!;
                    }
                    else
                    {
                        columns.Add(prop.presentation);
                        rowBlank.Add(prop.value!);
                    }
                }

                foreach (var price in product.prices!) 
                {
                    var newRow = rowBlank.ToList();
                    var pi = pisp;
                    newRow[pi++] = price.dt!;
                    newRow[pi++] = price.retailer!;
                    newRow[pi++] = price.retailer_name!;
                    newRow[pi++] = price.retailer_logo_url!;
                    newRow[pi++] = price.retailer_mini_logo_url!;
                    newRow[pi++] = price.store!;
                    newRow[pi++] = price.lat!;
                    newRow[pi++] = price.lon!;
                    newRow[pi++] = price.address!;
                    newRow[pi++] = price.price!;
                    newRow[pi++] = price.original_price!;
                    newRow[pi++] = price.discount!;
                    newRow[pi++] = price.discount_ends_at!;
                    newRow[pi++] = price.unit_price!;
                    newRow[pi++] = price.original_unit_price!;
                    newRow[pi++] = price.stock!;
                    rows.Add(newRow);
                }
            }
            foreach (var row in rows)
                while (row.Count < columns.Count) row.Add(null!);

            var table_responce = new Products_List_From_Barcode_Table_ZC(productInfo.query_id, productInfo.user_id, productInfo.isRequestCompleted, columns, rows, productInfo.error);

            return table_responce;
        }

    }
}
