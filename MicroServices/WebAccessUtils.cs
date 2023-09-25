using System.Net;
using System.Net.Http;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Diagnostics;
using AngleSharp;
using AngleSharp.Html.Parser;
using AngleSharp.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Runtime.InteropServices;
using System.Globalization;
using SB_Parser_API.Models;
using static SB_Parser_API.MicroServices.DBSerices;
using static SB_Parser_API.Models.WebModels;
using System.Security.Policy;
using RandomUserAgent;
using System.Net.NetworkInformation;
using static System.Net.Mime.MediaTypeNames;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.ComponentModel;
using AngleSharp.Common;
using System.Net.Mime;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Xml.Linq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Proxy = SB_Parser_API.Models.Proxy;
using ProxyS = OpenQA.Selenium.Proxy;
using Microsoft.AspNetCore.Hosting.Server;
using static System.Net.WebRequestMethods;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.Chromium;

namespace SB_Parser_API.MicroServices
{
    public class GetInfo_Response
    {
        public string text = "";
        public HttpResponseMessage response = null!;
    }

    public class proxyscrapeProfile
    {
        public string url = "";
        public string protocol = "";
        public string defence = "";
    }


    public class WebAccessUtils
    {
        public static string curCT = $"WebAccessUtils";
        //thisProcess = Process.GetCurrentProcess();

        public static void RunProxyServer() //async HttpListener server
        {
            Console.WriteLine("Hello from Proxy");
            ProxyServer.Server.DumpHeaders = true;
            ProxyServer.Server.DumpPostData = true;
            ProxyServer.Server.DumpResponseData = true;

            if(ProxyServer.Server.Start())
                Console.WriteLine("Server started.");


            /*
                        while (true)
                        {
                            Console.WriteLine("Hello1 from Proxy");
                            var context =  server.GetContext();
                            Console.WriteLine("Hello2 from Proxy");

                            var request = context.Request;  // получаем данные запроса
                            var response = context.Response;    // получаем объект для установки ответа
                            var user = context.User;        // получаем данные пользователя
                            Console.WriteLine($"Получен запрос {request.HttpMethod} {request.Url}");

                            Console.WriteLine($"reqURL={request.Url}");

                            HttpClient httpClient = new HttpClient();

                            var reqUrl = request.Url?.ToString().Replace(":8888", "") ?? "";
                            var rUri = new Uri(reqUrl);
                            // Прямо передаем пришедший HTTP-запрос
                            HttpRequestMessage targetRequest = new HttpRequestMessage(new HttpMethod(request.HttpMethod), rUri);

                            // Копируем заголовки и тело запроса
                            string?[] array = (request.Headers.AllKeys ?? new string[] { });
                            for (int i = 0; i < array.Length; i++)
                            {
                                if(array[i]!.Contains("Proxy") || array[i]!.Contains("Lenght"))
                                    continue;
                                string headerName = array[i]!;
                                targetRequest.Headers.Add(headerName, request.Headers.Get(headerName));
                            }

                            using (Stream bodyStream = request.InputStream)
                            {
                                byte[] requestBodyBytes = new byte[request.ContentLength64];
                                await bodyStream.ReadAsync(requestBodyBytes, 0, requestBodyBytes.Length);
                                targetRequest.Content = new ByteArrayContent(requestBodyBytes);
                            }

                            var targetResponse = await httpClient.SendAsync(targetRequest);
                            targetResponse ??= new();

                            // Отправляем ответ от целевого сервера клиенту
                            response.StatusCode = (int)targetResponse.StatusCode;
                            response.ContentType = targetResponse?.Content.Headers?.ContentType?.ToString() ?? "";
                            byte[] responseData = await targetResponse!.Content.ReadAsByteArrayAsync();
                            response.ContentLength64 = responseData.Length;
                            response.OutputStream.Write(responseData, 0, responseData.Length);
                            response.Close();
                        }

                        server.Stop(); // останавливаем сервер
                        server.Close(); // закрываем HttpListener
             */
        }

        public static string GetCookieFromSelenium(string proxy, string userAgent)
        {

            HttpListener server = new HttpListener();
            // установка адресов прослушки
            /*
            server.Prefixes.Add("http://127.0.0.1:8888/");
            server.Start(); // начинаем прослушивать входящие подключения
            Console.WriteLine("Proxy server is started");
            */
            RunProxyServer();
            //Console.ReadKey();
            //var proxyTask = Task.Factory.StartNew(() => RunProxyServer(server), TaskCreationOptions.LongRunning);

            //Task.Delay(5000).Wait();
            ChromeOptions options = new ChromeOptions();
            //options.AddArguments("--proxy-server=http://127.0.0.1:8888");
            options.AddArgument("--user-agent=Mozilla/7.0 (iPad; CPU OS 66_0 like Mac OS X) AppleWebKit/536.26 (KHTML, like Gecko) Version/6.0 Mobile/10A5355d Safari/8536.25");
            //options.AddArgument("--proxy-server=http://45.189.252.57:999");
            //options.AddArgument("headless");
            options.AddArgument("--disable-blink-features");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("--enable-automation=false");
            options.UseWebSocketUrl = true;
            options.AddAdditionalOption("useAutomationExtension", false);
            var capabilities = options.ToCapabilities();

            var driver = new ChromeDriver(options);

            //driver.ExecuteAsyncScript("");
            //driver.Url = "http://zakazportretov.ru"; //sbermarket.ru
            // Настройки прокси
            string proxyIP = "101.51.169.208";
            string proxyPort = "8080";

            // JavaScript-скрипт для смены прокси
            string jsScript = $@"
var config = {{
  mode: ""fixed_servers"",
  rules: {{
    singleProxy: {{
      scheme: ""http"",
      host: ""{proxyIP}"",
      port: {proxyPort}
    }},
    bypassList: []
  }}
}};
chrome.proxy.settings.set({{value: config, scope: ""regular""}}, function() {{}});
";

            // Выполните JavaScript-скрипт
            //IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;
            //jsExecutor.ExecuteScript(jsScript);

            driver.ExecuteCdpCommand("Network.setUserAgentOverride", new() { { "userAgent", "Mozilla/10.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.53 Safari/537.36" } });

            // Настройки прокси
            var proxyConfig = new Dictionary<string, object>
            {
                { "type", "manual" },
                //{ "httpProxy", "your_proxy" },
                //{ "httpsProxy", "your_proxy" },
                //{ "sslProxy", "your_proxy" },
                //{ "ftpProxy", "your_proxy" },
                { "socksProxy", "socks4://182.52.63.95:4153" },
                { "noProxy", "" }
            };

            var devTools = driver as IDevTools;
            var session = devTools.GetDevToolsSession();

            // Отправляем команду Network.enable для включения сетевых событий
            //session.Send(Network.Enable());
            // Настройки прокси
            var proxyConfig1 = """" 
            {
                Type = "manual",
                SocksProxy = "socks4://182.52.63.95:4153",
                NoProxy = ""
            }
            """";
            //session.SendCommand(ChromeDriver.SendChromeCommandWithResult,"SetProxyOverride(proxyConfig1)");

            // Отправляем команду CDP для изменения настроек прокси
            //var ce = driver.CommandExecutor;

            //ce.Execute(ChromiumDriver.ExecuteCdp);

            //driver.ExecuteCdpCommand("chrome.proxy.settings.set", proxyConfig);
            //driver.ExecuteCdpCommand("Network.setProxyOverride", proxyConfig);

            //var proxySettings = driver.ExecuteCdpCommand("Network.getProxySettings", new());
            Task.Delay(2000).Wait();
            driver.Navigate().GoToUrl("https://sbermarket.ru/api/v2/products?q=&sid=9595&tid=&page=11&per_page=25&sort=popularity"); //sbermarket.ru/api/v2/products?q=&sid=9595&tid=&page=23&per_page=25&sort=popularity http://go.com/  "https://sbermarket.ru/api/v2/products?q=&sid=9595&tid=&page=1&per_page=23&sort=popularity"
            Task.Delay(5000).Wait();
            var element = driver.FindElement(By.TagName("pre"));
            var innerHtml1 = element.GetAttribute("innerHTML");

            driver.SwitchTo().NewWindow(WindowType.Tab);
            driver.Navigate().GoToUrl("https://sbermarket.ru/api/v2/products?q=&sid=9595&tid=&page=11&per_page=25&sort=popularity");

            Task.Delay(5000).Wait();
            driver.SwitchTo().Window(driver.WindowHandles.First());

            //driver.ExecuteScript(jsScript);

            var spid = driver.Manage().Cookies.GetCookieNamed("spid")?.Value;
            var spsc = driver.Manage().Cookies.GetCookieNamed("spsc")?.Value;
            var spca = driver.Manage().Cookies.GetCookieNamed("spca")?.Value;
            var page_source = driver.PageSource;
             element = driver.FindElement(By.TagName("pre"));
            var innerHtml2 = element.GetAttribute("innerHTML");
            var ch_Window = driver.Manage().Window;
            var agent = driver.ExecuteScript("return navigator.userAgent");
            //driver.Capabilities.
            //driver.Close();
            var rt =IPAddress.Loopback;

            driver.Quit();
            return "";
            driver.FindElement(By.XPath(@".//div[@id='search-3']/form/input[@id='s']")).SendKeys("c#");
            driver.FindElement(By.XPath(@".//input[@id='searchsubmit']")).Click();
            Thread.Sleep(3000);
            var links = driver.FindElements(By.XPath(".//h2/a"));
            foreach (IWebElement link in links)
                Console.WriteLine("{0} - {1}", link.Text, link.GetAttribute("href"));
            return "";
        }
        public static string GetWebInfoSysOld(Func<HttpRequestMessage> requestGenerator, string proxy, int tOut = defaultTimeOut)
        {
            HttpResponseMessage response = null!;
            string str = "";

            var handler = new HttpClientHandler();
            if (proxy.Length > 0)
            {
                handler.Proxy = new WebProxy(new Uri(proxy));
                handler.UseProxy = true;
            }
            else
            {
                handler.Proxy = null;
                handler.UseProxy = false;
            }

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(tOut)
            };
            IEnumerable<string> cookies;

            var wdt = DateTime.Now.AddMilliseconds(tOut * 1.5);

            var ErrTxt = new List<string>();
            var att = 0;
            for (var j = 1; j == 1;)
            {
                if (wdt < DateTime.Now)
                {
                    Console.Title = curCT;
                    return $"$$TimeOut-{tOut * 1.5 / 1000}sec";
                }
                if (ErrTxt.Count > 1 && ErrTxt[ErrTxt.Count - 1] == ErrTxt[ErrTxt.Count - 2])
                {
                    //var bc = Console.BackgroundColor;
                    //Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    //Console.WriteLine($"{ErrTxt[ErrTxt.Count - 1]}");
                    //Console.BackgroundColor=bc;
                    return ErrTxt[ErrTxt.Count - 1];
                }
                if (att > 2)
                {
                    //var bc = Console.BackgroundColor;
                    //Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    //Console.WriteLine($"3 bad attempts");
                    //Console.BackgroundColor = bc;
                    return $"3 bad attempts";
                }
                att++;
                j = 0;
                var request = requestGenerator(); //"{\"Method\":{\"Method\":\"GET\"},\"RequestUri\":\"https://igooods.ru\",\"Headers\":[{\"User-Agent\":\"Mozilla/5.0\"},{\"User-Agent\":\"(Windows NT 10.0; Win64 x64)\"},{\"User-Agent\":\"AppleWebKit/537.36\"},{\"User-Agent\":\"(KHTML, like Gecko)\"},{\"User-Agent\":\"Chrome/104.0.0.0\",\"Safari/537.36\"}]}";
                                                  //request.Content //= new StringContent("0123456789 some json", Encoding.UTF8, MediaTypeNames.Text.Plain);// .Application.Json /* or "application/json" in older versions */);

                /*
                var strr = JsonConvert.SerializeObject(request1);
                strr = "{\"Method\":{\"Method\":\"GET\"},\"RequestUri\":\"https://igooods.ru\",\"Headers\":[{\"User-Agent\":\"Mozilla/5.0\"},{\"User-Agent\":\"(Windows NT 10.0; Win64 x64)\"},{\"User-Agent\":\"AppleWebKit/537.36\"},{\"User-Agent\":\"(KHTML, like Gecko)\"},{\"User-Agent\":\"Chrome/104.0.0.0\",\"Safari/537.36\"}]}";
                var request = JsonConvert.DeserializeObject<HttpRequestMessage>(strr);
                */
                var uri = request?.RequestUri;

                try
                {
                    var taskR = client.SendAsync(request!);
                    taskR.Wait();
                    /*var wdtl = DateTime.Now.AddMilliseconds(tOut);
                    while (wdtl > DateTime.Now)
                        if (taskR.IsCompleted)
                            break;
                        else
                            Task.Delay(100).Wait();
                    */
                    //if (taskR.IsCompleted)
                    //{
                    response = taskR.Result;              //response = (HttpWebResponse)request.GetResponse();
                    taskR?.Dispose();
                    if (taskR?.Exception is not null)
                    {
                        ErrTxt.Add(taskR?.Exception?.Message ?? "");
                        Console.Title = $"PCIrtEx({Thread.CurrentThread.ManagedThreadId}):{ErrTxt[ErrTxt.Count - 1]} / {uri}({proxy})";
                        Task.Delay(1000).Wait();
                        j = 1;
                        continue;
                    }
                    //}
                    //else 
                    //{ 
                    //    try 
                    //    {
                    //        ErrTxt.Add(taskR?.Exception?.Message ?? "");
                    //        Console.Title = $"PCIrt({Thread.CurrentThread.ManagedThreadId}):{ErrTxt[ErrTxt.Count - 1]} / tOut-{uri}({proxy})"; 
                    //    } 
                    //    catch 
                    //    { ErrTxt.Add("Error Reading ErrMsg"); }
                    //    //taskR?.Dispose();
                    //    j = 1; 
                    //    continue; 
                    //}
                    cookies = response.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value;
                    str = response.Content.ReadAsStringAsync().Result;
                }
                catch (Exception ex)
                {
                    j = 1;
                    ErrTxt.Add(ex.Message);
                    Console.Title = $"PCI({Thread.CurrentThread.ManagedThreadId}):{ex.Message} / {uri}({proxy})";
                    Task.Delay(1000).Wait();
                    continue;
                }
            }
            response?.Dispose();
            Console.Title = curCT;
            return str;
        }



        public static void GetWebInfoSys(WebRequestOrder wrOrder)
        {
            Proxy_to_Domain ptdCurrent = new();
            Proxy_to_Domain? ptd_inDB_Current = null;
            var reqTime = DateTime.Now;
            var requestGenerator = () => {
                var req = new HttpRequestMessage()
                {
                    RequestUri = new Uri(wrOrder.url),
                    Method = wrOrder.method
                };
                foreach (var h in wrOrder.headers)
                    req.Headers.Add(h.key, h.value);
                return req;
            };
            /*
            var updateDomain = (Proxy_to_Domain ptd_or, Proxy_to_Domain ptd_in_DB) =>
            {
                var ptd = ptd_or with { tested = 1, lastCheck = DateTime.Now, inUseTill = null };
                ptd.client = ptd_or.client;
                ptd.rate = ptd.testOK * 1000;
                //ptd.userAgent = "";
                ptd.setParametr("userAgent", "");

                if (ptd_in_DB is not null)
                {
                    if (ptd_in_DB.rate == 0 && ptd_in_DB.testOK == 0 && ptd.testOK > 0 && ptd_in_DB.tested < 9)
                        ptd.rate = (9 + ptd.testOK - ptd_in_DB.tested) * 100;
                    else
                        ptd.rate = (int)Math.Ceiling((double)(ptd_in_DB.rate * 9 + ptd.rate) / 10);

                    ptd.tested += ptd_in_DB.tested;
                    ptd.testOK += ptd_in_DB.testOK;
                    ptd.setParametr("userAgent", ptd_in_DB.getParametr("userAgent") ?? "");
                    //ptd.userAgent = ptd_in_DB.userAgent ?? "";
                    ptd.domain = ptd_in_DB.domain;
                }
                else
                    if (ptd.testOK == 0)
                    return;

                var uA = ptd.getParametr("userAgent");
                if ((uA is null || uA.Length == 0) && ptd.testOK > 0)
                    ptd.setParametr("userAgent", RandomUA());
                //if ((ptd.userAgent is null || ptd.userAgent.Length == 0) && ptd.testOK > 0)
                //  ptd.userAgent = RandomUA();

                if (ptd.testOK > ptd.tested) { Console.Beep(); Console.Beep(); Console.Beep(); Console.Beep(); Console.WriteLine($"\r\n\r\ntested={ptd.tested} testOK={ptd.testOK} !!!!"); }

                if (!ProxyToDomainLists.TryGetValue(ptd.domain, out _))
                    ProxyToDomainLists[ptd.domain].PD_List = ProxyToDomainGet(new Proxy_to_Domain() { domain = ptd.domain }).OrderBy(x => x.rate).ThenBy(x => x.testOK).ToList();

                lock (ProxyToDomainLists[ptd.domain])
                {
                    ProxyToDomainLists[ptd.domain].RemoveAll(x => x.ip == ptd.ip && x.port == ptd.port &&
                    x.protocol == ptd.protocol && x.domain == ptd.domain);
                    ProxyToDomainLists[ptd.domain].Add(ptd);
                    ProxyToDomainAddOrUpdate(ptd);
                }
            };
            */
            var ptdoGet = (string theDomain) =>
            {
                ProxyToDomainContext? the_ptdl;
                if (!ProxyToDomainLists.TryGetValue(theDomain, out the_ptdl))
                    lock (ProxyToDomainLists)
                        the_ptdl = ProxyToDomainLists[theDomain] = new()
                        {
                            PD_List = ProxyToDomainGet(new Proxy_to_Domain()
                            { domain = theDomain }).Distinct(new ProxyToDomainIpPortProtocolComparer()).
                            OrderBy(x => x.rate).ThenBy(x => x.testOK).ToList()
                        };
                return the_ptdl;
            };

            var updateDomain = (Proxy_to_Domain ptd_or, Proxy_to_Domain? ptd_in_DB) =>
            {
                var ptd = ptd_or with { tested = 1, lastCheck = DateTime.Now, inUseTill = null };
                ptd.client = ptd_or.client;
                ptd.rate = ptd.testOK * 1000;
                ptd.setParametr("userAgent", "");
                var ptdo = ptdoGet(ptd.domain);
                if (ptd_in_DB is null || ptdo.PD_List.IndexOf(ptd_in_DB) < 0)
                {
                    lock (ProxyToDomainLists[ptd.domain])
                    {
                        ptd_in_DB = ptdo?.PD_List?.FirstOrDefault(x => x.ip == ptd.ip && x.port == ptd.port && x.protocol == ptd.protocol);
                        if(ptd_in_DB is not null)
                            ptd_in_DB.inUseTill = DateTime.Now.AddMilliseconds(wrOrder.timeOut * 6);
                    }
                }

                if (ptd_in_DB is not null)
                {
                    if (ptd_in_DB.rate == 0 && ptd_in_DB.testOK == 0 && ptd.testOK > 0 && ptd_in_DB.tested < 9)
                        ptd.rate = (9 + ptd.testOK - ptd_in_DB.tested) * 100;
                    else
                        ptd.rate = (int)Math.Ceiling((double)(ptd_in_DB.rate * 9 + ptd.rate) / 10);

                    ptd.tested += ptd_in_DB.tested;
                    ptd.testOK += ptd_in_DB.testOK;
                    ptd.setParametr("userAgent", ptd_in_DB.getParametr("userAgent") ?? "");
                    //ptd.userAgent = ptd_in_DB.userAgent ?? "";
                    ptd.domain = ptd_in_DB.domain;
                }
                else
                    if (ptd.testOK <= 0)
                        return;

                var uA = ptd.getParametr("userAgent");
                if ((uA is null || uA.Length == 0) && ptd.testOK > 0)
                    ptd.setParametr("userAgent", RandomUA());

                if (ptd.testOK > ptd.tested) { Console.Beep(); Console.Beep(); Console.Beep(); Console.Beep(); Console.WriteLine($"\r\n\r\ntested={ptd.tested} testOK={ptd.testOK} !!!!"); }

                /*
                if (!ProxyToDomainLists.TryGetValue(ptd.domain, out _))
                    lock(ProxyToDomainLists)
                        ProxyToDomainLists[ptd.domain] = new() { PD_List = ProxyToDomainGet(new Proxy_to_Domain() 
                        { domain = ptd.domain }).OrderBy(x => x.rate).ThenBy(x => x.testOK).ToList() };
                */
                //foreach(var x in ProxyToDomainLists) Console.WriteLine(x.Key);
                lock (ProxyToDomainLists[ptd.domain])
                {
                    int i, j;
                    if (ptd_in_DB is null)
                    {
                        i = ProxyToDomainLists[ptd.domain].PD_List.Count;
                        ProxyToDomainLists[ptd.domain].PD_List.Add(ptd);
                        ProxyToDomainLists[ptd.domain].PD_ListToUpdate.Add(ptd);
                    }
                    else
                    {
                        i = ProxyToDomainLists[ptd.domain].PD_List.IndexOf(ptd_in_DB);
                        if (i < 0)
                        {
                            var ptdi = ProxyToDomainLists[ptd.domain].PD_List.Find(x => x.ip == ptd_in_DB.ip &&
                            x.port == ptd_in_DB.port && x.protocol == ptd_in_DB.protocol && x.domain == ptd_in_DB.domain);

                            if (ptdi is not null)
                                i = ProxyToDomainLists[ptd.domain].PD_List.IndexOf(ptdi);
                            else
                            {
                                i = ProxyToDomainLists[ptd.domain].PD_List.Count;
                                ProxyToDomainLists[ptd.domain].PD_List.Add(ptd_in_DB);
                            }
                        }
                        j = ProxyToDomainLists[ptd.domain].PD_ListToUpdate.IndexOf(ptd_in_DB);

                        try
                        {
                            ProxyToDomainLists[ptd.domain].PD_List[i] = ptd;
                        }
                        catch (Exception e)
                        {
                            var sbo = ProxyToDomainLists[ptd.domain].PD_List;
                            Console.Beep();
                            Console.WriteLine($"Count={sbo.Count},i={i}");
                            Console.WriteLine($"Count={sbo.Count}");
                        }

                        if (j >= 0)
                            ProxyToDomainLists[ptd.domain].PD_ListToUpdate[j] = ptd;
                        else
                            ProxyToDomainLists[ptd.domain].PD_ListToUpdate.Add(ptd);

                        var ptdl = ProxyToDomainLists[ptd.domain].PD_List;
                        int imax = ptdl.Count - 1;
                        while (i < imax && ptdl[i].rate > ptdl[i + 1].rate)
                        {
                            (ptdl[i + 1], ptdl[i]) = (ptdl[i], ptdl[i + 1]);
                            i++;
                        }
                        while (i > 0 && ptdl[i].rate < ptdl[i - 1].rate)
                        {
                            (ptdl[i - 1], ptdl[i]) = (ptdl[i], ptdl[i - 1]);
                            i--;
                        }
                        if (ProxyToDomainLists[ptd.domain].proxiesInUse > 0)
                            ProxyToDomainLists[ptd.domain].proxiesInUse--;
                    }
                }
                
                /*
                    ProxyToDomainLists[ptd.domain].PD_List.Add(ptd_in_DB);

                    ProxyToDomainLists[ptd.domain].PD_List.RemoveAll(x => x.ip == ptd.ip && x.port == ptd.port &&
                    x.protocol == ptd.protocol && x.domain == ptd.domain);
                    ProxyToDomainLists[ptd.domain].PD_List.Add(ptd);
                    ProxyToDomainAddOrUpdate(ptd);
                */
            };

            var updateProxyInfo = (string em, Proxy_to_Domain pc, Proxy_to_Domain? pcInDB) =>
            {
                Console.Title = em + $"url={wrOrder.url}";
                wrOrder.errorMsg = em;

                if (pc.ip.Length == 0)
                    return;

                if (pc.requestCounterInArow > 0)
                    pc.requestCounterInArow--;

                pc.domain = GetDomain(wrOrder.url);
                pc.testOK = em.Length == 0 ? 1 : (pc.requestCounterInArow = 0);
                updateDomain(pc, pcInDB);  // ??? pc, pc

                Console.Title = curCT;
            };
            /*
            var updateProxyInfo = (string em, Proxy_to_Domain pc) =>
            {
                Console.Title = em + $"url={wrOrder.url}";
                wrOrder.errorMsg = em;

                if (pc.ip.Length == 0)
                    return;
                var domainsToUpdate = new List<Proxy_to_Domain>();
                if (em.Contains(PINGfailed))
                    pc.domain = "";
                else
                    pc.domain = GetDomain(wrOrder.url);

                foreach (var dkey in ProxyToDomainLists.Keys)
                    domainsToUpdate.AddRange(ProxyToDomainLists[dkey].Where(x => (x is not null) && x.ip == pc.ip && x.port == pc.port && x.protocol == pc.protocol &&
                    (pc.domain.Length == 0 ? true : x.domain == pc.domain)).ToList());

                //var domainsToUpdate = ProxyToDomainGet(pc);

                if (pc.requestCounterInArow > 0)
                    pc.requestCounterInArow--;

                if (em.Length == 0)
                {
                    pc.testOK = 1;
                    //pc.errorCounterInArow = 0;
                }
                else
                {
                    pc.testOK = 0;
                    //pc.errorCounterInArow = 0;
                    pc.requestCounterInArow = 0;
                }
                if (domainsToUpdate.Count <= 0)
                {
                    pc.testOK = em.Length == 0 ? 1 : 0;
                    updateDomain(pc, null!);
                }
                else
                    foreach (var domain in domainsToUpdate)
                    {
                        pc.testOK = em.Length == 0 ? 1 : 0;
                        updateDomain(pc, domain);
                    }
                Console.Title = curCT;
            };
            */

            if (wrOrder.pingOnly)
            {
                if (IsUrlDomainAlive(wrOrder.url, wrOrder.timeOut / 6))
                    wrOrder.pingOk = true;
                else
                    wrOrder.pingOk = false;
                Console.Title = $"END_1({Environment.CurrentManagedThreadId},url={wrOrder.url},rTime={(DateTime.Now - reqTime).ToString(@"m\:ss\.f")})";
                //Console.WriteLine($"END_1({Environment.CurrentManagedThreadId},url={wrOrder.url},rTime={(DateTime.Now-reqTime):ss.fff})");
                return;
            }

            if (wrOrder.proxy.ip.Length > 0)
            {
                if (IsUrlDomainAlive(wrOrder.proxy.ip, wrOrder.timeOut / 6))
                {
                    ptdCurrent = wrOrder.proxy;
                    wrOrder.givenProxyPingOk = true;
                }
                else
                {
                    updateProxyInfo($"{PINGfailed} {wrOrder.proxy.protocol}://{wrOrder.proxy.ip}:{wrOrder.proxy.port}", wrOrder.proxy,null);
                    wrOrder.givenProxyPingOk = false;
                }
            }
            if (wrOrder.needProxy && ptdCurrent.ip.Length == 0)
            {
                var dom = GetDomain(wrOrder.url);
                Proxy_to_Domain? pC = null;
                //var ProxyToDomainList = ProxyToDomainLists[dom];

                var ptdC = ptdoGet(dom);

                lock (ProxyToDomainLists[dom])
                {
                    ptdC.proxiesInUse++;
                    var proxiesPull = ptdC.proxiesInUse * ProxyPerQuiryThread;
                    proxiesPull = proxiesPull > ptdC.PD_List.Count ? ptdC.PD_List.Count : proxiesPull;
                    var curMin = ptdC.PD_List.Count - proxiesPull;
                    var curMax = ptdC.PD_List.Count - 1;
                    ptdC.cursor = ptdC.cursor > curMax ? curMax : ptdC.cursor;
                    ptdC.cursor = ptdC.cursor < curMin ? curMin : ptdC.cursor;
                    var curT = ptdC.cursor;
                    for (var i = 0; i < proxiesPull; i++)
                    {
                        var pCC = ptdC.PD_List[curT];
                        if (pCC.requestCounterInArow > 0 && (pCC.inUseTill is null || pCC.inUseTill < DateTime.Now))
                        {
                            pC = pCC;
                            break;
                        }
                        curT--;
                        if (curT < curMin)
                            curT = curMax;
                    }
                    if (pC is null)
                    {
                        for (var i = 0; i < proxiesPull; i++)
                        {
                            var pCC = ptdC.PD_List[ptdC.cursor];
                            if (pCC.inUseTill is null || pCC.inUseTill < DateTime.Now)
                            {
                                if (IsUrlDomainAlive(pCC.ip, wrOrder.timeOut / 6))
                                {
                                    pC = pCC;
                                    break;
                                }
                                else
                                    updateProxyInfo($"{PINGfailed} {pCC.protocol}://{pCC.ip}:{pCC.port}", pCC with { }, null);

                            }
                            ptdC.cursor--;
                            if (ptdC.cursor < curMin)
                                ptdC.cursor = curMax;
                        }
                    }
                    if (pC is not null)
                    {
                        pC.inUseTill = DateTime.Now.AddMilliseconds(wrOrder.timeOut * 16);
                        if (pC.requestCounterInArow <= 0) pC.requestCounterInArow = MaxProxyRequestInArow;
                        pC.lastCheck = DateTime.Now;
                        //ProxyToDomainAddOrUpdate(pC);
                        ptdCurrent = pC with { };
                        ptdCurrent.client = pC.client;
                        ptd_inDB_Current = pC;
                        if (ptdC.PD_ListToUpdate.IndexOf(pC) < 0)
                            ptdC.PD_ListToUpdate.Add(pC);
                    }
                }
            }
            //Console.WriteLine($"clientCount={WebModels.ProxyToDomainList.Where(x => x.client is not null).ToList().Count()}");
            if (wrOrder.needUserAgent)
            {
                wrOrder.headers.RemoveAll(x => x.key == "User-Agent");
                var ua = RandomUA();
                if (ptdCurrent is not null)
                {
                    var cUA = ptdCurrent.getParametr("userAgent");
                    if ((cUA is null) || (cUA.Length == 0))
                        ptdCurrent.setParametr("userAgent", ua);
                    else
                        ua = cUA;
                }
                wrOrder.headers.Add(new RequestHeader() { key = "User-Agent", value = ua });
            }
            HttpClient client;
            if (ptdCurrent?.client is null)
            {
                var handler = new HttpClientHandler();
                if (ptdCurrent?.ip.Length > 0)
                {
                    handler.Proxy = new WebProxy(new Uri($"{ptdCurrent.protocol}://{ptdCurrent.ip}:{ptdCurrent.port}"));
                    handler.UseProxy = true;
                    wrOrder.proxy = ptdCurrent;
                }
                else
                {
                    handler.Proxy = null;
                    handler.UseProxy = false;
                }
                wrOrder.proxy.client = ptdCurrent!.client = client = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(wrOrder.timeOut) };
                //var jsonString = JsonConvert.SerializeObject(client);
            }
            else
                client = ptdCurrent.client;

            var wdt = DateTime.Now.AddMilliseconds(wrOrder.timeOut * 1.5);

            var ErrTxt = new List<string>();
            var att = 0;

            for (var j = 1; j == 1;)
            {
                wrOrder.errorMsg = "";
                if (wdt < DateTime.Now)
                    wrOrder.errorMsg += $"$$TimeOut-{wrOrder.timeOut * 1.5 / 1000}sec";
                if (ErrTxt.Count > 1 && ErrTxt[^1] == ErrTxt[^2])
                    wrOrder.errorMsg += (wrOrder.errorMsg.Length > 0 ? "; " : "") + ErrTxt[^1];
                if (att > 2)
                    wrOrder.errorMsg += (wrOrder.errorMsg.Length > 0 ? "; " : "") + $"3 bad attempts";

                if (wrOrder.errorMsg.Length > 0)
                {
                    updateProxyInfo(wrOrder.errorMsg, ptdCurrent!,ptd_inDB_Current);
                    wrOrder.proxy = ptdCurrent!;
                    Console.Title = $"END_2({Thread.CurrentThread.ManagedThreadId},url={wrOrder.url},rTime={(DateTime.Now - reqTime).ToString(@"m\:ss\.f")})";
                    //Console.WriteLine($"END_2({Thread.CurrentThread.ManagedThreadId},url={wrOrder.url},rTime={(DateTime.Now - reqTime):ss})");
                    return;
                }
                att++;
                j = 0;
                var request = requestGenerator(); //"{\"Method\":{\"Method\":\"GET\"},\"RequestUri\":\"https://igooods.ru\",\"Headers\":[{\"User-Agent\":\"Mozilla/5.0\"},{\"User-Agent\":\"(Windows NT 10.0; Win64 x64)\"},{\"User-Agent\":\"AppleWebKit/537.36\"},{\"User-Agent\":\"(KHTML, like Gecko)\"},{\"User-Agent\":\"Chrome/104.0.0.0\",\"Safari/537.36\"}]}";
                                                  //request.Content //= new StringContent("0123456789 some json", Encoding.UTF8, MediaTypeNames.Text.Plain);// .Application.Json /* or "application/json" in older versions */);
                var uri = request?.RequestUri;
                var collectErrorMsg = (string em) =>
                {
                    j = 1;
                    ErrTxt.Add(em);
                    Console.Title = $"PCIrtEx({Thread.CurrentThread.ManagedThreadId}):{em} / {uri}({$"{ptdCurrent?.protocol}://{ptdCurrent?.ip}:{ptdCurrent?.port}"})";
                    Task.Delay(1000).Wait();
                };

                try
                {
                    var taskR = client.SendAsync(request!);
                    taskR.Wait();
                    wrOrder.response = taskR.Result;              //response = (HttpWebResponse)request.GetResponse();
                    if (taskR?.Exception is not null)
                    {
                        collectErrorMsg(taskR?.Exception?.Message ?? "");
                        taskR?.Dispose();
                        continue;
                    }
                    taskR?.Dispose();
                }
                catch (Exception ex)
                {
                    collectErrorMsg(ex.Message);
                    continue;
                }
            }
            string txt = wrOrder.textResponse() ?? "";

            if (wrOrder.contentKeys.Count <= 0)
                wrOrder.requestDone = true;
            else
                foreach (var key in wrOrder.contentKeys)
                    if (txt.Contains(key))
                        wrOrder.requestDone = true;

            updateProxyInfo(wrOrder.requestDone ? "" : "Unexpected content in response.", ptdCurrent!, ptd_inDB_Current);
            wrOrder.proxy = ptdCurrent!;
            Console.Title = $"END_3({Thread.CurrentThread.ManagedThreadId},url={wrOrder.url},rTime={(DateTime.Now - reqTime).ToString(@"m\:ss\.f")})";
            //Console.WriteLine($"END_3({Thread.CurrentThread.ManagedThreadId},url={wrOrder.url},rTime={(DateTime.Now - reqTime).ToString(@"m\:ss\.f")})");
            return;
        }
       
        static public int requestCount = 0;
        public static void RequestQueueController()
        {
            //int requestCount = 0;
            //ProxyToDomainList = ProxyToDomainGet() ?? new List<Proxy_to_Domain>();
            if(ProxyToDomainLists is null)
                ProxyToDomainLists = new();
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

            var takeNextOrder = () => {
                WebRequestOrder? nextOrder;
                int wroq = 0;
                string wroq0 = null!;

                lock (WebRequestOrderQueueL)
                {
                    if (WebRequestOrderQueue.Count > 0)
                    {
                        var minPriority = WebRequestOrderQueue.Min(x => x.priority);
                        var shortOrderList = WebRequestOrderQueue.Where(x => x.priority == minPriority);

                        if (shortOrderList.Count() == 0)
                            nextOrder = null;
                        else
                        {
                            var dtMin = shortOrderList.Min(x => x.created);
                            nextOrder = shortOrderList.FirstOrDefault(x => x.created == dtMin);
                            if (nextOrder is not null)
                                WebRequestOrderQueue.Remove(nextOrder);
                        }
                    }
                    else
                        nextOrder = null;
                    wroq = WebRequestOrderQueue.Count;
                    wroq0 = wroq>0 ? WebRequestOrderQueue[0].url: "";
                }
                requestCount++;
                //Console.WriteLine($"ReqOrder{requestCount}; url={nextOrder?.url}; wroq={wroq},wroq[0]={wroq0}"); //wroq={WebRequestOrderQueue.Count},wroq[0]={WebRequestOrderQueue[0].url}
                return nextOrder;
            };
            Task? ptdUpdtTask = null;
            List<Proxy_to_Domain> ptdUpdtList = new();
            while (true)
            {
                try
                {
                    RequestQueueControllerTimeToCheck.WaitOne(20000);
                    if ((ptdUpdtTask is null || ptdUpdtTask.IsCompleted) && ProxyToDomainListsNextRefresh < DateTime.Now)
                    {
                        lock (ProxyToDomainLists)
                        {
                            Console.WriteLine($"Domains_Total = {ProxyToDomainLists.Keys.Count}");
                            ptdUpdtList.Clear();
                            foreach (var dkey in ProxyToDomainLists.Keys)
                            {
                                Console.WriteLine($"{dkey}-{ProxyToDomainLists[dkey].PD_ListToUpdate.Count}");

                                lock (ProxyToDomainLists[dkey])
                                {
                                    foreach (var ptdUpdt in ProxyToDomainLists[dkey].PD_ListToUpdate)
                                        ptdUpdtList.Add(ptdUpdt with { });

                                    ProxyToDomainLists[dkey].PD_ListToUpdate.Clear();
                                }

                                //continue;
                                //ProxyToDomainAddOrUpdateList(ProxyToDomainLists[dkey]);
                                //ProxyToDomainLists[dkey] = ProxyToDomainGet(new Proxy_to_Domain() { domain = dkey.Length>0 ? dkey : "____"}); //Check
                            }
                        }
                        ptdUpdtTask = Task.Factory.StartNew(() => ProxyToDomainAddOrUpdateList(ptdUpdtList), TaskCreationOptions.LongRunning);
                        ProxyToDomainListsNextRefresh = DateTime.Now.AddSeconds(ProxyToDomainListsRefreshInterval);
                    }
                    List<WebRequestTaskInfo> OrderQueueItemsToDelete = new();
                    //Console.WriteLine($"WRTIL_Count={WebRequestTaskInfoList.Count}");
                    for (var i = 0; i < WebRequestTaskInfoList.Count; i++)
                    {
                        var wrti = WebRequestTaskInfoList[i];
                        if ((wrti.task is null) || wrti.task.IsCompleted || wrti.task.IsFaulted || wrti.task.IsCanceled)
                            wrti.task = Task.Factory.StartNew(() => RequestExecutor(wrti), TaskCreationOptions.LongRunning);
                        if (wrti.isResultReady)
                        {
                            wrti.webRequestOrder.completed.Set();
                            var no = takeNextOrder();
                            if (no is not null)
                            {
                                wrti.webRequestOrder = no;
                                wrti.isResultReady = false;
                                wrti.command = CMD.ExecuteRequest;
                            }
                            else
                            {
                                wrti.command = CMD.Exit;
                                OrderQueueItemsToDelete.Add(wrti);
                            }
                            wrti.checkCommand.Set();
                        }
                        else
                        { 
                        
                        }
                    }
                    OrderQueueItemsToDelete.ForEach(x => WebRequestTaskInfoList.Remove(x));
                    while (WebRequestTaskInfoList.Count < MaxWebRequestTask /*&& WebRequestOrderQueue.Count() > 0*/)
                    {
                        WebRequestOrder wro_next;
                        if ((wro_next = takeNextOrder()) is null)
                            break;
                        var tn = WebRequestTaskInfoList.Select(x => x.taskNumber).ToList();
                        var newTaskInfo = new WebRequestTaskInfo() { webRequestOrder = wro_next, isResultReady = false, taskNumber = Utils.getIDforNewRecord(tn), command = CMD.ExecuteRequest };
                        WebRequestTaskInfoList.Add(newTaskInfo);
                        newTaskInfo.task = Task.Factory.StartNew(() => RequestExecutor(newTaskInfo), TaskCreationOptions.LongRunning);
                        newTaskInfo.checkCommand.Set();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\n\n\n{nameof(RequestQueueController)}[th={Thread.CurrentThread.Name},({Thread.CurrentThread.ManagedThreadId})]{e.Message}\n\n\n");
                    RequestQueueControllerTimeToCheck.Set();
                }
            }
        }
        public static void RequestExecutor(WebRequestTaskInfo IO_Block)
        {
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;
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
                        IO_Block.isResultReady = false;
                        IO_Block.webRequestOrder.requestDone = false;
                        while (IO_Block.webRequestOrder.attempts-- > 0)
                        {
                            //Console.WriteLine($"Attempts rest:{IO_Block.webRequestOrder.attempts}");
                            GetWebInfoSys(IO_Block.webRequestOrder);
                            if(IO_Block.webRequestOrder.requestDone)
                                break;
                            IO_Block.webRequestOrder.proxy = new();
                        }
                        IO_Block.command = CMD.StandBy;
                        IO_Block.isResultReady = true;
                        RequestQueueControllerTimeToCheck.Set();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"\n\n\nRequestExecutor: [t={IO_Block.taskNumber}]{e.Message}\n\n\n");
                    IO_Block.webRequestOrder.requestDone = false;
                    IO_Block.isResultReady = false;
                    IO_Block.checkCommand.Set();
                }
            }
        }
        public static void GetWebInfo(WebRequestOrder wro)
        {
            wro.created=DateTime.Now;
            lock (WebRequestOrderQueueL)
                WebRequestOrderQueue.Add(wro);

            WaitHandle.SignalAndWait(RequestQueueControllerTimeToCheck, wro.completed);
        }
        public static AutoResetEvent GetWebInfoSetOrder(WebRequestOrder wro)
        {
            wro.created = DateTime.Now;
            lock (WebRequestOrderQueueL)
                WebRequestOrderQueue.Add(wro);
            RequestQueueControllerTimeToCheck.Set();
            return wro.completed;
        }
        public static Task GetWebInfoAsync(WebRequestOrder wro) => Task.Factory.StartNew(() => GetWebInfo(wro), TaskCreationOptions.LongRunning);

        public static GetInfo_Response GetInfoRequest(Func<HttpRequestMessage> requestGenerator,int PSnum = defaultNumberOfProxySubstitutions, int tOut = defaultTimeOut)
        {
            return null!;
        }
        public static void AddUserAgent(HttpRequestMessage request)
        {
            //request.Headers.Add("Host", "free-proxy.cz");
            //request.Headers.Add("Connection", "keep-alive");
            //request.Headers.Add("Pragma", "no-cache");
            //request.Headers.Add("Cache-Control", "no-cache");
            //request.Headers.Add("Upgrade-Insecure-Requests", "1");
            //string rua = RandomUA(); //Console.WriteLine(rua);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/104.0.0.0 Safari/537.36");
            //request.Headers.Add("Accept","text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            //request.Headers.Add("Referer", "http://free-proxy.cz/en/proxylist/main/6");
            //request.Headers.Add("Accept-Encoding","gzip, deflate");
            //request.Headers.Add("Accept-Language","ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
            //request.Headers.Add("Cookie", "fpxy_tmp_access=86197-dd00a-2ef8b");

            //request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue();
            //request.Headers.CacheControl.NoCache = true;

            //text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,
            //request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("AppleWebKit", "537.36"));
            //request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("(KHTML, like Gecko)"));
            //request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Atom", "22.0.0.37"));
            //request.Headers.Referrer = request.RequestUri;
            //request.Headers.Add("Proxy-Connection", "keep-alive");
            //request.Headers.Accept("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
        }
        public static void get_hidemy_name_list()
        {
            string uri = "";
            var reqGen = () => {
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(uri), 
                    Method = HttpMethod.Get,
                };
                AddUserAgent(request);
                return request;
            };

            curCT = $"Hidme_name({Thread.CurrentThread.ManagedThreadId})";
            var pl = new List<Proxy>();
            var htmlParser = BrowsingContext.New(Configuration.Default).GetService<IHtmlParser>();

            string str = "";
            Console.Title = curCT;

            var wdt = DateTime.Now;
            pl.RemoveAll(x => true);

            (_,var proxy) = GetDBParam(DBParam.HideMyNameProxy);
            Proxy_to_Domain? goodProxy = null;
            for (var h = 0; true; h++)
            {
                uri = @"https://hidemyna.me/ru/proxy-list/?anon=234";// "http://hidemy.name/ru/proxy-list/?anon=234"; https://sbermarket.ru/api/stores/20
                if (h > 0)
                    uri += $"&start={h * 64}";
                str = "";
                /*
                while (!str.Contains("class=country"))
                    str = GetWebInfoSysOld(reqGen, @"http://51.158.169.52:29976");     //proxy  http://46.42.16.245:31565 http://51.158.169.52:29976//, "class=country" http://157.100.12.138:999 http://176.192.70.58:8006 http://211.138.6.37:9091
                */
                while (true)
                {
                    WebRequestOrder wro = new() { url = uri };
                    wro.proxy = goodProxy is null ? wro.proxy : goodProxy;
                    wro.contentKeys.Add("class=country");
                    wro.priority = Priority.Low;
                    wro.needUserAgent = false;
                    wro.method = HttpMethod.Get;
                    //var stringContent = new StringContent(JsonConvert.SerializeObject("""md=eInoQSZXrdBfzCAjWyLF.ZlfzKiFMWXZ7SU8hgDffS0-1688559957-0-AWYd9AK8PT3ArzyDgYIddJVZswrDw8vqjhVWGxk4ycNYwQld0FlTLWocfTMC35XIWXzPu1YT7IB9GDC_pUepqTXZRwBF78c9ZtaOwIhdFc18RvodNJOVnV0nLeYPTK77GASJdIE-wZAjTeoizXyORhsg2JR87zi9rK3evkcNPrF_4exQQu-DkOvrgqnFWWYgyuXLLjs7hETz2vLmWPJOX5sX2rQajuz-aBKdhWmuzQL6MEkAMr81kRpapjpo40SBi5PK0aF1JdYPkVufSwqq4oreHENIRFAhCVEqa4S3w9yNsfBeAWDK7d9NF6wJpZRsxyw68IsqZenWNRtRxfqslLSsdykvA4pSH0deC1vkE2i-gIlWCGXmIfAoEBGoO3EM0uxkQaDobNzjiMa6vAfUIOdAYvVWukNS9-e_OfWTapjV24s58WlUEzxGgKHavMLv8XHDYAnpav0EH_ZPLnZLBfXXWfcDl5kl7jD62rmwYqeQRl4g7sGX3P-AFBExcXhBVq4ChINNxuiIAc-79WqRPVMGJQmnO-6J5zZtWu6wImW_a9kCNLDUBjNfy_y5s-VGwCsoJ-5f7GSilLH-QQAH_kpWGU78kL9wtDjl8s4-rx7ClqP0BLVBRVGOusrPKFDZjzLnzisyIB8SIUeNS4b7Zxm6GHG-E1Ud0eC9kirJHJZPhGBHlnC7CINLZh76Hm5YfgCZnojfxYvvhCFQY4DYdlTG0NOgBBA2KoKXrGJebE0GrJMgS-0uFEdQW4wL6GuzRTgWTqoRbmimFFrp4MSiOTLzt8OqE0Pyr58liwguMBYWf0bO_uo6XlhmKc4OveEd2XJzeFuFL_ROM26a9lvIXzDTdRu0JG6s7gJR4GYyCfYB5buq-6ULKkHGW4kxyR31stUCA2FIwwNcF8LoYihNFULWiYN6bsx5AGK4XC0W810kK4q-FKr0FJi0x-UKGeKaVySWfp72NviffbNQAjjmsLL71UIYeX4OsVL3IIlAGgUhY624dWejQA2b-gDJ_ynhV03umdmlfLGPqppQpQX8S7XxyDfE8v6ZUqYVi7-373PJPkJrCBt_qe4DikKDEhtUcvNj8Gpx78YNq6p_tVlYrD0YfGAHszjzKa0rpdlizkAcj4TESzcD1inE4c6_PBG9Yv7DqSxbt8OGQ5ibZJpM8IPY0gMtV3u3rzKgZ5B-il0YV36bqVfr-UIBfW6knnG7GcNLE-86E_rRKo8T8h3S1lbfN6kMTrhbuVJPRIo1OxTTq2uYerpsJkh-ZTlXnYow64maeHCtmPz9pAQWrssL0GlpFdjY6Xy6KgAz2ACyw-vA0_1x3qmnr_-jIFMYE9b6FqIU_AFFxssdFVlMwb6LMpTRoFOTXo-EU3Df3GGRwbWfbArm8g7zLnuG4UFkond6oRBE-TeCfEgPJmiNMh-Bg5netluBBnV9QgzIJC63iYjzd2imM5WU24s1BC1T68FvRpgazzCUxY37sSZNAk6a-GLO55u5oyy6hOdMnf1QKsdfo7ao2OfRNlPX1keiOujnzx4BqpT4R_dB6HPWfclSjzA_BpkNKy_0QboBNE_TZ2TVVyZQos-Ad3erGnlOrAxE48sR-EQUc84s7PNIzW-Mdqoqjs7OSwJJNKeQsu-DAftUsCM1c-A5mTUPCR8gxZCnihfDJXAUWCUZ0dE3-wzNNt8jIsopG6W1q6I2yVRCp9QYHUZV-m0kjuLQXKLJ-dO8FCYXOoUiEKlRBySU3W-RtnrkyMHF_fWTrUi3P15wQiSIEoiUKEEYU5-TcNK7iiL2CLukRKv9hHQdI0NIPWjiE4mZE9dyX5hQ5Ujgo9EeTZHiTkpJoCvgAMgJHN-q_fJHHtrzPzD5JHvKDrKOfJieM9cnZivWg6Hb9qSQtPc4Ic9kdWLko1bQxYFa0AA3imCJWhH0XZ9dCp9w9JtYcWF4eVr3CN_C_bkqP1ZnJ9QgJWm4KZWGIeS36k2soZ8kmEHbqxBrHwt44rGPit3pqac-KylrYcP5bP-08_PX_ESg8GyyTHJ5pcwaaYzORkEPStgFtXwI0GPu0CHic29tuEPaZZFBt5t4ifLrT8pNpJJfaJEuFlTIZtrCUUEbyGu_b4FLQEWCiVxn0kjrAhAqdsoWjguRpb0p8rJ1kXHzwN2TUoca6HP6yBsQodZ9fmiEOl9uGjlmHZ0iA_gH6KIwU9ilgJrbTow7X8pHHwuB6jVfY7rxyB-YG9O0WvPL6AlcYpE6j8LZIM1s7CvqQglCwoMXOO7p0tKMmqgvCldceHGCaaNP1uXKrqiqKz3AkuAb2-3LQ3E-vQEgExspc7hyhlYQNZBirRYZDDadalf8dn0pVSuf37umed1fgA3J2eDAs385qrp2yP_9d4vBVPaSojoYxKp9HtftN3nr55mg53394U-5teKU98ww7pjKJ7A1WygX1HVZhdVojUZDG7lL6HGVnE9RMC-lE8heosMTT7n_7Vin3EdDYKmcSoCLF-aQ6dAbjAP2w3OfObTxPq4sZVb5PuBGuDhA-i0mXCsKMuWCoV9HtuVISfoEOTTh6nP93CATwj50y3uinDEQLsURiOX6bsJJeGXJk3yk2sOpXcCVMccJRYv7fmLr6NdWrokKtKPh45vaX0-Le7l0nX3dI0soOdQMMlgjLzk-vznjFjJszCwnvIt2cxYYSqt7UQlQ9oo6CswxoO8JnRA2ZkCLFhRAjUdSVcTrQh-S_CbdYeI8itWkQLKg-gdnHL7X1xNLIGJvrhWBz6njhXaWCXzMasyOcFJ2x0f2JiRjzmp6d4sLCpKZZE1RzFYZEmf-MpYdhpFovqxalgwFgZJI4MgMOQC60PnS-b9a9rVu7g-xJ4wJNvn0D2583Uhec7wv9zRMuV_ch6tvtGTktM_JhtwVFhIpOsXTCP5z5UoqXKKdxfSRMBTJcd5iG5W7avLn4JWHrEc-h6t8e0kcJ0UVn4abfPO9bi-a8wDhotPnR26CxM5EUTYWY7v4kg0LfjsI6_5POHplHEwL7SyETGhkGaSpXcJ1dcatZA7pk1IbFhBnv4kD1FBkB-SQlXwXc3GgQMgUR7tFJjvCYr0_fD2zImIfcH0Po7b6QOUqIWxrmWzKkBCdLcC2u9O-FlU33NGnSpRiADVng6g-W6tBdo1yniuSSSYAsopl2p1QCMtRcxOFoE2NYXWSd_-3VVxT0OlrAvMhtKzFbovcuZswhpfjIuUDsEHvMJM&sh=d687c3300dfcee271db3893c4c6d8974&aw=UHPUTETaAsUM-1-7e1f97f6482b1628"""), Encoding.UTF8, "application/json");
                    //wro.content = stringContent;
                    wro.needProxy = true;
                    wro.headers.Add(new RequestHeader() { key = "User-Agent", value = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36" });
                    //wro.headers.Add(new RequestHeader() { key = "Referer", value = "https://hidemy.name/ru/proxy-list/?anon=234&start=64&__cf_chl_tk=n.aMgNR.7sr7kc6.pCc1qrZdGlz9F8vADVHacGjTcRY-1688559957-0-gaNycGzNDNA" });
                    //wro.headers.Add(new RequestHeader() { key = "Cookie", value = "PAPVisitorId=a1cbe1c89dcec6dae37d3f38a38ckJ4k; _ym_uid=168867836723066523; _ym_d=1688678367; _tt_enable_cookie=1; _ttp=CWajkhyojFxP1H9ixqIBTPSVV6X; _gid=GA1.2.1722204398.1688678368; _fbp=fb.1.1688678368096.1730104395; _ym_isad=2; _ga=GA1.1.1078954233.1688678367; _ga_KD5MHSSLTF=GS1.2.1688678368.1.1.1688678900.20.0.0; _ga_KJFZ3PJZP3=GS1.1.1688678367.1.1.1688678907.0.0.0" });

                    /*
                    wro.headers.Add(new() { key = "cookie", value = "cf_chl_2=cb4b7e23889a115"});
                    wro.headers.Add(new() { key = "user-agent", value = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36" });
                    wro.needUserAgent = false;   
                    wro.needProxy = false;
                    wro.proxy.ip = "";//"51.158.169.52";
                    wro.proxy.port = "";//"29976";
                    wro.proxy.protocol = "";//"HTTP";
                    wro.proxy.domain = "";//"https://hidemy.name";
                    */

                    wro.attempts = 1;

                    GetWebInfo(wro);
                    
                    if (wro.requestDone)
                    {
                        try { str = wro.textResponse(); } catch { }
                        goodProxy = wro.proxy;
                        break;
                    }
                    else
                        goodProxy = null;
                }

                var document = htmlParser.ParseDocument(str);  // SOCKS4://182.52.19.252:3629 Uyukin SOCKS5://161.8.174.48:1080 SOCKS5://5.161.93.53:1080 http://176.192.70.58:8010 http://218.253.141.178:8080
                // http://176.192.70.58:8006 http://79.175.51.212:29205
                var res = document.QuerySelectorAll("tbody tr");
                var protList = new List<string>() { "HTTP", "SOCKS4A", "SOCKS4", "SOCKS5" };
                foreach (var pr in res)
                {
                    string prot = pr.QuerySelectorAll("td:nth-last-child(3)")[0].TextContent.Trim();
                    foreach (var p in protList)
                    {
                        if (prot.Contains(p, StringComparison.OrdinalIgnoreCase))
                        {
                            prot.Replace(p, "",StringComparison.OrdinalIgnoreCase);
                            var np = new Proxy();
                            np.ip = pr.QuerySelectorAll("td:first-child")[0].TextContent.Trim();
                            np.port = pr.QuerySelectorAll("td:nth-child(2)")[0].TextContent.Trim();
                            np.lastCheckS = pr.QuerySelectorAll("td:last-child")[0].TextContent.Trim();
                            np.defence = pr.QuerySelectorAll("td:nth-last-child(2)")[0].TextContent.Trim();
                            np.delayS = pr.QuerySelectorAll("td p")[0].TextContent.Trim();
                            np.location = (pr.QuerySelectorAll("td span.country")[0].TextContent.Trim() + ", " + pr.QuerySelectorAll("td span.city")[0].TextContent.Trim()).Trim(new char[] { ' ', ',' });
                            np.protocol = p; // pr.QuerySelectorAll("td:nth-last-child(3)")[0].TextContent.Trim().Replace("HTTP, HTTPS", "HTTP").Replace("HTTPS", "HTTP").Replace("SOCKS4, SOCKS5", "SOCKS5").Replace("HTTP, SOCKS5", "HTTP"); //"HTTP";
                            pl.Add(np);
                        }
                    }
                }

                if (res.Count() < 64)
                    break;
            }
            Console.WriteLine($"Total proxies={pl.Count}");
            FindKnownProxies(pl);
            CheckProxyList(pl.Where(x => (x.status ?? false)).ToList(),30);
        }

        public static void get_proxyscrape_list()
        {
            string uri = "";
            var reqGen = () => {
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(uri),
                    Method = HttpMethod.Get,
                };
                AddUserAgent(request);
                return request;
            };
            string cont = "Only_chars:0 1 2 3 4 5 6 7 8 9 \r \n . :";
            var contArr = cont.Replace("Only_chars:", "").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var checkResp = (string st) =>
            {
                var s = st;
                foreach (var c in contArr)
                    s = s.Replace(c, "");
                if (s.Length == 0)
                    return st;
                else
                    return "";
            };

            var profList = new List<proxyscrapeProfile>() { 
            new proxyscrapeProfile() 
            { url = "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks5&timeout=10000&country=all", 
              protocol = "SOCKS5", defence = "Высокая" },
            new proxyscrapeProfile()
            { url = "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks4&timeout=10000&country=all", 
              protocol = "SOCKS4", defence = "Высокая"},
            new proxyscrapeProfile()
            { url = "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=http&timeout=10000&country=all&ssl=all&anonymity=elite", 
              protocol = "HTTP", defence = "Высокая" },
            new proxyscrapeProfile()
            { url = "https://api.proxyscrape.com/v2/?request=displayproxies&protocol=http&timeout=10000&country=all&ssl=all&anonymity=anonymous", 
              protocol = "HTTP", defence = "Средняя" }};


            curCT = $"Proxyscrape({Thread.CurrentThread.ManagedThreadId})";
            var pl = new List<Proxy>();
            var htmlParser = BrowsingContext.New(Configuration.Default).GetService<IHtmlParser>();

            string str = "";
            Console.Title = curCT;

            var wdt = DateTime.Now;
            pl.RemoveAll(x => true);

            //(_, var proxy) = GetDBParam(DBParam.HideMyNameProxy);
            Proxy_to_Domain? goodProxy = null;
            foreach (var pr in profList)
            {
                str = "";
                uri = pr.url;
                /*
                while (checkResp(str).Length==0)
                    str = GetWebInfoSysOld(reqGen, @"HTTP://109.231.146.106:31586"); // HTTP://51.158.169.52:29976  SOCKS5://37.187.133.177:56538 HTTP://46.99.205.2:8080"
                */
                while (true)
                {
                    WebRequestOrder wro = new() { url = uri };
                    wro.proxy = goodProxy is null ? wro.proxy : goodProxy;
                    //wro.contentKeys.Add("class=country");
                    wro.attempts = 100;
                    wro.priority = Priority.Low;
                    /*
                    wro.proxy.ip = "109.231.146.106";
                    wro.proxy.port = "31586";
                    wro.proxy.protocol = "HTTP";
                    wro.proxy.domain = "https://api.proxyscrape.com";
                    */
                    GetWebInfo(wro);
                    if (wro.requestDone)
                    {
                        if (wro.response is not null)
                        {
                            try { str = wro.textResponse(); } catch { }
                            if (checkResp(str).Length > 0)
                            {
                                try { str = wro.textResponse(); } catch { }
                                goodProxy = wro.proxy;
                                break;
                            }
                        }
                    }
                    else
                        goodProxy = null;
                }

                var prawl = str.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var praw in prawl)
                {
                    var ipp = praw.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    var np = new Proxy();
                    np.ip = ipp[0];
                    np.port = ipp[1];
                    np.protocol = pr.protocol;
                    np.defence = pr.defence;
                    np.lastCheckS = "";
                    np.delayS = "";
                    np.location = "";
                    pl.Add(np);
                }
            }
            Console.WriteLine($"Total proxies={pl.Count}");
            FindKnownProxies(pl);
            CheckProxyList(pl.Where(x => (x.status ?? false)).ToList(), 30);
        }
        public static void get_free_proxy_cz_list()
        {
            string uri = "";
            var reqGen = () => {
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(uri),
                    Method = HttpMethod.Get,
                };
                AddUserAgent(request);
                return request;
            };
            curCT = $"Free_proxy_cz({Thread.CurrentThread.ManagedThreadId})";
            var pl = new List<Proxy>();
            var htmlParser = BrowsingContext.New(Configuration.Default).GetService<IHtmlParser>();

            string str = "";
            Console.Title = curCT;

            var wdt = DateTime.Now;

            pl.RemoveAll(x => true);
            (_, var proxy) = GetDBParam(DBParam.HideMyNameProxy);
            for (var h = 0; true; h++)
            {
                uri = @"http://free-proxy.cz/en/proxylist/main/";
                uri += $"{h + 1}";//#list &start={h*64}  {h+1}
                str = "";
                /*
                while (!str.Contains("Free Proxy List")) //class=country   Free Proxy List
                    str = GetWebInfoSysOld(reqGen, $"");  //proxy  http://46.42.16.245:31565 http://51.158.169.52:29976//, "class=country" http://157.100.12.138:999 http://176.192.70.58:8006 http://211.138.6.37:9091
                */

                while (true)
                {
                    WebRequestOrder wro = new() { url = uri };
                    wro.contentKeys.Add("Free Proxy List");
                    wro.priority = Priority.Low;

                    GetWebInfo(wro);
                    if (wro.requestDone)
                    {
                        try { str = wro.textResponse(); } catch { }
                        break;
                    }
                }

                var document = htmlParser.ParseDocument(str);  // SOCKS4://182.52.19.252:3629 Uyukin SOCKS5://161.8.174.48:1080 SOCKS5://5.161.93.53:1080 http://176.192.70.58:8010 http://218.253.141.178:8080
                // http://176.192.70.58:8006
                var res = document.QuerySelectorAll("tbody tr");
                foreach (var pr in res)
                {

                    if (pr.QuerySelectorAll("td").Length < 11) continue;
                    var np = new Proxy();
                    np.ip = pr.QuerySelectorAll("td")[0].TextContent.Trim();
                    if (!np.ip.Contains("Base64"))
                        continue;
                    np.ip = Encoding.UTF8.GetString(Convert.FromBase64String(Regex.Replace(np.ip, ".+\"([^']+)\".+", "$1")));
                    np.port = pr.QuerySelectorAll("td")[1].TextContent.Trim();
                    np.protocol = pr.QuerySelectorAll("td")[2].TextContent.Trim(); //lastCheckS
                    np.location = pr.QuerySelectorAll("td")[3].QuerySelectorAll("div a")[0].TextContent.Trim(); //defence
                    np.location += $" {pr.QuerySelectorAll("td")[4].TextContent.Trim()}";
                    np.location += $" {pr.QuerySelectorAll("td")[5].TextContent.Trim()}";
                    np.defence = pr.QuerySelectorAll("td")[6].TextContent.Trim(); //delayS
                    np.Speed = pr.QuerySelectorAll("td")[7].InnerHtml;
                    if (np.Speed.Contains("icon-question")) np.Speed = ""; else np.Speed = pr.QuerySelectorAll("td")[7].TextContent.Trim();
                    np.UpTime = pr.QuerySelectorAll("td")[8].TextContent.Trim();
                    np.delayS = pr.QuerySelectorAll("td")[9].TextContent.Trim();
                    np.lastCheckS = pr.QuerySelectorAll("td")[10].TextContent.Trim();

                    pl.Add(np);
                }

                if (res.Count() < 36)
                    break;
            }
            Console.WriteLine($"Total proxies={pl.Count}");
            FindKnownProxies(pl);
            CheckProxyList(pl.Where(x=>(x.status ?? false)).ToList());
        }
        public static void CheckDeadProxies(int maxThreads=100)
        {
            //return;
            curCT = $"Dead proxy checker({Thread.CurrentThread.ManagedThreadId})";
            Console.Title = curCT;
            var dpidl = GetDeadProxyDB_List();//.Where(x => x.location?.Length == 0).ToList();
            //var pidl = GetProxyDB_partList();
            var str = JsonConvert.SerializeObject(dpidl);
            var pl = JsonConvert.DeserializeObject<List<Proxy>>(str);
            pl=pl?.OrderBy(x => x.lastCheck).ToList();
            CheckProxyList(pl, maxThreads);
        }

        public static void CheckProxies(int maxThreads = 100)
        {
            //return;
            curCT = $"Proxy checker({Thread.CurrentThread.ManagedThreadId})";
            Console.Title = curCT;
            var pl = GetProxyDB_List(); //.Where(x=>x.location?.Length==0).ToList();
            pl = pl?.OrderBy(x => x.lastCheck).ToList();
            CheckProxyList(pl, maxThreads);
        }
        public static void FindKnownProxies(List<Proxy> pl)
        {
            int tot = 0;
            int i = 0;
            var pidl = GetProxyDB_partList();
            var dpidl = GetDeadProxyDB_partList();

            var lt = DateTime.Now;

            foreach (var pr in pl)
            {
                tot++;
                if ((i = dpidl.FindIndex(x => x.ip == pr.ip && x.port == pr.port && (x.protocol ?? "Null").Contains(pr.protocol ?? "Null"))) >= 0)
                {
                    i = dpidl[i].id;
                    Console.WriteLine($"({tot} of {pl.Count})->({i})_________DeadProxy________________________");
                    pr.status = false;
                    continue;
                }
                if ((i = pidl.FindIndex(x => x.ip == pr.ip && x.port == pr.port && (x.protocol ?? "Null").Contains(pr.protocol ?? "Null"))) >= 0)
                {
                    i = pidl[i].id;
                    Console.WriteLine($"({tot} of {pl.Count})->({i})__________________________________________");
                    pr.status = false;
                    continue;
                }
                tot--;
                pr.status = true;
            }
        }
        public static void CheckProxyList(List<Proxy>? pl, int maxThreads = 100)
        {
            if (pl == null || pl.Count <= 0) return;
            int tot = 0;
            int i = 0;
            var col = Console.BackgroundColor;
            var npCount = pl.Count;

            Console.WriteLine($"Total number of proxies to check = {npCount}");
            var ChP_task = new Task<Proxy>[npCount <= maxThreads ? npCount : maxThreads]; i = 0;

            for (i = 0; i < ChP_task.Length; i++)
            {
                var il = i;
                var pr = pl[il];
                ChP_task[il] = Task.Factory.StartNew(() => CheckProxy(pr), TaskCreationOptions.LongRunning);
                npCount--;
            }


            //foreach (var pr in pl.Where(x => x.status == true))
            //    ChP_task[i++] = Task.Factory.StartNew(() => CheckProxy(pr), TaskCreationOptions.LongRunning);

            Console.BackgroundColor = col;
            while (ChP_task.Length > 0)
            {
                Console.WriteLine($"ThreadId={Environment.CurrentManagedThreadId},ChP_task.Count={ChP_task.Length}");
                i = Task.WaitAny(ChP_task);
                var pr = ChP_task[i].Result;
                if (npCount > 0)
                {
                    var pln = pl[pl.Count - npCount--];
                    ChP_task[i] = Task.Factory.StartNew(() => CheckProxy(pln), TaskCreationOptions.LongRunning);
                }
                else
                    ChP_task[i] = null!;

                ChP_task = ChP_task.Where(x => x is not null).ToArray();
                tot++;
                Console.WriteLine($"({tot} of {pl.Where(x => x.status == true).Count()})->______________________________________________");
                pr.tested ??= 0;
                pr.testOK ??= 0;
                if (pr.rate > 0)
                {
                    pr.location = pr.location?.Replace("'", "`");
                    pr.delay = pr.delayms;
                    pr.lastCheck = pr.pauseTill = DateTime.Now;
                    pr.insertDateTime = pr.insertDateTime ?? pr.lastCheck;
                    pr.login = pr.login ?? "";
                    pr.pass = pr.pass ?? "";
                    pr.tested++;
                    pr.testOK++;
                    //pr.lastRate = pr.rate;// (rate * 1000)/13;
                    pr.status = true;
                    pr.inUse = false;
                    i = ProxyAddNew(pr) ?? -1;
                }
                else
                {
                    var dpr = new DeadProxy();
                    dpr.ip = pr.ip;
                    dpr.port = pr.port;
                    dpr.protocol = pr.protocol;
                    dpr.defence = pr.defence;
                    dpr.location = pr.location?.Replace("'", "`");
                    dpr.delay = 30000;
                    dpr.lastCheck = DateTime.Now;
                    dpr.insertDateTime = pr.insertDateTime ?? dpr.lastCheck;
                    dpr.login = pr.login ?? "";
                    dpr.pass = pr.pass ?? "";
                    dpr.tested = pr.tested+1; 
                    dpr.testOK = pr.testOK ?? 0;
                    dpr.rate = pr.rate;
                    dpr.lastRate = pr.lastRate;
                    dpr.status = true;
                    i = DeadProxyAddNew(dpr) ?? -1;
                }

                if (pr.rate > 0)
                {
                    if (pr.rate <= pr.lastRate)
                        Console.BackgroundColor = ConsoleColor.DarkGreen;
                    else
                        Console.BackgroundColor = ConsoleColor.DarkYellow;
                }
                else
                    Console.BackgroundColor = ConsoleColor.DarkRed;

                Console.WriteLine($"({i})_{pr.protocol}//:{pr.ip}:{pr.port} delay:{pr.delayS}, security:{pr.defence}, location:{pr.location}, lastCheck:{pr.lastCheckS}");
                Console.BackgroundColor = ConsoleColor.Black;
            }
        }
        static Proxy CheckProxy(Proxy np)
        {
            np.rate = np.rate ?? 0;
            np.lastRate = 0;
            var del = new List<int>();
            HttpClient client = null!;
            var htmlParser = BrowsingContext.New(Configuration.Default).GetService<IHtmlParser>();
            string proxy = $"{np.protocol?.Replace("HTTPS","HTTP", (StringComparison)CompareOptions.IgnoreCase)}://{np.ip}:{np.port}";
            string res, st="", str = "", sts = "";
            var wdt = DateTime.Now;
            var ptd = new Proxy_to_Domain() { ip=np.ip!,port=np.port!,protocol=np.protocol!};
            /*
                        var updateDomain = (Proxy_to_Domain ptd_in_DB) =>
                        {
                            ptd.tested = 1;
                            ptd.lastCheck = DateTime.Now;
                            ptd.rate = ptd.testOK * 1000;
                            ptd.userAgent = "";

                            if (ptd_in_DB is not null)
                            {
                                if (ptd_in_DB.rate == 0 && ptd_in_DB.testOK == 0 && ptd.testOK > 0 && ptd_in_DB.tested < 9)
                                    ptd.rate = (9 + ptd.testOK - ptd_in_DB.tested) * 100;
                                else
                                    ptd.rate = (ptd_in_DB.rate * 9 + ptd.rate) / 10;

                                ptd.tested += ptd_in_DB.tested;
                                ptd.testOK += ptd_in_DB.testOK;
                                ptd.userAgent = ptd_in_DB.userAgent ?? "";
                                ptd.domain = ptd_in_DB.domain;
                            }
                            else
                                if (ptd.testOK == 0)
                                    return;

                            if ((ptd.userAgent is null || ptd.userAgent.Length == 0) && ptd.testOK > 0)
                                ptd.userAgent = RandomUA();

                            ProxyToDomainAddOrUpdate(ptd);
                        };
            */
            var getLocation = (bool withProxy) =>
            {
                Proxy pr = withProxy ? np : null!;
                str = TryProxy(pr, @"https://api.ipgeolocation.io/ipgeo?apiKey=a02da4c62f664902bedd36ce10357ba4&ip=" + np.ip, "city",client); //""
                if (str.Contains(PINGfailed)) str = "";
                if (str.Length > 0)
                {
                    try
                    {
                        st = (res = (string)JObject.Parse(str).SelectTokens("$.country_name").FirstOrDefault()! ?? "").Length > 0 ? res + ", " : "";
                        st += (res = (string)JObject.Parse(str).SelectTokens("$.state_prov").FirstOrDefault()! ?? "").Length > 0 ? res + ", " : "";
                        st += (res = (string)JObject.Parse(str).SelectTokens("$.city").FirstOrDefault()! ?? "").Length > 0 ? res + ", " : "";
                        st += (string)JObject.Parse(str).SelectTokens("$.district").FirstOrDefault()! ?? "";
                        st = st.Trim(new char[] { ' ', ',' });
                        Console.WriteLine($"Location={st}");
                        if (st.Length > 0)
                            sts = st;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());    
                        Console.WriteLine($"str={str}; st={st}; sts={sts}");
                        //Console.ReadLine(); 
                    }
                }
            };

            var delInc = (string site, string con, int inc) => {
                var wdtimer = DateTime.Now;
                if(inc>0)
                    str = TryProxy(np, site, con, client); //TryProxy(proxy, site, con);
                else
                    str = TryProxy(null!, site, con, client);  //str = TryProxy("", site, con);

                if (str.Length > 0)
                {
                    if (str.Contains($"{PINGfailed}"))
                    {
                        if (np.location?.Length == 0)
                        {
                            getLocation(false);
                            np.location = sts;
                        }
                        str = "";
                        return false;
                    }
                    //ptd.testOK = 1;
                    np.lastRate += inc;
                    del.Add((int)(DateTime.Now - wdtimer).TotalMilliseconds);
                    var bc = Console.BackgroundColor;
                    Console.BackgroundColor = ConsoleColor.DarkBlue;
                    Console.WriteLine($"{site} delay={del[del.Count - 1]}__/{proxy}/");
                    Console.BackgroundColor = bc;
                    if (site.Contains("sber")) Console.Beep();
                }
                //else
                //ptd.testOK = 0;

                //ptd.domain = GetDomain(site);
                //updateDomain(ProxyToDomainGet(ptd)?.FirstOrDefault()!);
                return true;
            };
/*
            if (!IsUrlDomainAlive(proxy, defaultTimeOut / 6))
            {
                if (np.location?.Length == 0)
                {
                    getLocation(false);
                    np.location = sts;
                }
                ptd.domain = "";
                var domainsToUpdate = ProxyToDomainGet(ptd);
                foreach (var domain in domainsToUpdate)
                {
                    ptd.testOK = 0;
                    updateDomain(domain);
                }
                return (np);
            }
*/
            delInc(@"https://voice-assistant.ru/", "GoogleWebApi", 1000); //if(!delInc(@"https://voice-assistant.ru/", "GoogleWebApi", 1000)) return np;
            delInc(@"http://zakazportretov.ru", "Uyukin",1000);
            delInc(@"https://kdveri24.ru", "kdveri24", 1000);
            delInc(@"https://sbermarket.ru/api/stores/26", "store", 4000);
            delInc(@"https://igooods.ru", "igooods", 2000);
            delInc(@"https://hidemyna.me/ru/proxy-list/", "class=country", 1000);
            delInc(@"https://d9ae6ad5-3627-4bf2-85a7-22bbd5549e94.selcdn.net/uploads/picture/picture/463837/mini_143958_120210122-10871-8ndfpp.jpg", "7CCCC20D0F705B1D816B26F9B9CCFCFF", 1000);

            //delInc(@"https://igstatic-a.akamaihd.net/uploads/picture/picture/100035/mini_4860009311561.JPG", "Gusarova_Anastasiya", 1000);

            delInc(@"https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks5&timeout=10000&country=all", "Only_chars:0 1 2 3 4 5 6 7 8 9 \r \n . :", 1000);
            /*
            delInc(@"http://smart-ip.net/", "Show on a map", 1000); // Не работает  (IP:193.178.146.17 Ukraine)
            if (str.Length > 0)
            {
                var document = htmlParser.ParseDocument(str);
                st = (res = document.QuerySelectorAll("[title='Show on a map']")[0].TextContent).Trim(new char[] { ' ', ',' });
                Console.WriteLine($"Location={st}, Delay={del.Where(x => x > 0).Min()}");
                sts = st;
            }
            */
            delInc(@"https://infobyip.com", "results wide home", 1000);
            if (str.Length > 0)
            {
                try
                {
                    var document = htmlParser.ParseDocument(str);
                    var ell = document.QuerySelectorAll("table.results.wide.home")[1].QuerySelectorAll("tr"); //[1].TextContent;
                    st = ell[1].QuerySelectorAll("td")[1].TextContent.Trim();
                    st += ", " + ell[2].QuerySelectorAll("td")[1].TextContent.Trim();
                    st += ", " + ell[3].QuerySelectorAll("td")[1].TextContent.Trim();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"AngleSharp1:{e.Message}");
                    Console.Beep(); Console.Beep(); Console.Beep();
                    Console.WriteLine($"str:{str}");
                }

                Console.WriteLine($"Location={st}");
                if (st.Length > 0)
                    sts = st;
            }

            /*  Пока не работает.
            delInc(@"https://suip.biz/ru/?act=myip", "Rev 1: ", 1000);
            if (str.Length > 0)
            {
                if (str.Contains("Rev 1: IP Address not found"))
                    st = "";
                else
                {
                    int p = str.IndexOf("</script><pre>");
                    p = str.IndexOf(", ", p + 14) + 2;
                    st = str.Substring(p, str.IndexOf("\n", p) - p);
                    p = str.IndexOf("Rev 1: ") + 7;  //7
                    p = str.IndexOf(",", p);
                    str = str.Substring(p, str.Length - p);
                    p = str.IndexOf(", "); p = str.IndexOf(", ", p + 2); p = str.IndexOf(", ", p + 2);
                    str = str.Substring(0, p);
                    st += str.Replace(", N/A", "");
                }
                st = st.Trim(new char[] { ' ', ',' });
                Console.WriteLine($"Location={st}, Delay={del.Where(x => x > 0).Min()}");
                if (st.Length > 0)
                    sts = st;
            }
            */

            delInc(@"https://api.find-ip.net", "ipbox", 1000); //http://smart-ip.net/geoip smart-grid-body
            if (str.Length > 0)
            {
                var document = htmlParser.ParseDocument(str);
                st = "";
                int d;
                try
                {
                    d = document.QuerySelectorAll("#ipbox div").Count();
                    for (var i = 1; i <= d; i += 2)
                    {
                        if (document.QuerySelectorAll($"#ipbox div:nth-child({i})")[0].TextContent.Replace("\n", "").Contains("Country"))
                            st += document.QuerySelectorAll($"#ipbox div:nth-child({i + 1})")[0].TextContent.Replace("\n", "");
                        if (document.QuerySelectorAll($"#ipbox div:nth-child({i})")[0].TextContent.Replace("\n", "").Contains("City"))
                        {
                            res = document.QuerySelectorAll($"#ipbox div:nth-child({i + 1})")[0].TextContent.Replace("\n", "");
                            st += st.Length == 0 ? res : ", " + res;
                        }
                        if (st.Length > 0)
                            while (st[st.Length - 1] == ' ') st = st.Substring(0, st.Length - 1);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"AngleSharp2:{e.Message}");
                    Console.Beep(); Console.Beep(); Console.Beep();
                    Console.WriteLine($"str:{str}");
                }
                st = st.Trim(new char[] { ' ', ',' });
                Console.WriteLine($"Location={st}");
                if (st.Length > 0)
                    sts = st;
            }

            /* Не работает
            delInc(@"https://whatleaks.com", "country_hide", 1000); //http://smart-ip.net/geoip smart-grid-body
            if (str.Length > 0)
            {
                var document = htmlParser.ParseDocument(str);
                st = (res = document.QuerySelectorAll("#main_ip_span + span")[0].TextContent).Contains("N/A") ? "" : res;
                st += (res = document.QuerySelectorAll("#top_table tbody tr:nth-child(3) span.txt")[0].TextContent).Contains("N/A") ? "" : ", " + res;
                st += (res = document.QuerySelectorAll("#top_table tbody tr:nth-child(2) span.txt")[0].TextContent).Contains("N/A") ? "" : ", " + res;
                st = st.Trim(new char[] { ' ', ',' });
                Console.WriteLine($"Location={st}, Delay={del.Where(x => x > 0).Min()}");
                if (st.Length > 0)
                    sts = st;
            } 
            */

            delInc(@"https://api.ipgeolocation.io/ipgeo?apiKey=a02da4c62f664902bedd36ce10357ba4&ip="+np.ip, "city", 1000); //https://whatleaks.site  http://smart-ip.net/geoip smart-grid-body
            getLocation(true);

            np.lastRate /= 15;
            np.rate = np.rate > 0 ? (np.rate * 9 + np.lastRate) / 10 : np.lastRate;
            if ((np.location is null) || (np.location.Length == 0))
                np.location = sts;
            if ((np.location is null) || (np.location.Length == 0))
            {
                getLocation(false);
                np.location = sts;
            }
            if (del.Count() > 0)
                np.delayms = del.Where(x => x > 0).Min();
            Console.WriteLine($"The End of ({Thread.CurrentThread.ManagedThreadId})[{np.lastRate}]_{np.ip}:{np.port}_{np.location}_");
            return (np);
        }
        static string TryProxy(Proxy proxy, string uri, string cont, HttpClient client=null!)
        {
            var str = "";
            /*var reqGen = () => {
                var request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(uri),
                    Method = HttpMethod.Get,
                };
                AddUserAgent(request);
                return request;
            };*/
            //str = GetWebInfoSysOld(reqGen,proxy);


            WebRequestOrder wro = new() { url = uri };
            if(!cont.Contains("Only_chars:"))
                wro.contentKeys.Add(cont);
            wro.needProxy=false;
            wro.attempts = 1;   
            if (proxy is not null)
            {
                wro.proxy.ip = proxy.ip!;
                wro.proxy.port = proxy.port!;
                wro.proxy.protocol = proxy.protocol!;
                wro.proxy.domain = GetDomain(uri);
                wro.proxy.client = client;
            }
            GetWebInfo(wro);
            client = wro.proxy.client ?? client;
            if (!wro.givenProxyPingOk)
                return $"{PINGfailed}";
            try { str = wro.textResponse(); } catch { }

            if (cont.Contains("Only_chars:"))
            {
                var contArr = cont.Replace("Only_chars:", "").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var s = str;
                foreach (var c in contArr)
                    s = s.Replace(c, "");
                if (s.Length==0)
                    return str;
                else
                    return "";
            }
            if (str.Contains(cont))
                return str;
            return "";
        }
        static string GetDomain(string site)
        {
            var dom = Regex.Replace(site, @"(?<dom>[^/]+(://)?[^/]+)/?.*", "${dom}");
            return dom;
        }
        static bool IsUrlDomainAlive(string url, int timeout = 4000, int att = 4)
        {
            var domain = Regex.Replace(url, @".+://", ""); // Remove protocol step1
            domain = Regex.Replace(domain, @"[/?].+", ""); // Remove path step2
            domain = Regex.Replace(domain, @":\d+", ""); // Remove port step3
            try
            {
                for (var i = 0; i < att; i++)
                {
                    Ping ping = new Ping();
                    PingReply reply = ping.Send(domain, timeout);
                    if(reply.Status == IPStatus.Success) 
                        return true;
                }
                return false;
            }
            catch { return false; }
        }
        public static string RandomUA()
        {
            string ua;
            while (!CheckUserAgent((ua = RandomUa.RandomUserAgent)));
            return ua;  
        }
        public static bool CheckUserAgent(string ua)
        {
            try
            {
                var request = new HttpRequestMessage();
                request.Headers.Add("User-Agent", ua);
                return true;
            }
            catch { return false; }
        }
    }
}


