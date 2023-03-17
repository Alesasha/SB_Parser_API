using Microsoft.Identity.Client;
using System.Net;
using static SB_Parser_API.Models.WebModels;

namespace SB_Parser_API.Models
{
    public class WebModels
    {
        public const int defaultTimeOut = 30000; //ms
        public const int MaxWebRequestTask = 140; //pcs.
        public const int MinimalProxyPool = 100; //pcs.
        public const int MaxProxyRequestInArow = 300; // times.
        //public const int MaxProxyErrorInArow = 3; // times.
        public const int defaultNumberOfProxySubstitutions = 20;
        public const string PINGfailed = "Proxy PING is failed";
        public const int ProxyToDomainListsRefreshInterval = 240; // seconds
        public const int ProxyPerQuiryThread = 10;
        public static List<WebRequestOrder> WebRequestOrderQueue { get; set; } = new();
        public static object WebRequestOrderQueueL { get; set; } = new();
        public static List<WebRequestTaskInfo> WebRequestTaskInfoList { get; set; } = new();
        //public static AutoResetEvent WebRequestOrderQueueAdd = new(false);
        public static AutoResetEvent RequestQueueControllerTimeToCheck { get; set; } = new(false);
        //public static List<Proxy_to_Domain> ProxyToDomainList { get; set; }  = null!;
        public record class ProxyToDomainContext
        {
            public List<Proxy_to_Domain> PD_List = new();
            public List<Proxy_to_Domain> PD_ListToUpdate = new();
            public int cursor = 0;
            public int proxiesInUse = 0;
        }
        public static Dictionary<string, ProxyToDomainContext> ProxyToDomainLists { get; set; } = null!;
        public static DateTime ProxyToDomainListsNextRefresh { get; set; } = DateTime.Now.AddSeconds(ProxyToDomainListsRefreshInterval);
    }
    /*
    public record class ProxyToDomainContext
    {
        public List<Proxy_to_Domain> PD_List = new();
        public List<Proxy_to_Domain> PD_ListToUpdate = new();
        public int cursor = 0;
        public int proxiesInUse = 0;
    }
    */
    public delegate void WRO_Action(WebRequestOrder x);
    public record class WebRequestOrder
    {
        public string url="";
        public HttpMethod method = HttpMethod.Get;
        public List<RequestHeader> headers = new();
        public HttpContent content = null!; // Content to be sent to the server associated with the url
        public AutoResetEvent completed = new(false);
        public List<string> contentKeys = new(); // Keywords that suppose to be as part in the server's response
        public string errorMsg = "";
        public bool needProxy = true;
        public Proxy_to_Domain proxy = new();
        public bool givenProxyPingOk = false;
        public bool needUserAgent = true;
        public bool pingOnly = false;
        public bool pingOk = false;
        public bool requestDone = false;
        public HttpResponseMessage response = null!;
        public int timeOut = defaultTimeOut;
        public int attempts = defaultNumberOfProxySubstitutions;
        public DateTime created = DateTime.Now;
        public Priority priority = Priority.Lowest;
        public string textResponse() { return response?.Content.ReadAsStringAsync().Result ?? ""; }
        public List<string> ResponseCookies() { return response.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value.ToList(); }
        public WRO_Action ProxyHeadersSetUp = null!;
    }
    public class RequestHeader
    {
        public string key = "";
        public string value = "";
    }
    public enum CMD : int
    {
        StandBy = 0, ExecuteRequest = 1, WriteDB = 2, Exit = 3, Exited = 4
    }
    public enum Priority : int
    {
        Highest = 0, High = 1, MediumHigh = 2, Medium = 3, MediumLow = 4, Low = 5, Lowest = 6
    }
    public record class WebRequestTaskInfo
    {
        public Task? task;
        public int taskNumber;
        public CMD command = CMD.StandBy;
        public AutoResetEvent checkCommand = new(false);
        public bool isResultReady = false;
        public WebRequestOrder webRequestOrder = null!;
    }
}
