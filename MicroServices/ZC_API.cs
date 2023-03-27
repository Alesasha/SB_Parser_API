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

//using AutoMapperApp.Models;

namespace SB_Parser_API.MicroServices
{
    public class ZC_API
    {
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
            ZC_User user = req.user ?? new() { id=0, info=""};
            long query_id = req.query_id ?? 0;

            var orbar = barcode;
            barcode = BarCodeCheck(barcode);

            string error="";
            if (barcode.Length <= 0) error = $"{{Error:'Invalid barcode -> [{orbar}]'}}"; 
            var bidl = GetBarcodeIdList(barcode);
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

            Products_List_From_Barcode_ZC productList = new(query_id, user.id, true, new List<Product_From_Barcode_ZC>(), "");

            foreach (var bid in bidl)
            {
                var prod = mapper.Map<Product_From_Barcode_ZC>(GetProductFromBarcodeId((int)bid));
                prod.barcodes = GetBarcodesFromSKU(prod.sku ?? 0);
                productList.products.Add(productInfoFromProductID(prod));
            }
            return productList;
        }

        public static Products_List_From_Barcode_ZC productInfoListFromTextQuery(ActiveRequest req)
        {

            string text_query = req.query ?? "";
            ZC_User user = req.user ?? new() { id = 0, info = "" };
            long query_id = req.query_id ?? 0;

            if(text_query.Replace(" ","").Length<=2)
                return new Products_List_From_Barcode_ZC(query_id, user.id, true, new(), $"The query '{text_query}' is too short");

            var frn = new FindRelevantNames();
            frn.SetSample(text_query, req.take);
            Parallel.ForEach(productNames, frn.FindBestProductNames);
            frn.TopNames = frn.TopNames.OrderByDescending(x => x.rate).ToList();

            if (user.id <= 0)
                AddNewUser(user);
            if (query_id <= 0)
            {
                var query = new Query_ZC() { id = query_id, user_id = user.id, query = text_query, query_type = QueryType.BarcodeSearch, dt = DateTime.Now };
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

            foreach (var nam in frn.TopNames)
            {
                var prod = mapper.Map<Product_From_Barcode_ZC>(GetProductFromSKU(nam.Id));
                prod.barcodes = GetBarcodesFromSKU(prod.sku ?? 0);
                prod.sort_rate = prod.match_rate = (double)nam.rate / avgSearchRate;
                var pi = productInfoFromProductID(prod);
                if (pi.prices?[0].price is not null)
                {
                    double gpu =  (double) (pi.grams_per_unit ?? 1);
                    gpu = gpu == 0 ? 1 : gpu;
                    avgPriceRate += (pi.prices[0].price ?? 0) * 1000 / gpu;
                    pCount += 1;
                }
                avgPriceRate += (pi.prices?[0].price ?? 1000000);
                products.Add(pi);
            }
            pCount = pCount == 0 ? 1 : pCount;
            avgPriceRate /= pCount;
            products.ForEach(x => {
                double gpu = (double)(x.grams_per_unit ?? 1);
                gpu = gpu == 0 ? 1 : gpu;
                double PricePU = (x.prices?[0].price ?? 0) * 1000 / gpu;
                PricePU = PricePU == 0 ? 1 : PricePU;
                x.price_rate = avgPriceRate / PricePU;
                x.sort_rate *= x.price_rate;
            });
            products = products.OrderByDescending(x => x.sort_rate).ToList();
            Products_List_From_Barcode_ZC productList = new(query_id, user.id, true, products, "");
            return productList;
        }

        public static Product_From_Barcode_ZC productInfoFromProductID(Product_From_Barcode_ZC product)
        {
            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Price_SB_V2, Price_ZC>().ReverseMap();
                cfg.CreateMap<Property_Product_SB_V2, Property_Product_DB>().ReverseMap();
            }).CreateMapper();

            var pricesWI = GetPricesFromSKU_WithRetailerAndStoreInfo(product.sku ?? 0);
            List<Price_ZC> prices = new();

            pricesWI?.ForEach(x => {
                var p = mapper.Map<Price_ZC>(x.price); p.lat = x.lat; p.lon = x.lon; p.retailer_name = x.retailer_name;
                p.retailer_logo_url = x.retailer_logo_image; p.retailer_mini_logo_url = x.retailer_mini_logo_image; prices.Add(p);
            });
            prices= prices.OrderBy(x => x.price).ToList();

            product.prices = prices;
            product.image_urls = GetImagesFromSKU(product.sku ?? 0);
            //JsonConvert.SerializeObject
            var propDB_List = GetProductPropertiesFromSKU(product.sku ?? 0) ?? new();
            List<Property_Product_SB_V2> properties;
            if ((propDB_List?.Count ?? 0) <= 0)
            {
                var productInfo = productInfoGet(product.id);
                properties = productInfo?.properties ?? new();
                var mtaxon = JsonConvert.SerializeObject(productInfo?.main_taxon).Replace("\"", "'");
                properties.Add(new Property_Product_SB_V2() { name = "main_taxon", presentation = "Категория", value = mtaxon });
                properties.Add(new Property_Product_SB_V2() { name = "brand", presentation = "Brand", value = productInfo?.brand_name });
                properties.Add(new Property_Product_SB_V2() { name = "brand_id", presentation = "Brand_ID", value = productInfo?.brand_id.ToString() });
                properties.Add(new Property_Product_SB_V2() { name = "description", presentation = "Описание товара", value = productInfo?.description });
                propDB_List = new();
                properties.ForEach((x => propDB_List.Add(mapper.Map<Property_Product_DB>(x))));
                propDB_List.ForEach(x => { x.product_id = product.id; x.product_sku = product.sku ?? 0; });
                Task.Factory.StartNew(() => AddProductProperties(propDB_List), TaskCreationOptions.LongRunning);
            }
            else
            {
                properties = new();
                propDB_List?.ForEach(x => properties.Add(mapper.Map<Property_Product_SB_V2>(x)));
            }
            product.properties = properties;
            return product;
        }
    }
}
