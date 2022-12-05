using Microsoft.Identity.Client;
using System.Net;
using static SB_Parser_API.Models.WebModels;

namespace SB_Parser_API.Models
{
    public class WebModels
    {
        public const int defaultTimeOut = 30000; //ms
        public const int MaxWebRequestTask = 110; //pcs.
        public const int MinimalProxyPool = 30; //pcs.
        public const int defaultNumberOfProxySubstitutions = 20;
        public const string PINGfailed = "Proxy PING is failed";
        public static List<WebRequestOrder> WebRequestOrderQueue = new();
        public static object WebRequestOrderQueueL = new();
        public static List<WebRequestTaskInfo> WebRequestTaskInfoList = new();
        //public static AutoResetEvent WebRequestOrderQueueAdd = new(false);
        public static AutoResetEvent RequestQueueControllerTimeToCheck = new(false);
        public static List<Proxy_to_Domain> ProxyToDomainList = null!;
    }
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
        public HttpResponseMessage response=null!;
        public int timeOut = defaultTimeOut;
        public int attempts = defaultNumberOfProxySubstitutions;
        public DateTime created = DateTime.Now;
        public Priority priority = Priority.Low;
        public string textResponse() { return response?.Content.ReadAsStringAsync().Result ?? ""; }
        public List<string> ResponseCookies() { return response.Headers.SingleOrDefault(header => header.Key == "Set-Cookie").Value.ToList(); }
    }
    public class RequestHeader
    {
        public string key = "";
        public string value = "";
    }
    public enum CMD : int
    {
        StandBy = 0, ExecuteRequest = 1, Exit = 2, Exited = 3
    }
    public enum Priority : int
    {
        High = 0, Medium = 1, Low = 2
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
