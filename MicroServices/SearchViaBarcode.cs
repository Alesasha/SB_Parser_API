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
            request.Headers.Add("cookie", "ngenix_jscv_cd881f1695eb=cookie_expires=1670260316&cookie_signature=8n0fruhtWkYQt22oAykPDEUc8ww%3D"); //reachedTimer=1 1666358476

            //request.Headers.Add("referer", "referer: https://sbermarket.ru/auchan?sid=245"); //rUrl
            request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.5112.124 YaBrowser/22.9.4.863 Yowser/2.5 Safari/537.36");
            //request.Headers.Add("sec-ch-ua", "Chromium\";v=\"106\", \"Google Chrome\";v=\"106\", \"Not;A=Brand\";v=\"99");

            response = await client.SendAsync(request);
            var cookiess = response.Headers?.SingleOrDefault(header => header.Key == "Set-Cookie").Value;
            var sc1 = cookiess?.FirstOrDefault();
            var sc2 = cookiess?.Where(x => x.Contains("ngenix_jscc_66dcf4")).FirstOrDefault();
            if (sc2 is not null)
            {
                //ngenix_jscc_66dcf4=challenge_complexity=10&challenge_url=%2Fjs-challenge-validation-8d5236eb82b5658ff0ce4a4c55f9833b&request_addr=195.170.35.108&
                //request_id=3c5bd5995b8737cc2605eb0244998c70&challenge_signature=f%2B%2Fjt9Kq7lZGWAK0ZLUc55z5eQY%3D&challenge_cookie_expires=1670242450&
                //verification_cookie_expires=1670245930; Expires=Mon, 05 Dec 2022 12:14:10 GMT; Domain=sbermarket.ru; Path=/

                var rnd = new Random();
                //sc2 = UrlDecode(str2);
                var sc2c = sc2.Replace("ngenix_jscc_66dcf4=", "");
                var data = sc2c.Split("&");
                var ch_url = "https://sbermarket.ru"+HttpUtility.UrlDecode(data.Where(x => x.Contains("challenge_url")).FirstOrDefault()?.Replace("challenge_url=", ""));
                var chc = data.FirstOrDefault(x => x.Contains("challenge_complexity"))?.Split('=')[1].ToString();
                var chs = data.FirstOrDefault(x => x.Contains("challenge_signature"))?.Split('=')[1].ToString();

                var engine = new Jurassic.ScriptEngine();
                var jsres = (string) engine.Evaluate(FindSolutionScript(chc ?? "",chs ?? ""));
                Console.WriteLine(jsres);

                request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(ch_url),
                    Method = HttpMethod.Post,
                };
                request.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.5112.124 YaBrowser/22.9.4.863 Yowser/2.5 Safari/537.36");
                request.Headers.Add("cookie", sc2);
                //request.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                //var tcon = $"solution={rnd.Next(1000000)}";

                Dictionary<string, string> fuecon = new Dictionary<string, string>
                {
                    ["solution"] = jsres,
                };
                var UEContent = new FormUrlEncodedContent(fuecon);
                request.Content = UEContent;

                response = await client.SendAsync(request);
            }
            text = response.Content.ReadAsStringAsync().Result;
            cookiess = response.Headers?.SingleOrDefault(header => header.Key == "Set-Cookie").Value;
            sc1 = cookiess?.FirstOrDefault();
            //ngenix_jscv_cd881f1695eb=cookie_expires=1670260316&cookie_signature=8n0fruhtWkYQt22oAykPDEUc8ww%3D; Expires=Mon, 05 Dec 2022 17:11:56 GMT; Domain=sbermarket.ru; Path=/



            //var engine = new Jurassic.ScriptEngine();
            //var jsres = engine.Evaluate(FindSolutionScript());
            //Console.WriteLine(jsres);

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
        public static string FindSolutionScript(string chc, string chs)
        {
            string fg  = """
                 'use strict';
                var chc = chc_value;
                var chs = 'chs_value';
                a0_0x476ee4({challenge_complexity:chc,challenge_signature:decodeURIComponent(chs)});
                function a0_0x476ee4(message) {
                    var result = parseInt(Math.random() * 1e6);
                    var valCamelCase = null;
                    var value = message.challenge_complexity;
                    var output = message.challenge_signature;
                    for (; valCamelCase != value;) {
                      result = result + 1;
                      valCamelCase = a0_0x4f6498(a0_0x2704ae(output + String(result)));
                    }
                    return result = String(result), result;
                  }
                  function a0_0x4f6498(data) {
                    var waitBeforeReconnect = 0;
                    var i = 0;
                    for (; i < data.length; i++) {
                      var reconnectTimeIncrease = a0_0x377fa3(data[i]);
                      waitBeforeReconnect = waitBeforeReconnect + reconnectTimeIncrease;
                      if (reconnectTimeIncrease < 8) {
                        break;
                      }
                    }
                    return waitBeforeReconnect;
                  }
                  function a0_0x377fa3(canCreateDiscussions) {
                    var rudhvi = 0;
                    if (canCreateDiscussions > 0) {
                      for (; (canCreateDiscussions & 128) == 0;) {
                        canCreateDiscussions = canCreateDiscussions << 1;
                        rudhvi++;
                      }
                      return rudhvi;
                    } else {
                      return 8;
                    }
                  }
                  function a0_0x2704ae(data) {
                    return a0_0x5e7c4f(a0_0x551f03(a0_0x9956db(data), data.length * 8));
                  }
                  function a0_0x5e7c4f(myPreferences) {
                    var PL$86 = [];
                    var ret = 0;
                    for (; ret < myPreferences.length * 32; ret = ret + 8) {
                      PL$86.push(myPreferences[ret >> 5] >>> 24 - ret % 32 & 255);
                    }
                    return PL$86;
                  }
                  function a0_0x551f03(data, i) {
                    data[i >> 5] |= 128 << 24 - i % 32;
                    data[(i + 64 >> 9 << 4) + 15] = i;
                    var intArray = Array(80);
                    var height = 1732584193;
                    var value = -271733879;
                    var c = -1732584194;
                    var d = 271733878;
                    var e = -1009589776;
                    var index = 0;
                    for (; index < data.length; index = index + 16) {
                      var y = height;
                      var options = value;
                      var oldc = c;
                      var oldd = d;
                      var olde = e;
                      var i = 0;
                      for (; i < 80; i++) {
                        if (i < 16) {
                          intArray[i] = data[index + i];
                        } else {
                          intArray[i] = (intArray[i - 3] ^ intArray[i - 8] ^ intArray[i - 14] ^ intArray[i - 16]) << 1 | (intArray[i - 3] ^ intArray[i - 8] ^ intArray[i - 14] ^ intArray[i - 16]) >>> 31;
                        }
                        var MIN_UNIT = a0_0x31d2c0(a0_0x31d2c0(height << 5 | height >>> 27, a0_0x14a0bd(i, value, c, d)), a0_0x31d2c0(a0_0x31d2c0(e, intArray[i]), a0_0x10807a(i)));
                        e = d;
                        d = c;
                        c = value << 30 | value >>> 2;
                        value = height;
                        height = MIN_UNIT;
                      }
                      height = a0_0x31d2c0(height, y);
                      value = a0_0x31d2c0(value, options);
                      c = a0_0x31d2c0(c, oldc);
                      d = a0_0x31d2c0(d, oldd);
                      e = a0_0x31d2c0(e, olde);
                    }
                    return Array(height, value, c, d, e);
                  }
                  function a0_0x31d2c0(x, y) {
                    var uch = (x & 65535) + (y & 65535);
                    var dwch = (x >> 16) + (y >> 16) + (uch >> 16);
                    return dwch << 16 | uch & 65535;
                  }
                  function a0_0x14a0bd(aRoundNumber, nChunkB, nChunkC, nChunkD) {
                    if (aRoundNumber < 20) {
                      return nChunkB & nChunkC | ~nChunkB & nChunkD;
                    }
                    if (aRoundNumber < 40) {
                      return nChunkB ^ nChunkC ^ nChunkD;
                    }
                    if (aRoundNumber < 60) {
                      return nChunkB & nChunkC | nChunkB & nChunkD | nChunkC & nChunkD;
                    }
                    return nChunkB ^ nChunkC ^ nChunkD;
                  }
                  function a0_0x10807a(aRoundNumber) {
                    return aRoundNumber < 20 ? 1518500249 : aRoundNumber < 40 ? 1859775393 : aRoundNumber < 60 ? -1894007588 : -899497514;
                  }
                  function a0_0x9956db(data) {
                    var resultArr = Array(data.length >> 2);
                    var i = 0;
                    for (; i < resultArr.length; i++) {
                      resultArr[i] = 0;
                    }
                    i = 0;
                    for (; i < data.length * 8; i = i + 8) {
                      resultArr[i >> 5] |= (data.charCodeAt(i / 8) & 255) << 24 - i % 32;
                    }
                    return resultArr;
                  } 
                """;
            string SBscript = "'use strict';\r\nvar chc = chc_value;\r\nvar chs = 'chs_value';\r\na0_0x476ee4({challenge_complexity:chc,challenge_signature:decodeURIComponent(chs)});\r\nfunction a0_0x476ee4(message) {\r\n    var result = parseInt(Math.random() * 1e6);\r\n    var valCamelCase = null;\r\n    var value = message.challenge_complexity;\r\n    var output = message.challenge_signature;\r\n    for (; valCamelCase != value;) {\r\n      result = result + 1;\r\n      valCamelCase = a0_0x4f6498(a0_0x2704ae(output + String(result)));\r\n    }\r\n    return result = String(result), result;\r\n  }\r\n  function a0_0x4f6498(data) {\r\n    var waitBeforeReconnect = 0;\r\n    var i = 0;\r\n    for (; i < data.length; i++) {\r\n      var reconnectTimeIncrease = a0_0x377fa3(data[i]);\r\n      waitBeforeReconnect = waitBeforeReconnect + reconnectTimeIncrease;\r\n      if (reconnectTimeIncrease < 8) {\r\n        break;\r\n      }\r\n    }\r\n    return waitBeforeReconnect;\r\n  }\r\n  function a0_0x377fa3(canCreateDiscussions) {\r\n    var rudhvi = 0;\r\n    if (canCreateDiscussions > 0) {\r\n      for (; (canCreateDiscussions & 128) == 0;) {\r\n        canCreateDiscussions = canCreateDiscussions << 1;\r\n        rudhvi++;\r\n      }\r\n      return rudhvi;\r\n    } else {\r\n      return 8;\r\n    }\r\n  }\r\n  function a0_0x2704ae(data) {\r\n    return a0_0x5e7c4f(a0_0x551f03(a0_0x9956db(data), data.length * 8));\r\n  }\r\n  function a0_0x5e7c4f(myPreferences) {\r\n    var PL$86 = [];\r\n    var ret = 0;\r\n    for (; ret < myPreferences.length * 32; ret = ret + 8) {\r\n      PL$86.push(myPreferences[ret >> 5] >>> 24 - ret % 32 & 255);\r\n    }\r\n    return PL$86;\r\n  }\r\n  function a0_0x551f03(data, i) {\r\n    data[i >> 5] |= 128 << 24 - i % 32;\r\n    data[(i + 64 >> 9 << 4) + 15] = i;\r\n    var intArray = Array(80);\r\n    var height = 1732584193;\r\n    var value = -271733879;\r\n    var c = -1732584194;\r\n    var d = 271733878;\r\n    var e = -1009589776;\r\n    var index = 0;\r\n    for (; index < data.length; index = index + 16) {\r\n      var y = height;\r\n      var options = value;\r\n      var oldc = c;\r\n      var oldd = d;\r\n      var olde = e;\r\n      var i = 0;\r\n      for (; i < 80; i++) {\r\n        if (i < 16) {\r\n          intArray[i] = data[index + i];\r\n        } else {\r\n          intArray[i] = (intArray[i - 3] ^ intArray[i - 8] ^ intArray[i - 14] ^ intArray[i - 16]) << 1 | (intArray[i - 3] ^ intArray[i - 8] ^ intArray[i - 14] ^ intArray[i - 16]) >>> 31;\r\n        }\r\n        var MIN_UNIT = a0_0x31d2c0(a0_0x31d2c0(height << 5 | height >>> 27, a0_0x14a0bd(i, value, c, d)), a0_0x31d2c0(a0_0x31d2c0(e, intArray[i]), a0_0x10807a(i)));\r\n        e = d;\r\n        d = c;\r\n        c = value << 30 | value >>> 2;\r\n        value = height;\r\n        height = MIN_UNIT;\r\n      }\r\n      height = a0_0x31d2c0(height, y);\r\n      value = a0_0x31d2c0(value, options);\r\n      c = a0_0x31d2c0(c, oldc);\r\n      d = a0_0x31d2c0(d, oldd);\r\n      e = a0_0x31d2c0(e, olde);\r\n    }\r\n    return Array(height, value, c, d, e);\r\n  }\r\n  function a0_0x31d2c0(x, y) {\r\n    var uch = (x & 65535) + (y & 65535);\r\n    var dwch = (x >> 16) + (y >> 16) + (uch >> 16);\r\n    return dwch << 16 | uch & 65535;\r\n  }\r\n  function a0_0x14a0bd(aRoundNumber, nChunkB, nChunkC, nChunkD) {\r\n    if (aRoundNumber < 20) {\r\n      return nChunkB & nChunkC | ~nChunkB & nChunkD;\r\n    }\r\n    if (aRoundNumber < 40) {\r\n      return nChunkB ^ nChunkC ^ nChunkD;\r\n    }\r\n    if (aRoundNumber < 60) {\r\n      return nChunkB & nChunkC | nChunkB & nChunkD | nChunkC & nChunkD;\r\n    }\r\n    return nChunkB ^ nChunkC ^ nChunkD;\r\n  }\r\n  function a0_0x10807a(aRoundNumber) {\r\n    return aRoundNumber < 20 ? 1518500249 : aRoundNumber < 40 ? 1859775393 : aRoundNumber < 60 ? -1894007588 : -899497514;\r\n  }\r\n  function a0_0x9956db(data) {\r\n    var resultArr = Array(data.length >> 2);\r\n    var i = 0;\r\n    for (; i < resultArr.length; i++) {\r\n      resultArr[i] = 0;\r\n    }\r\n    i = 0;\r\n    for (; i < data.length * 8; i = i + 8) {\r\n      resultArr[i >> 5] |= (data.charCodeAt(i / 8) & 255) << 24 - i % 32;\r\n    }\r\n    return resultArr;\r\n  }";
            SBscript = SBscript.Replace("chc_value", chc).Replace("chs_value", chs);
            return SBscript;
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