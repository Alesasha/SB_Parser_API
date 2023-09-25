using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SB_Parser_API.Controllers;

using SB_Parser_API.MicroServices;
using static SB_Parser_API.MicroServices.DBSerices;
using static SB_Parser_API.MicroServices.SearchViaBarcode;
using static SB_Parser_API.MicroServices.WebAccessUtils;
using static SB_Parser_API.MicroServices.SB_Parsers;

using SB_Parser_API.Models;
//using SB_Parser_API.MicroServices;
using static SB_Parser_API.MicroServices.SB_API;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
//using SB_Parser_API.Models;
//using Microsoft.Extensions.Configuration;
//using System.Configuration;
//using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using static System.Web.HttpUtility;
using System.Text.Encodings.Web;
using System.Net;
using Jurassic;
using System.Web;

namespace SB_Parser_API
{
    class Shutdown
    {
        /// <summary>
        /// Windows restart
        /// </summary>
        public static void Restart()
        {
            StartShutDown("-f -r -t 5");
        }
        /// <summary>
        /// Log off.
        /// </summary>
        public static void LogOff()
        {
            StartShutDown("-l");
        }
        /// <summary>
        ///  Shutting Down Windows
        /// </summary>
        public static void Shut()
        {
            StartShutDown("-f -s -t 5");
        }
        private static void StartShutDown(string param)
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.FileName = "cmd";
            proc.WindowStyle = ProcessWindowStyle.Hidden;
            proc.Arguments = "/C shutdown " + param;
            Process.Start(proc);
        }
    }
    public class Program
    {
        //static int thisProcessId = 0;
        //static Process thisProcess;
        //static ProcessData thisPD;
        //static Active_Processes myAPR;

        //const int SWP_NOSIZE = 0x0001;
        //const int SWP_HIDEWINDOW = 0x0080;
        //const int SWP_SHOWWINDOW = 0x0040;

        //[DllImport("kernel32.dll")]
        //static extern IntPtr GetConsoleWindow();

        //[DllImport("user32.dll")]
        //static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        //const int SW_HIDE = 0;
        //const int SW_SHOW = 5;

        //[DllImport("kernel32.dll", ExactSpelling = true)]
        //private static extern IntPtr GetConsoleWindow();

        //private static IntPtr MyConsole = GetConsoleWindow();

        //[DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        //public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
        public static Task? reqQC;
        public static async Task Main(string[] args)
        {
            //GetShops(55.911865, 37.734596, 5.5).Wait();

            //Console.Title = "1222";
            //var MyConsole = GetConsoleWindow();
            //var cps = Process.GetProcesses().Where(x => x.MainWindowHandle == MyConsole);//IntPtr.Zero
            //var cp = Process.GetCurrentProcess();
            //Process.GetProcesses();

            //cp.WaitForInputIdle(2000);
            //cp.Refresh();           //var wh = cp.MainWindowHandle;
            //Process proc = Process.Start(new ProcessStartInfo { Arguments = "cons.exe", FileName = "cmd", WindowStyle = ProcessWindowStyle.Normal});
            //Console.WriteLine("Привет!");
            //SetWindowPos(wh, 0, 0, 0, 1000, 500, 0);//MyConsole
            //wh = Process.GetCurrentProcess().MainWindowHandle;
            //var si = new ProcessStartInfo();
            //si.FileName = @"cons.exe";
            //var pp= Process.Start(si);
            //var pp = new Process();

            //pp.StartInfo.FileName = @"cmd.exe"; //C:\Users\Alexander\source\repos\Conn\bin\Debug\net6.0\conn.exe
            //cons.exe C:\Users\Alexander\source\repos\SB_Parser_API\bin\Debug\net6.0\   @"\\LOCALHOST\c$\windows\system32\cmd.exe";
            //pp.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            //pp.StartInfo.UseShellExecute = true;
            //pp.StartInfo.CreateNoWindow = true;

            //pp.StartInfo.RedirectStandardInput = true;
            //pp.StartInfo.RedirectStandardOutput = false;
            //pp.StartInfo.RedirectStandardError = false;
            //pp.StartInfo.ErrorDialog = true;
            //pp.StartInfo.LoadUserProfile = true;
            //pp.StartInfo.Verb = "cmd.exe";
            //var pph = pp.Handle;
            //pp.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data); 
            //pp.EnableRaisingEvents = true;


            //pp.Start();

            //Console.OpenStandardOutput();



            //var wh = pp.MainWindowHandle;
            //var cp = Process.GetCurrentProcess();
            //var whcp = cp.MainWindowHandle;
            //cps = Process.GetProcesses().Where(x=>x.MainWindowHandle!=IntPtr.Zero); //x.MainModule!.ModuleName == "cons.exe"

            //Console.WriteLine("Привет!");

            //var w =pp.StandardInput;
            //var r = pp.StandardOutput;
            //for (var i = 0; i < 1000000; i++)
            //{
            //    w.Write($"({i}) <Приветы!!!>\r\n");
            //    Console.WriteLine("Привет---!");
            //    //Console.SetWindowPosition(0, 0);
            //    //Console.Clear();

            //    Thread.Sleep(1);
            //}
            //dynamic r= new Object();
            var r = new Retailer();
            Console.WriteLine($"Type={r.GetType().Name}");

            var db = new PISBContext();
            var et = db.Model.FindEntityType(r.GetType());// .GetEntityTypes().FirstOrDefault(x=> x.ClrType.Name == "Retailer");
            r.id = 2;
            //var rt = r.GetType();
            db.Remove(r);
            db.Add(r);
            var ret=db.Retailers.ToList();
            //db.SaveChanges();
            var em=et?.Model;
            //var dbSetProperties = db.GetType().GetProperties();
            var dbSet = (DbSet<Retailer>) db.GetType().GetProperties().Select(x => x.GetValue(db)).FirstOrDefault(x => x!.GetType().FullName!.Contains(r.GetType().Name))!;
            var dbst = dbSet.GetType().FullName;
            var t = r.GetType();
            Type rt = typeof(Retailer);
            var list = new List<Retailer>();
            list = (dbSet?.Local as IEnumerable<Retailer>)?.ToList();
            var list1 = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(r.GetType()))!;
            list1.Add(r);


            var dd = db.Model.GetRelationalModel;
            var ent = db.Entry(r);
            //db.Dispose();

            //request.Headers.Add("cookie", "PAPVisitorId=fef4517aef9eff87737ba5ac529a786i; PAPVisitorId=fef4517aef9eff87737ba5ac529a786i; _ym_uid=16592862331034702442; _ym_d=1659286233; _ga=GA1.2.1985235452.1659286233; _gid=GA1.2.1581002767.1659286233; _ym_isad=2; _tt_enable_cookie=1; _ttp=fe6e411e-0057-4c2c-baca-dbb7e45e5695; _gat_UA-90263203-1=1; _dc_gtm_UA-90263203-1=1");
            Console.BackgroundColor = ConsoleColor.Black;
            //if (OperatingSystem.IsWindows())
              //  Console.BufferHeight = 5000;

            InitDBSerices();
            productNames = GetProductNamesForSearch() ?? new();
            //goto m1;

            /*
            var httpClient = new HttpClient();
            string str;
            using (var Tstream = httpClient.GetStringAsync(@"https://api.proxyscrape.com/v2/?request=displayproxies&protocol=socks5&timeout=10000&country=all"))
            {
                Tstream.Wait();
                str=Tstream.Result;
          
                var stream = Tstream.Result;
                using (var fileStream = new FileStream(@"D:\psocks5.txt", FileMode.CreateNew))
                {
                    stream.CopyToAsync(fileStream).Wait();
                }
          
            }
            */

            reqQC = Task.Factory.StartNew(() => RequestQueueController(), TaskCreationOptions.LongRunning); //CheckDeadProxies(40);
            var rqcCheck = Task.Factory.StartNew(() =>
            {
                Task.Delay(60000).Wait();
                if (reqQC.IsCompleted || reqQC.IsFaulted || reqQC.IsCanceled)
                    reqQC = Task.Factory.StartNew(() => RequestQueueController(), TaskCreationOptions.LongRunning);

            }, TaskCreationOptions.LongRunning);

            //var bc = Utils.BarCodeCheck("03010615");
            //CollectRetailers();
            //await CollectShops(40);
            //InitDBSerices();
            //SB_API.categoryGet(33388);
            //return;
            //CollectProducts();
            //CollectProductProperties(true); // true - new products only 
            //GetCookieFromSelenium("", "");

            /*
            var engine = new Jurassic.ScriptEngine();
            var jsres = (string)engine.Evaluate(FindSolutionScript("10", HttpUtility.UrlDecode("ciJKT%2BOt2guzJ8iIpd8qL2rnnnI%3D")));
            Console.WriteLine(jsres);
            */

            var httpClient = new HttpClient();
            string str;

            var req = new HttpRequestMessage()
            {
                RequestUri = new Uri(@"https://sbermarket.ru/api/v2/products?q=&sid=9595&tid=&page=11&per_page=25&sort=popularity"),

                //&oirutpspca=1694430204000_bfff2c2f10bbf371ad32723f4a375c4b_f6ec265495bb87f5e8311c2bb61c1014
                //&oirutpspca=1694431124000_dbdaa16f6bd274bd59b54a5fae2473d1_f6ec265495bb87f5e8311c2bb61c1014

                Method = HttpMethod.Get
            };
            req.Headers.Add("Cookie", "ngenix_jscv_cd881f1695eb=session_id_e0227498=73b33a402d5a3cc7a7777e63bd6b63b7&bot_profile_check=true&payload=JnmR6wHepNzhrlMdnsFpHxCtcl1v%2FSSDpTj9i8v4PLY%3D&visitor_id_af50ddc3=aa5725750c1ec9cdc1d30970af441ca2&challenge_complexity=10&cookie_expires=1695557060&cookie_signature=TzPlOuSgmgPZ4MmbZ%2Bn%2FHssL%2FCk%3D");

            //req.Headers.Add("cookie", "spca=1694430204000_bfff2c2f10bbf371ad32723f4a375c4b_f6ec265495bb87f5e8311c2bb61c1014;");
            //                           spca=1694431124000_dbdaa16f6bd274bd59b54a5fae2473d1_f6ec265495bb87f5e8311c2bb61c1014


            req.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
            req.Headers.Add("Accept-Encoding", "gzip, deflate, br");
            req.Headers.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
            req.Headers.Add("Cache-Control", "max-age=0");
            req.Headers.Add("Referer", "https://sbermarket.ru/api/v2/products?q=&sid=9595&tid=&page=11&per_page=25&sort=popularity");
            //req.Headers.Add("Connection", "keep-alive");
            //req.Headers.Add("cookie", "spca=1694431124000_dbdaa16f6bd274bd59b54a5fae2473d1_f6ec265495bb87f5e8311c2bb61c1014");

            //req.Headers.Add("Host", "sbermarket.ru");

            //req.Headers.Add("Pragma", "no-cache");

            req.Headers.Add("Sec-Ch-Ua", (string?) null);
            req.Headers.Add("Sec-Ch-Ua-Mobile", "?0");
            req.Headers.Add("Sec-Ch-Ua-Platform", "\"\"");

            req.Headers.Add("Sec-Fetch-Dest", "document");
            req.Headers.Add("Sec-Fetch-Mode", "navigate");
            req.Headers.Add("Sec-Fetch-Site", "same-origin");
            //req.Headers.Add("Sec-Fetch-User", "?1");

            req.Headers.Add("Upgrade-Insecure-Requests", "1");
            req.Headers.Add("User-Agent", "Mozilla/7.0 (iPad; CPU OS 66_0 like Mac OS X) AppleWebKit/536.26 (KHTML, like Gecko) Version/6.0 Mobile/10A5355d Safari/8536.25");
            
            //req.Headers.Add("sec-ch-ua", "\"Chromium\";v=\"106\", \"Atom\";v=\"26\", \"Not;A=Brand\";v=\"99\"");
            //req.Headers.Add("sec-ch-ua-mobile", "?0");
            //req.Headers.Add("sec-ch-ua-platform", "\"Windows\"");


            var taskR = httpClient.SendAsync(req);
            taskR.Wait();

            var res = taskR.Result;
            var txt = res?.Content.ReadAsStringAsync().Result ?? "";

            using (var Tstream = httpClient.GetStringAsync(@"https://sbermarket.ru/api/v2/products?q=&sid=9595&tid=&page=1&per_page=23&sort=popularity"))
            {
                Tstream.Wait();
                str = Tstream.Result;

                var stream = Tstream.Result;
                using (var fileStream = new FileStream(@"D:\psocks5.txt", FileMode.CreateNew))
                {
                    //stream.CopyTo(fileStream);
                }
            }

            

            return;
            //GetShops(55.901223, 37.741567, 10).Wait();
            //GetDBParam(DBParam.TotalPriceScanners);
            //get_hidemy_name_list();

            //var ce =    IPAddress.Loopback;
            
            /*
            var chdpT = Task.Factory.StartNew(() => CheckDeadProxies(10), TaskCreationOptions.LongRunning); //CheckDeadProxies(40);
            var chpT = Task.Factory.StartNew(() => CheckProxies(40), TaskCreationOptions.LongRunning); //CheckProxies(40);
            
            while (true)
            {
                var nextStart = DateTime.Now.AddMinutes(120);
                get_hidemy_name_list();
                get_proxyscrape_list();
                if (nextStart < DateTime.Now)
                    nextStart = DateTime.Now.AddMinutes(2);
                Task.Delay((int)(nextStart-DateTime.Now).TotalMilliseconds).Wait();
            }
            */

            //var txt = GetInfoSys(reqGen, $"http://176.192.70.58:8006"); //http://46.42.16.245:31565 //, "class=country" http://157.100.12.138:999
            //SB_Parser_API.MicroServices.ZC_API.testAutoMapper();
            //InitDBSerices();
            /*
            dynamic dbc = GetDBContext(typeof(Retailer));
            var lr = dbc.Retailers;
            var ss = 43;
            ss += 12;
            */
            //CollectRetailers();
            //CollectShops().Wait();
            //CreateRequestAPI_3_0("");
            //var sl = productMultiSearch(55.908399, 37.730014, "молоко", Priority.Low);
            //GetShops(55.901223, 37.741567,10).Wait();
            //Init_AddOrUpdateProductsBatch();

            //Thread.Sleep(3600000);
appGo:
            var builder = WebApplication.CreateBuilder(args);
            productNames = GetProductNamesForSearch() ?? new();
            RegsStoresList = MosRegStoresGet() ?? new(); 
            productBarcodes = GetProductBarcodesForSearch() ?? new();



            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSignalR();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
#pragma warning disable ASP0014 // Suggest using top level route registrations
            _ = app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<ChatHub>("/chat");
            });
#pragma warning restore ASP0014 // Suggest using top level route registrations

            //app.UseAuthorization();

            app.MapControllers();
            app.UseDefaultFiles();
            app.UseStaticFiles();


            app.Run();
        }
    }
}