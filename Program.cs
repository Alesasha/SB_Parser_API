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
//using static SB_Parser_API.MicroServices.SB_API;
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

namespace SB_Parser_API
{
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

            //var en = et.Name;

            //var reqGen = () => {
            //    var request = new HttpRequestMessage(){
            //        RequestUri = new Uri("http://hidemy.name/ru/proxy-list/?anon=234"), //https://sbermarket.ru/api/stores/21 http://hidemy.name/ru/proxy-list/?anon=234
            //        Method = HttpMethod.Get,
            //    };
            //    request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Mozilla", "5.0"));// (Windows NT 10.0; rv:78.0) Gecko/20100101 Firefox/78.0")) ;  //(Windows NT 10.0; rv:78.0) Gecko/20100101 Firefox/78.0")
            //    request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("(Windows NT 10.0; rv:78.0)"));
            //    return request;
            //};

            //request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            //request.Headers.Host = "hidemy.name";
            //request.Headers.Add("authority", "hidemy.name");
            //var ua = 

            //request.Headers.Add("UserAgent" , "Mozilla/5.0 (Windows NT 10.0; rv:78.0) Gecko/20100101 Firefox/78.0");
            //request.Headers.Add("Referer", "http://hidemy.name/ru/proxy-list/?type=hs&anon=234");
            //request.Headers.Referrer = new Uri($"http://hidemy.name/ru/proxy-list/?anon=234");
            /*
            request.Headers.Add("cache-control", "no-cache");
            request.Headers.Add("pragma", "no-cache");
            request.Headers.Add("sec-ch-ua", "\" Not;A Brand\";v=\"99\", \"Google Chrome\";v=\"91\", \"Chromium\";v=\"91\"");
            request.Headers.Add("sec-ch-ua-mobile", "?0");
            request.Headers.Add("upgrade-insecure-requests", "1");
            request.Headers.Add("sec-fetch-site", "none");
            request.Headers.Add("sec-fetch-mode", "navigate");
            request.Headers.Add("sec-fetch-user", "?1");
            request.Headers.Add("sec-fetch-dest", "document");
            */

            //request.Headers.Add("cookie", "PAPVisitorId=fef4517aef9eff87737ba5ac529a786i; PAPVisitorId=fef4517aef9eff87737ba5ac529a786i; _ym_uid=16592862331034702442; _ym_d=1659286233; _ga=GA1.2.1985235452.1659286233; _gid=GA1.2.1581002767.1659286233; _ym_isad=2; _tt_enable_cookie=1; _ttp=fe6e411e-0057-4c2c-baca-dbb7e45e5695; _gat_UA-90263203-1=1; _dc_gtm_UA-90263203-1=1");
            Console.BackgroundColor = ConsoleColor.Black;
            //if (OperatingSystem.IsWindows())
              //  Console.BufferHeight = 5000;

            InitDBSerices();
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
            /*
            var isCorrect1 = CheckUserAgent("Mozilla/4.79 [en] (compatible; MSIE 7.0; Windows NT 5.0; .NET CLR 2.0.50727; InfoPath.2; .NET CLR 1.1.4322; .NET CLR 3.0.04506.30; .NET CLR 3.0.04506.648)");
            var isCorrect2 = CheckUserAgent("Mozilla/4.0 (compatible; MSIE 7.0b; Windows NT 5.1; .NET CLR 1.1.4322; InfoPath.1; .NET CLR 2.0.50727)");
            var PTDList = ProxyToDomainGet() ?? new List<Proxy_to_Domain>();
            var i=0;
            foreach (var ptd in PTDList)
            {
                i++;
                if (CheckUserAgent(ptd.userAgent ?? ""))
                {
                    //Console.WriteLine($"[{i}]Good UA :{ptd.userAgent}");
                    continue;
                }
                else
                {
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"[{i}]Bad UA :{ptd.userAgent}");
                    ptd.userAgent = RandomUA();
                    ProxyToDomainAddOrUpdate(ptd);
                    Console.BackgroundColor = ConsoleColor.DarkGreen;
                    Console.WriteLine($"[{i}]New UA :{ptd.userAgent}");
                }
            }
            Console.BackgroundColor = ConsoleColor.Black;
            */
            reqQC = Task.Factory.StartNew(() => RequestQueueController(), TaskCreationOptions.LongRunning); //CheckDeadProxies(40);
            var rqcCheck = Task.Factory.StartNew(() =>
            {
                Task.Delay(60000).Wait();
                if (reqQC.IsCompleted || reqQC.IsFaulted || reqQC.IsCanceled)
                    reqQC = Task.Factory.StartNew(() => RequestQueueController(), TaskCreationOptions.LongRunning);

            }, TaskCreationOptions.LongRunning);

            /*
            WebRequestOrder wro = new();
            wro.url = "https://hidemy.name/ru/proxy-list/?anon=234&start=128";
            wro.contentKeys.Add("class=country");
            GetWebInfo(wro);
            var txt = wro.textResponse();
            */
            //goto appGo;
            /*            var cs = new Task[] 
            { 
                CollectShops("",1,3000), //HTTP://185.15.172.212:3128
                CollectShops("", 3000, 6000), //SOCKS4://94.253.95.241:3629
                CollectShops("", 6000, 9000), //HTTP://176.192.70.58:8014
                CollectShops("", 9000, 12000), //HTTP://176.192.70.58:8017
                CollectShops("", 12000, 15000), //HTTP://176.192.70.58:8022
                CollectShops("", 15000, 18000),//HTTP://193.138.178.6:8282
                CollectShops("", 18000, 21000),//SOCKS4://5.188.64.79:5678
                CollectShops("", 21000, 24000), //HTTP://176.192.70.58:8029
                CollectShops("", 24000, 27000), //HTTP://176.192.70.58:8006
                CollectShops("", 27000, 30000),
                CollectShops("", 30000, 33000),
                CollectShops("", 33000, 36000),
                CollectShops("", 36000, 39000),
                CollectShops("", 39000, 44000),
            };
            
            Task.WaitAll(cs);
            */
            //var bc = Utils.BarCodeCheck("03010615");
            //CollectRetailers();
            //await CollectShops("",17000, 50000,40);
            //CollectProducts();
            //SB_API.categoryGet(33388);
            //return;
            //CollectProducts();
            //return;
            //GetShops(55.901223, 37.741567, 10).Wait();
            //GetDBParam(DBParam.TotalPriceScanners);
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
            InitDBSerices();
            /*
            dynamic dbc = GetDBContext(typeof(Retailer));
            var lr = dbc.Retailers;
            var ss = 43;
            ss += 12;
            */
            //CollectRetailers();
            //CollectShops().Wait();
            //CreateRequestAPI_3_0("");
            //GetShops(55.901223, 37.741567,10).Wait();

            //Thread.Sleep(3600000);
appGo:
            var builder = WebApplication.CreateBuilder(args);



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