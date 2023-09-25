using SB_Parser_API.Models;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Web;
using static System.Web.HttpUtility;
using static SB_Parser_API.MicroServices.WebAccessUtils;
using static SB_Parser_API.MicroServices.DBSerices;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System.Reflection.Metadata;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SB_Parser_API.MicroServices
{
    public class SB_API
    {
        public const int errorAttempts = 5;
        static DateTime lastTokenInit { get; set; } = DateTime.MinValue;
        public const string get_API_Key_Url = "https://sbermarket.ru/"; // STOREFRONT_API_V3_CLIENT_TOKEN
        public static string get_retailers(int page, int per_page) => $"https://sbermarket.ru/api/retailers?page={page}&per_page={per_page}";
        public const string api_version = "3.0";
        static string client_token { get; set; } = "7ba97b6f4049436dab90c789f946ee2f";
        static object client_token_Lock { get; set; } = new();
        public const int productsPerPage = 24;
        public const int maxProductsPerRequest = 600;
        public readonly string[] sort_order_SB = { "price_asc", "price_desc", "unit_price_asc", "popularity"};
        public enum SortProductSB : int
        { price_asc = 0, price_desc = 1, unit_price_asc = 2, popularity = 3 }

        static readonly Regex client_token_get = new(@"^.+STOREFRONT_API_V3_CLIENT_TOKEN[^""]+""([^""]+).+");
        public static string GeoQueryURL(double lat, double lon) => $"https://sbermarket.ru/api/stores?lat={lat.ToString().Replace(",", ".")}&lon={lon.ToString().Replace(",", ".")}&shipping_method=delivery";
        //&include=closest_shipping_options%2Clabels%2Cretailer%2Clabel_store_ids&shipping_method=delivery" &shipping_method=pickup_from_store
        //public static string SearchQueryURL(string query, int store) => $"https://sbermarket.ru/api/stores/19961/products?q=4607161622308&page=1&per_page=15";
        public static string SearchQueryURL_V2(string query, string categoryId, int store = 1, int page = 1, int per_page = 24, SortProductSB sort = SortProductSB.price_asc) => $"https://sbermarket.ru/api/v2/products?q={UrlEncode(query)}&sid={store}&tid={categoryId}&page={page}&per_page={per_page}&sort={sort}";
        public static string CategoryURL_V2(int store = 1) => $"https://sbermarket.ru/api/v2/categories?depth=1&reset_cache=true&sid={store}";
        public static string StoreInfo(int store) => $"https://sbermarket.ru/api/stores/{store}";
        public static string StoreSetInfo(int page, int per_page) => $"https://sbermarket.ru/api/stores?page={page}&per_page={per_page}";
        public static string ProductInfo(long product_id) => $"https://sbermarket.ru/api/v2/products/{product_id}?sid=1";
        
        public static string MultiSearchQueryURL_V3(double lat, double lon, string query) => $"https://sbermarket.ru/api/v3/multisearches?lat={lat.ToString().Replace(",", ".")}&lon={lon.ToString().Replace(",", ".")}&q={UrlEncode(query)}&include[]=retailer";

        public static List<string> ImageFolders { get; set; } = new() { "width-auto", "size-64-64", "size-192-185", "size-210-210", "size-680-680", };

        //https://sbermarket.ru/api/categories?depth=1&reset_cache=true&store_id=1
        //https://sbermarket.ru/api/stores/19961/products?q=4607161622308&page=1&per_page=15
        //https://sbermarket.ru/api/v3/multisearches?lat=55.908399&lon=37.730014&q=молоко 3.2&include[]=retailer&include[]=closest_shipping_options&shipping_method=delivery


        public static void setUpSBToken(WebRequestOrder wro)
        {
            try
            {
                var cookie = wro.headers.FirstOrDefault(x => x.key == "cookie")?.value ?? "";
                cookie += cookie.Length > 0 ? ";" : "";
                wro.headers.Add(new() { key = "cookie", value = cookie + wro.proxy.getParametr("cookie") ?? "" });
            }
            catch (Exception e) { Console.WriteLine($"{nameof(setUpSBToken)}: {e.Message}"); }
        }
        public static (List<Product_SB_V2>?, List<Price_SB_V2>?, Meta_Products_SB_V2?) productSearch(string query, string category, int store = 1, Priority prio = Priority.Low, int page = 1, int per_page = 24, SortProductSB sort = SortProductSB.popularity)
        {
            var wro = new WebRequestOrder() { url = SearchQueryURL_V2(query, category, store, page, per_page, sort), needProxy= true, priority = prio };
            wro.contentKeys.Add("products");
            //wro.timeOut = 40000;
            //wro.needProxy = false;
            //wro.proxy = ProxyToDomainGet(new Proxy_to_Domain() { ip = $"185.15.172.212", port = $"3128", protocol = $"HTTP", domain = $"https://sbermarket.ru" }).FirstOrDefault()!;
            GetWebInfo(wro);
            var txt= wro.textResponse();
            var prods = JObject.Parse(txt!).SelectToken("$.products")?.ToObject<List<Product_SB_V2>>();//JsonConvert.DeserializeObject<List<Category>>(txt);
            var prices = JObject.Parse(txt!).SelectToken("$.products")?.ToObject<List<Price_SB_V2>>();//JsonConvert.DeserializeObject<List<Category>>(txt);
            var meta = JObject.Parse(txt!).SelectToken("$.meta")?.ToObject<Meta_Products_SB_V2>();//JsonConvert.DeserializeObject<List<Category>>(txt);
            return (prods, prices, meta);
        }
        public static List<Category_SB_V2>? categoryGet(int store = 1, Priority prio = Priority.Low)
        {
            var wro = new WebRequestOrder() { url = CategoryURL_V2(store), needProxy = true, priority = prio };
            wro.contentKeys.Add("categories");
            //wro.timeOut = 40000;
            //wro.needProxy = false;
            //wro.proxy = ProxyToDomainGet(new Proxy_to_Domain() { ip = $"185.15.172.212", port = $"3128", protocol = $"HTTP", domain = $"https://sbermarket.ru" }).FirstOrDefault()!;
            GetWebInfo(wro);
            var txt = wro.textResponse();
            try
            {
                var cats = JObject.Parse(txt!).SelectToken("$.categories")?.ToObject<List<Category_SB_V2>>();//JsonConvert.DeserializeObject<List<Category>>(txt);
                //ReferenceLoopHandling TypeNameHandling = TypeNameHandling.All
                //var jsonSerializerSettings = new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Serialize };
                //var txt1 = JsonConvert.SerializeObject(cats); //, typeof(List<Category_SB_V2>), jsonSerializerSettings
                return cats;
            }
            catch (Exception e) { Console.WriteLine(e.Message); return null; }
        }
        public static Store? storeInfoGet(int store, Priority prio = Priority.Low)
        {
            var wro = new WebRequestOrder() { url = StoreInfo(store), needProxy = true, priority = prio };
            wro.contentKeys.Add("store");
            wro.contentKeys.Add("error");
            GetWebInfo(wro);
            var txt = wro.textResponse();
            Store? sto = null;
            if (txt.Contains("store"))
            {
                try { sto = JObject.Parse(txt).SelectToken("$.store")?.ToObject<Store>(); return sto;}
                catch (Exception e) { Console.WriteLine(txt); Console.WriteLine(e.Message); }
            }
            return null;
        }

        public static StoreSet_SB? storeListInfoGet(int page,int per_page, Priority prio = Priority.Low)
        {
            var wro = new WebRequestOrder() { url = StoreSetInfo(page, per_page), needProxy = true, priority = prio };
            wro.contentKeys.Add("stores");
            wro.contentKeys.Add("error");
            GetWebInfo(wro);
            var txt = wro.textResponse();
            StoreSet_SB? sts = null;
            if (txt.Contains("stores"))
            {
                try { sts = JObject.Parse(txt).ToObject<StoreSet_SB>(); sts?.stores?.ForEach(x => { x.city_id = x._city_id; return; }); return sts; }  //sts = JObject.Parse(txt).ToObject<StoreSet_SB>(); return sts;
                catch (Exception e) { Console.WriteLine(txt); Console.WriteLine(e.Message); }
            }
            return null;
        }


        public static Product_Details_SB_V2? productInfoGet(long product_id, Priority prio = Priority.Low)
        {
            return  productInfoFrom_WRO(productInfoSendRequest(product_id, prio));
            /*
            var wro = new WebRequestOrder() { url = ProductInfo(product_id), needProxy = true, priority = prio };
            wro.contentKeys.Add("product");
            wro.contentKeys.Add("error");
            GetWebInfo(wro);
            var txt = wro.textResponse();
            Product_Details_SB_V2? prInfo = null;
            if (txt.Contains("product"))
            {
                try { prInfo = JObject.Parse(txt).SelectToken("$.product")?.ToObject<Product_Details_SB_V2>(); return prInfo; }
                catch (Exception e) { Console.WriteLine(txt); Console.WriteLine(e.Message); }
            }
            return null;
            */
        }
        public static WebRequestOrder productInfoSendRequest(long product_id, Priority prio = Priority.Low)
        {
            var wro = new WebRequestOrder() { url = ProductInfo(product_id), needProxy = true, priority = prio };
            wro.contentKeys.Add("product");
            wro.contentKeys.Add("error");
            GetWebInfoSetOrder(wro);
            return wro;
        }
        public static Product_Details_SB_V2? productInfoFrom_WRO(WebRequestOrder wro)
        {
            var txt = wro.textResponse();
            Product_Details_SB_V2? prInfo = null;
            if (txt.Contains("product"))
            {
                try { prInfo = JObject.Parse(txt).SelectToken("$.product")?.ToObject<Product_Details_SB_V2>(); return prInfo; }
                catch (Exception e) 
                { 
                    Console.WriteLine(txt); 
                    Console.WriteLine(e.Message); 
                }
            }
            return null;
        }
        //productInfoFrom_WRO

        public static List<Multi_Search_Result> productMultiSearch(double lat, double lon, string query, Priority prio = Priority.Low)
        {
            var wro = new WebRequestOrder() { url = MultiSearchQueryURL_V3(lat, lon, query), needProxy = true, priority = prio };
            wro.headers.Add(new() { key = nameof(api_version).Replace("_", "-"), value = api_version });
            wro.headers.Add(new() { key = nameof(client_token).Replace("_", "-"), value = client_token });
            wro.contentKeys.Add("[");
            //wro.timeOut = 40000;
            //wro.needProxy = false;
            //wro.proxy = ProxyToDomainGet(new Proxy_to_Domain() { ip = $"185.15.172.212", port = $"3128", protocol = $"HTTP", domain = $"https://sbermarket.ru" }).FirstOrDefault()!;
            GetWebInfo(wro);
            var txt = wro.textResponse();
            if (txt.Contains("error")) return new();
            var shopList = JsonConvert.DeserializeObject<List<Multi_Search_Result>>(txt) ?? new();
            //var shopList = JObject.Parse(txt!).ToObject<List<Multi_Search_Result>>() ?? new();
            return shopList;
        }

        public static HttpRequestMessage CreateRequest(string url)
        {
            var rUri = new Uri(url);
            var request = new HttpRequestMessage()
            {
                RequestUri = rUri,
                Method = HttpMethod.Get,
            };
            request.Headers.Add("cookie", "reachedTimer=1");
            request.Headers.Add("Referer", url);
            return request;
        }
        public static HttpRequestMessage CreateRequestAPI_3_0(string url)
        {
            if ((DateTime.Now - lastTokenInit).TotalDays > 1)
            {
                lock (client_token_Lock)
                {
                    var result = GetStringFromUrl(get_API_Key_Url, nameof(CreateRequestAPI_3_0) + $"('{url}')") ?? client_token;
                    client_token = client_token_get.Replace(result.Replace("\n", ""), "$1");
                    Console.WriteLine($"({DateTime.Now})--{nameof(client_token)}='{client_token}'");
                    lastTokenInit = DateTime.Now;
                }
            }
            var request=CreateRequest(url);
            request.Headers.Add(nameof(api_version).Replace("_","-"), api_version);
            request.Headers.Add(nameof(client_token).Replace("_", "-"), client_token);
            return request;
        }
        public static string? GetStringFromUrl(string url,string method)
        {
            Task<string> task=null!;
            HttpClient client = new HttpClient();
            int i;
            for (i = 0; i < errorAttempts; i++)
            {
                try
                {
                    task = client.GetStringAsync(url);
                    task.Wait();
                    break;
                }
                catch (Exception ex) { Console.WriteLine($"({DateTime.Now})--{method}:{ex.Message}"); }
            }
            if (i == errorAttempts)
                return null;

            return task?.Result;
        }
        public static string? GetStringFromRequest(HttpRequestMessage request, string method)
        {
            Task<HttpResponseMessage> task = null!;
            IEnumerable<string> cookies;
            HttpClient client = new HttpClient();
            string text=null!;
            int i;
            for (i = 0; i < errorAttempts; i++)
            {
                try
                {
                    task = client.SendAsync(request);
                    task.Wait();
                    var response = task.Result;
                    cookies = response.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;

                    //response.RequestMessage.Headers.GetType().GetProperty("From");

                    text = response.Content.ReadAsStringAsync().Result;
                    break;
                }
                catch (Exception ex) { Console.WriteLine($"({DateTime.Now})--{method}:{ex.Message}"); }
            }
            if (i == errorAttempts)
                return null;

            return text;
        }
        public static string? Retailers(int page, int per_page) => GetStringFromUrl(get_retailers(page, per_page), nameof(Retailers));
        public static string? NearbyStores(double lat, double lon) => GetStringFromRequest(CreateRequestAPI_3_0(GeoQueryURL(lat, lon)),nameof(NearbyStores));
    }
}
