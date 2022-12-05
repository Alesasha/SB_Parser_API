using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SB_Parser_API.Models;
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
using System.Diagnostics;
//using System.Web.Mvc;
//using System.Data.Objects.ObjectQuery;

namespace SB_Parser_API.MicroServices
{
    public static class SearchViaBarcode
    {
        public static async Task GetShops(double lat, double lon, double radius)
        {
            List<Store> storesWithinRadius=null!;
            using (var db = new PISBContext())
            {
                var stores = db.Stores.ToList();
                storesWithinRadius = stores.Where(x => (x.distanceToMyPoint=CalcDistance(lat, lon, x.lat, x.lon)) <= radius).ToList();
                storesWithinRadius = storesWithinRadius.OrderBy(x => x.distanceToMyPoint).ToList();
            }
            var len = storesWithinRadius.Where(x => x.name!.Contains("ЛЕНТА")).ToList();
            //return;

              HttpClient client = new HttpClient();
            //https://sbermarket.ru/api/retailers
            //https://sbermarket.ru/auchan/search?keywords=4607161622308&sid=245
            //https://sbermarket.ru/api/stores/245/products/slivki-prostokvashino-pitievye-ul-trapasterizovannye-10-bzmzh-350-ml-eed21be
            //https://sbermarket.ru/api/stores/19961/products/slivki-pitievye-prostokvashino-ul-trapasterizovannye-10-350-ml-7f1ee92
            //https://sbermarket.ru/api/stores/19961/products?q=4607161622308&page=1&per_page=15
            //string rUrl = "https://sbermarket.ru/api/stores?lat=55.900067&lon=37.736743&shipping_method=delivery"; //&include=closest_shipping_options%2Clabels%2Cretailer%2Clabel_store_ids&shipping_method=delivery  &shipping_method=pickup_from_store
            string rUrl = $"https://sbermarket.ru/api/stores?lat={lat.ToString().Replace(",",".")}&lon={lon.ToString().Replace(",", ".")}&shipping_method=delivery";//&include=closest_shipping_options%2Clabels%2Cretailer%2Clabel_store_ids&shipping_method=delivery"; //&include=closest_shipping_options%2Clabels%2Cretailer%2Clabel_store_ids&shipping_method=delivery  &shipping_method=pickup_from_store
            var rUri = new Uri(rUrl); //4607053479126 4606419013691 4607161622308
            var request = new HttpRequestMessage()
            {
                RequestUri = rUri,
                Method = HttpMethod.Get,
            };
            request.Headers.Add("api-version", "3.0");
            request.Headers.Add("client-token", "7ba97b6f4049436dab90c789f946ee2f");
            request.Headers.Add("cookie", "reachedTimer=1");
            request.Headers.Add("Referer", rUrl);
            var response = await client.SendAsync(request);
            IEnumerable<string> cookies = response.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;
            var text = response.Content.ReadAsStringAsync().Result;

            var roorRet = "";// SB_API.Retailers();
            text = ""; // SB_API.NearbyStores(lat, lon);

            
            /*
            var o = (JObject) JsonConvert.DeserializeObject(text)!;
            var id = (int) o.SelectToken("$.product.id")!;
            //var id1 = (int) (o as dynamic).product.id.Value;
            var jo = JObject.Parse(text);
            //var id = (int) o.product.id.Value;
            */

            //var jo = JObject.Parse(text).SelectToken("$.retailers")!; //mini_logo_image

            //foreach (var o in jo)
            //{
            //(o as JObject).Add("mini_logo_image", o.SelectToken("$..mini_logo_image"));
            //}
            text = $"{{x:{text}}}";
            var ret = new List<int>();
            // JObject.Parse(text).SelectTokens("$..store_id").ToList().ForEach(x => ret.Add((int)x));

            rUrl = "https://sbermarket.ru/api/stores/245/products?q=4600699503903";// &private_filters=with_similar:false,in_stock:false"; //19961 //1949
            //rUrl = "https://sbermarket.ru/api/multisearches?lat=55.89551&lon=37.705562&q=4600699503903";
            //rUrl = "https://sbermarket.ru/api//stores/1/products/slivki-pitievye-prostokvashino-ul-trapasterizovannye-10-bzmzh-350-ml-a0cbcaf";//4543 //stores/245/
            rUri = new Uri(rUrl); //rUrl
            request = new HttpRequestMessage()
            {
                RequestUri = rUri,
                Method = HttpMethod.Get,
            };
            request.Headers.Add("api-version", "3.0");
            request.Headers.Add("client-token", "7ba97b6f4049436dab90c789f946ee2f");
            request.Headers.Add("cookie", "ngenix_jscv_cd881f1695eb=cookie_signature=ZeixES5EpgKRcK7%2BdSy2JjV7eIc%3D&cookie_expires=1667131342"); //reachedTimer=1 1666358476

            //request.Headers.Add("referer", "referer: https://sbermarket.ru/auchan?sid=245"); //rUrl
            request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.5112.124 YaBrowser/22.9.4.863 Yowser/2.5 Safari/537.36");
            //request.Headers.Add("sec-ch-ua", "Chromium\";v=\"106\", \"Google Chrome\";v=\"106\", \"Not;A=Brand\";v=\"99");

            response = await client.SendAsync(request);
            var cookiess = response.Headers?.SingleOrDefault(header => header.Key == "Set-Cookie").Value;
            var sc1 = cookiess?.FirstOrDefault();
            var sc2 = cookiess?.Where(x => x.Contains("ngenix_jscc_66dcf4")).FirstOrDefault();
            text = response.Content.ReadAsStringAsync().Result;
            var data = sc2?.Split("&");
            
            
            var engine = new Jurassic.ScriptEngine();
            var jsres = engine.Evaluate("5 * 10 + 2");
            Console.WriteLine(jsres);

            var ob = (JObject)JsonConvert.DeserializeObject(text)!;


            //var ret = jo.ToObject<List<Retailer>>();

            //var ret = JsonConvert.DeserializeObject<Root>(text);
            text += "";

            Console.WriteLine($"lat={lat}, lon={lon}, {nameof(radius)}={radius}");
        }
        public static async Task CollectShops()
        {
            HttpResponseMessage? response = null;
            HttpRequestMessage? request = null;
            var handler = new HttpClientHandler()
            {
                Proxy = new WebProxy(new Uri($"http://46.42.16.245:31565")),
                UseProxy = true,
            };

            HttpClient client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromMilliseconds(20000);
            int imax = 0;
            using (var db = new PISBContext())
            {
                var stores = db.Stores.ToList();
                imax=stores.Max(x=>x.id);

            }
            int emptyLinks = 0;
            for (var i = imax + 1; emptyLinks < 100 && i < 100000; i++)
            {
                string rUrl = $"https://sbermarket.ru/api/stores/{i}";
                var rUri = new Uri(rUrl);
                string text;
                while (true)
                {
                    try
                    {
                        request = new HttpRequestMessage()
                        {
                            RequestUri = rUri,
                            Method = HttpMethod.Get,
                        };
                        response = await client.SendAsync(request);
                        IEnumerable<string> cookies = response.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;
                        text = response.Content.ReadAsStringAsync().Result;
                        break;
                    }
                    catch (Exception e) { Console.WriteLine($"Call Point 001: {e.Message}"); request?.Dispose(); await Task.Delay(3000); continue; }
                }

                if (text.Contains("error"))
                {
                    emptyLinks++;
                    continue;
                }

                var sto = JObject.Parse(text).SelectToken("$.store")?.ToObject<Store>();

                using (var db = new PISBContext())
                {


                    var stores = db.Stores.ToList();
                    // добавляем их в бд

                    var ob = stores.FirstOrDefault(x => x.id == sto!.id)!;

                    if (ob != null) db.Stores.Remove(ob);
                    db.Stores.Add(sto!);
                    while (true)
                    {
                        try
                        {
                            db.SaveChanges();
                            break;
                        }
                        catch (Exception e) { Console.WriteLine($"Call Point 002: {e.Message}"); await Task.Delay(3000); continue; }
                    }
                    Console.Clear();
                    Console.WriteLine("Объекты успешно сохранены");

                    // получаем объекты из бд и выводим на консоль
                    stores = db.Stores.OrderBy(x => x.id).ToList();
                    Console.WriteLine("Список объектов:");
                    foreach (var s in stores)
                    {
                        Console.WriteLine($"{s.id}.{s.name} - {s.city}");
                    }
                }
                emptyLinks = 0;
                await Task.Delay(2000);
            }

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









//request.Headers.Add("user-agent", "Mozilla / 5.0(Windows NT 10.0; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 102.0.0.0 Safari / 537.36";
//request.Headers.Add("api-version", "3.0");
//request.Headers.Add("client-token", "7ba97b6f4049436dab90c789f946ee2f");
//request.Headers.Add("accept","application / json, text / plain, */*");
//request.Headers.Add("accept-language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
//request.Headers.Add("cache-control", "no-cache");
//request.Headers.Add("cookie", "reachedTimer=1");
//request.Headers.Add("is-storefront-ssr", "false");
//request.Headers.Add("pragma", "no-cache");
//request.Headers.Add("sec-ch-ua", "\" Not A;Brand\";v=\"99\", \"Chromium\";v=\"102\", \"Google Chrome\";v=\"102\"");
//request.Headers.Add("sec-ch-ua-mobile", "?0");
//request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
//request.Headers.Add("sec-fetch-dest", "empty");
//request.Headers.Add("sec-fetch-mode", "cors");
//request.Headers.Add("sec-fetch-site", "same-origin");
//request.Headers.Add("Referer", "https://sbermarket.ru/auchan/slivki-prostokvashino-pitievye-ul-trapasterizovannye-10-bzmzh-350-ml-eed21be");
//request.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");