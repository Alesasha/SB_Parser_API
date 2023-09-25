using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Caching.Memory;
using System.Collections;

namespace SB_Parser_API.MicroServices
{
    public class HTTPS_Proxy
    {

    }
    public sealed class ProxyServer
    {
        private static readonly ProxyServer _server = new ProxyServer();

        private static readonly int BUFFER_SIZE = 32768;
        private static readonly char[] semiSplit = new char[] { ';' };
        private static readonly char[] equalSplit = new char[] { '=' };
        private static readonly String[] colonSpaceSplit = new string[] { ": " };
        private static readonly char[] spaceSplit = new char[] { ' ' };
        private static readonly char[] commaSplit = new char[] { ',' };
        private static readonly Regex cookieSplitRegEx = new Regex(@",(?! )");
        private static X509Certificate2? _certificate;
        private static object _outputLockObj = new object();


        private TcpListener _listener;
        private Thread _listenerThread=null!;
        private Thread _cacheMaintenanceThread=null!;

        public IPAddress ListeningIPInterface
        {
            get
            {
                IPAddress? addr = IPAddress.Loopback;
                if (System.Configuration.ConfigurationManager.AppSettings["ListeningIPInterface"] != null)
                    IPAddress.TryParse(System.Configuration.ConfigurationManager.AppSettings["ListeningIPInterface"], out addr);

                return addr ?? IPAddress.Loopback;
            }
        }

        public Int32 ListeningPort
        {
            get
            {
                Int32 port = 8888;  // 8081
                if (System.Configuration.ConfigurationManager.AppSettings["ListeningPort"] != null)
                    Int32.TryParse(System.Configuration.ConfigurationManager.AppSettings["ListeningPort"], out port);

                return port;
            }
        }

        private ProxyServer()
        {
            _listener = new TcpListener(ListeningIPInterface, ListeningPort);
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
        }

        public Boolean DumpHeaders { get; set; }
        public Boolean DumpPostData { get; set; }
        public Boolean DumpResponseData { get; set; }

        public static ProxyServer Server
        {
            get { return _server; }
        }

        public bool Start()
        {
            try
            {
                String certFilePath = String.Empty;
                if (System.Configuration.ConfigurationManager.AppSettings["CertificateFile"] != null)
                    certFilePath = System.Configuration.ConfigurationManager.AppSettings["CertificateFile"] ?? "";
                try
                {
                    _certificate = new X509Certificate2(certFilePath,"");  //"q"
                }
                catch (Exception ex)
                {
                    throw new ConfigurationErrorsException(String.Format("Could not create the certificate from file from {0}", certFilePath), ex);
                }
                _listener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            _listenerThread = new Thread(new ParameterizedThreadStart(Listen));
            _cacheMaintenanceThread = new Thread(new ThreadStart(ProxyCache.CacheMaintenance));

            _listenerThread.Start(_listener);
            _cacheMaintenanceThread.Start();

            return true;
        }

        public void Stop()
        {
            _listener.Stop();

            //wait for server to finish processing current connections...

            _listenerThread.Interrupt(); //.Abort();
            _cacheMaintenanceThread.Interrupt(); //Abort();

            _listenerThread.Join();
            _cacheMaintenanceThread.Join();
            //_listenerThread.Join();
        }

        private static void Listen(Object? obj)
        {
            if (obj == null)   
                return;

            TcpListener listener = (TcpListener) obj;
            try
            {
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    while (!ThreadPool.QueueUserWorkItem(new WaitCallback(ProxyServer.ProcessClient), client)) ;
                }
            }
            catch (ThreadAbortException) { }
            catch (SocketException) { }
        }


        private static void ProcessClient(Object? obj)
        {
            if(obj == null) return; 

            TcpClient client = (TcpClient)obj;
            try
            {
                DoHttpProcessing(client);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                client.Close();
            }
        }

        //[Obsolete]
        private static void DoHttpProcessing(TcpClient client)
        {
            Stream clientStream = client.GetStream();
            Stream outStream = clientStream; //use this stream for writing out - may change if we use ssl
            SslStream? sslStream = null;
            StreamReader clientStreamReader = new StreamReader(clientStream);
            CacheEntry? cacheEntry = null;
            MemoryStream? cacheStream = null;

            if (Server.DumpHeaders || Server.DumpPostData || Server.DumpResponseData)
            {
                //make sure that things print out in order - NOTE: this is bad for performance
                Monitor.TryEnter(_outputLockObj, TimeSpan.FromMilliseconds(-1.0));
            }

            try
            {
                //read the first line HTTP command
                String httpCmd = clientStreamReader.ReadLine() ?? "";
                if (String.IsNullOrEmpty(httpCmd))
                {
                    clientStreamReader.Close();
                    clientStream.Close();
                    return;
                }
                //break up the line into three components
                String[] splitBuffer = httpCmd.Split(spaceSplit, 3);

                String method = splitBuffer[0];
                String remoteUri = splitBuffer[1];
                Version version = new Version(1, 0);

                HttpWebRequest webReq;
                HttpWebResponse response = null;
                if (splitBuffer[0].ToUpper() == "CONNECT")
                {
                    //Browser wants to create a secure tunnel
                    //instead = we are going to perform a man in the middle "attack"
                    //the user's browser should warn them of the certification errors however.
                    //Please note: THIS IS ONLY FOR TESTING PURPOSES - you are responsible for the use of this code
                    remoteUri = "https://" + splitBuffer[1];
                    while (!String.IsNullOrEmpty(clientStreamReader.ReadLine())) ;
                    StreamWriter connectStreamWriter = new StreamWriter(clientStream);
                    connectStreamWriter.WriteLine("HTTP/1.0 200 Connection established");
                    connectStreamWriter.WriteLine(String.Format("Timestamp: {0}", DateTime.Now.ToString()));
                    connectStreamWriter.WriteLine("Proxy-agent: matt-dot-net");
                    connectStreamWriter.WriteLine();
                    connectStreamWriter.Flush();

                    sslStream = new SslStream(clientStream, false);
                    try
                    {
                        sslStream.AuthenticateAsServer(_certificate, false, SslProtocols.Tls12 | SslProtocols.Tls13 | SslProtocols.Ssl3, true);
                        //sslStream.AuthenticateAsServer(_certificate);
                        //SslProtocols.Tls | SslProtocols.Ssl3 | SslProtocols.Ssl2 | SslProtocols.Tls12 | SslProtocols.Tls13

                    }
                    catch (AuthenticationException e)
                    {
                        sslStream.Close();
                        clientStreamReader.Close();
                        connectStreamWriter.Close();
                        clientStream.Close();
                        return;
                    }

                    //HTTPS server created - we can now decrypt the client's traffic
                    clientStream = sslStream;
                    clientStreamReader = new StreamReader(sslStream);
                    outStream = sslStream;
                    //read the new http command.
                    httpCmd = clientStreamReader.ReadLine() ?? "";
                    if (String.IsNullOrEmpty(httpCmd))
                    {
                        clientStreamReader.Close();
                        clientStream.Close();
                        sslStream.Close();
                        return;
                    }
                    splitBuffer = httpCmd.Split(spaceSplit, 3);
                    method = splitBuffer[0];
                    remoteUri = remoteUri + splitBuffer[1];
                }

                //construct the web request that we are going to issue on behalf of the client.
                webReq = (HttpWebRequest)HttpWebRequest.Create(remoteUri);
                webReq.Method = method;
                webReq.ProtocolVersion = version;

                //read the request headers from the client and copy them to our request
                int contentLen = ReadRequestHeaders(clientStreamReader, webReq);

                webReq.Proxy = null;
                webReq.KeepAlive = false;
                webReq.AllowAutoRedirect = false;
                webReq.AutomaticDecompression = DecompressionMethods.None;


                if (Server.DumpHeaders)
                {
                    Console.WriteLine(String.Format("{0} {1} HTTP/{2}", webReq.Method, webReq.RequestUri.AbsoluteUri, webReq.ProtocolVersion));
                    DumpHeaderCollectionToConsole(webReq.Headers);
                }

                //using the completed request, check our cache
                if (method.ToUpper() == "GET")
                    cacheEntry = ProxyCache.GetData(webReq);
                else if (method.ToUpper() == "POST")
                {
                    char[] postBuffer = new char[contentLen];
                    int bytesRead;
                    int totalBytesRead = 0;
                    StreamWriter sw = new StreamWriter(webReq.GetRequestStream());
                    while (totalBytesRead < contentLen && (bytesRead = clientStreamReader.ReadBlock(postBuffer, 0, contentLen)) > 0)
                    {
                        totalBytesRead += bytesRead;
                        sw.Write(postBuffer, 0, bytesRead);
                        if (ProxyServer.Server.DumpPostData)
                            Console.Write(postBuffer, 0, bytesRead);
                    }
                    if (Server.DumpPostData)
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                    }

                    sw.Close();
                }

                if (cacheEntry == null)
                {
                    Console.WriteLine(String.Format("ThreadID: {2} Requesting {0} on behalf of client {1}", webReq.RequestUri, client.Client.RemoteEndPoint?.ToString() ?? "", Thread.CurrentThread.ManagedThreadId));
                    webReq.Timeout = 15000;

                    try
                    {
                        response = (HttpWebResponse) webReq.GetResponse();
                    }
                    catch (WebException webEx)
                    {
                        response = (webEx.Response as HttpWebResponse)!;
                    }
                    if (response != null)
                    {
                        List<Tuple<String, String>> responseHeaders = ProcessResponse(response);
                        StreamWriter myResponseWriter = new StreamWriter(outStream);
                        Stream responseStream = response.GetResponseStream();
                        try
                        {
                            //send the response status and response headers
                            WriteResponseStatus(response.StatusCode, response.StatusDescription, myResponseWriter);
                            WriteResponseHeaders(myResponseWriter, responseHeaders);

                            DateTime? expires = null;
                            CacheEntry? entry = null;
                            Boolean canCache = (sslStream == null && ProxyCache.CanCache(response.Headers, ref expires));
                            if (canCache)
                            {
                                entry = ProxyCache.MakeEntry(webReq, response, responseHeaders, expires);
                                if (response.ContentLength > 0)
                                    cacheStream = new MemoryStream(entry.ResponseBytes);
                            }

                            Byte[] buffer;
                            if (response.ContentLength > 0)
                                buffer = new Byte[response.ContentLength];
                            else
                                buffer = new Byte[BUFFER_SIZE];

                            int bytesRead;

                            while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                if (cacheStream != null)
                                    cacheStream.Write(buffer, 0, bytesRead);
                                outStream.Write(buffer, 0, bytesRead);
                                if (Server.DumpResponseData)
                                    Console.Write(UTF8Encoding.UTF8.GetString(buffer, 0, bytesRead));
                            }
                            if (Server.DumpResponseData)
                            {
                                Console.WriteLine();
                                Console.WriteLine();
                            }

                            responseStream.Close();
                            if (cacheStream != null)
                            {
                                cacheStream.Flush();
                                cacheStream.Close();
                            }

                            outStream.Flush();
                            if (canCache)
                                ProxyCache.AddData(entry ?? new());
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        finally
                        {
                            responseStream.Close();
                            response.Close();
                            myResponseWriter.Close();
                        }
                    }
                }
                else
                {
                    //serve from cache
                    StreamWriter myResponseWriter = new StreamWriter(outStream);
                    try
                    {
                        WriteResponseStatus(cacheEntry.StatusCode, cacheEntry.StatusDescription, myResponseWriter);
                        WriteResponseHeaders(myResponseWriter, cacheEntry.Headers);
                        if (cacheEntry.ResponseBytes != null)
                        {
                            outStream.Write(cacheEntry.ResponseBytes, 0, cacheEntry.ResponseBytes.Length);
                            if (ProxyServer.Server.DumpResponseData)
                                Console.Write(UTF8Encoding.UTF8.GetString(cacheEntry.ResponseBytes));
                        }
                        myResponseWriter.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        myResponseWriter.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (Server.DumpHeaders || Server.DumpPostData || Server.DumpResponseData)
                {
                    //release the lock
                    Monitor.Exit(_outputLockObj);
                }

                clientStreamReader.Close();
                clientStream.Close();
                if (sslStream != null)
                    sslStream.Close();
                outStream.Close();
                if (cacheStream != null)
                    cacheStream.Close();
            }

        }

        private static List<Tuple<String, String>> ProcessResponse(HttpWebResponse response)
        {
            String? value = null;
            String? header = null;
            List<Tuple<String, String>> returnHeaders = new List<Tuple<String, String>>();
            foreach (String s in response.Headers.Keys)
            {
                if (s.ToLower() == "set-cookie")
                {
                    header = s;
                    value = response.Headers[s];
                }
                else
                    returnHeaders.Add(new Tuple<String, String>(s, response.Headers[s] ?? "" ));
            }

            if (!String.IsNullOrWhiteSpace(value))
            {
                response.Headers.Remove(header ?? "");
                String[] cookies = cookieSplitRegEx.Split(value);
                foreach (String cookie in cookies)
                    returnHeaders.Add(new Tuple<String, String>("Set-Cookie", cookie));

            }
            returnHeaders.Add(new Tuple<String, String>("X-Proxied-By", "matt-dot-net proxy"));
            return returnHeaders;
        }

        private static void WriteResponseStatus(HttpStatusCode code, String description, StreamWriter myResponseWriter)
        {
            String s = String.Format("HTTP/1.0 {0} {1}", (Int32)code, description);
            myResponseWriter.WriteLine(s);
            if (ProxyServer.Server.DumpHeaders)
                Console.WriteLine(s);
        }

        private static void WriteResponseHeaders(StreamWriter myResponseWriter, List<Tuple<String, String>> headers)
        {
            if (headers != null)
            {
                foreach (Tuple<String, String> header in headers)
                    myResponseWriter.WriteLine(String.Format("{0}: {1}", header.Item1, header.Item2));
            }
            myResponseWriter.WriteLine();
            myResponseWriter.Flush();

            if (Server.DumpHeaders)
                DumpHeaderCollectionToConsole(headers);
        }

        private static void DumpHeaderCollectionToConsole(WebHeaderCollection headers)
        {
            foreach (String s in headers.AllKeys)
                Console.WriteLine(String.Format("{0}: {1}", s, headers[s]));
            Console.WriteLine();
        }

        private static void DumpHeaderCollectionToConsole(List<Tuple<String, String>>? headers)
        {
            if (headers?.Any() ?? false) return;
            foreach (Tuple<String, String> header in headers!)
                Console.WriteLine(String.Format("{0}: {1}", header.Item1, header.Item2));
            Console.WriteLine();
        }

        private static int ReadRequestHeaders(StreamReader sr, HttpWebRequest webReq)
        {
            String httpCmd;
            int contentLen = 0;
            do
            {
                httpCmd = sr.ReadLine() ?? "";
                if (String.IsNullOrEmpty(httpCmd))
                    return contentLen;
                String[] header = httpCmd.Split(colonSpaceSplit, 2, StringSplitOptions.None);
                switch (header[0].ToLower())
                {
                    case "host":
                        webReq.Host = header[1];
                        break;
                    case "user-agent":
                        webReq.UserAgent = header[1];
                        break;
                    case "accept":
                        webReq.Accept = header[1];
                        break;
                    case "referer":
                        webReq.Referer = header[1];
                        break;
                    case "cookie":
                        webReq.Headers["Cookie"] = header[1];
                        break;
                    case "proxy-connection":
                    case "connection":
                    case "keep-alive":
                        //ignore these
                        break;
                    case "content-length":
                        int.TryParse(header[1], out contentLen);
                        break;
                    case "content-type":
                        webReq.ContentType = header[1];
                        break;
                    case "if-modified-since":
                        String[] sb = header[1].Trim().Split(semiSplit);
                        DateTime d;
                        if (DateTime.TryParse(sb[0], out d))
                            webReq.IfModifiedSince = d;
                        break;
                    default:
                        try
                        {
                            webReq.Headers.Add(header[0], header[1]);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(String.Format("Could not add header {0}.  Exception message:{1}", header[0], ex.Message));
                        }
                        break;
                }
            } while (!String.IsNullOrWhiteSpace(httpCmd));
            return contentLen;
        }
    }
    public class CacheKey
    {
        public String AbsoluteUri { get; set; }
        public String UserAgent { get; set; }
        public CacheKey(String requestUri, String userAgent)
        {
            AbsoluteUri = requestUri;
            UserAgent = userAgent;
        }
        public override bool Equals(object? obj)
        {
            CacheKey? key = obj as CacheKey;
            if (key != null)
                return (key.AbsoluteUri == AbsoluteUri && key.UserAgent == UserAgent);
            return false;
        }
        public override int GetHashCode()
        {
            String s = AbsoluteUri + UserAgent;
            return s.GetHashCode();
        }
    }
    public class CacheEntry
    {
        public CacheKey Key { get; set; } = null!;
        public DateTime? Expires { get; set; }
        public DateTime DateStored { get; set; }
        public Byte[] ResponseBytes { get; set; } = null!;
        public HttpStatusCode StatusCode { get; set; }
        public String StatusDescription { get; set; } = "";
        public List<Tuple<String, String>> Headers { get; set; } = new();
        public Boolean FlagRemove { get; set; }
    }
    public static class ProxyCache
    {
        private static Hashtable _cache = new Hashtable();
        private static Object _cacheLockObj = new object();
        private static Object _statsLockObj = new object();
        private static Int32 _hits;

        public static CacheEntry? GetData(HttpWebRequest request)
        {
            CacheKey key = new CacheKey(request.RequestUri.AbsoluteUri, request.UserAgent ?? "");
            if (_cache[key] != null)
            {
                CacheEntry entry = (CacheEntry) (_cache[key] ?? new());
                if (entry.FlagRemove || (entry.Expires.HasValue && entry.Expires < DateTime.Now))
                {
                    //don't remove it here, just flag
                    entry.FlagRemove = true;
                    return null;
                }
                Monitor.Enter(_statsLockObj);
                _hits++;
                Monitor.Exit(_statsLockObj);
                return entry;
            }
            return null;
        }

        public static CacheEntry MakeEntry(HttpWebRequest request, HttpWebResponse response, List<Tuple<String, String>> headers, DateTime? expires)
        {
            CacheEntry newEntry = new CacheEntry();
            newEntry.Expires = expires;
            newEntry.DateStored = DateTime.Now;
            newEntry.Headers = headers;
            newEntry.Key = new CacheKey(request.RequestUri.AbsoluteUri, request.UserAgent ?? "None");
            newEntry.StatusCode = response.StatusCode;
            newEntry.StatusDescription = response.StatusDescription;
            if (response.ContentLength > 0)
                newEntry.ResponseBytes = new Byte[response.ContentLength];
            return newEntry;
        }

        public static void AddData(CacheEntry entry)
        {

            Monitor.Enter(_cacheLockObj);
            if (!_cache.Contains(entry.Key))
                _cache.Add(entry.Key, entry);
            Monitor.Exit(_cacheLockObj);


        }

        public static Boolean CanCache(WebHeaderCollection headers, ref DateTime? expires)
        {

            foreach (String s in headers.AllKeys)
            {
                String value = headers[s]?.ToLower() ?? "";
                switch (s.ToLower())
                {
                    case "cache-control":
                        if (value.Contains("max-age"))
                        {
                            int seconds;
                            if (int.TryParse(value, out seconds))
                            {
                                if (seconds == 0)
                                    return false;
                                DateTime d = DateTime.Now.AddSeconds(seconds);
                                if (!expires.HasValue || expires.Value < d)
                                    expires = d;

                            }
                        }

                        if (value.Contains("private") || value.Contains("no-cache"))
                            return false;
                        else if (value.Contains("public") || value.Contains("no-store"))
                            return true;

                        break;

                    case "pragma":

                        if (value == "no-cache")
                            return false;

                        break;
                    case "expires":
                        DateTime dExpire;
                        if (DateTime.TryParse(value, out dExpire))
                        {
                            if (!expires.HasValue || expires.Value < dExpire)
                                expires = dExpire;
                        }
                        break;
                }
            }
            return true;
        }

        public static void CacheMaintenance()
        {
            try
            {
                while (true)
                {
                    Thread.Sleep(30000);
                    List<CacheKey> keysToRemove = new List<CacheKey>();
                    foreach (CacheKey key in _cache.Keys)
                    {
                        CacheEntry entry = (CacheEntry)(_cache[key] ?? new());
                        if (entry.FlagRemove || entry.Expires < DateTime.Now)
                            keysToRemove.Add(key);
                    }

                    foreach (CacheKey key in keysToRemove)
                        _cache.Remove(key);

                    Console.WriteLine(String.Format("Cache maintenance complete.  Number of items stored={0} Number of cache hits={1} ", _cache.Count, _hits));
                }
            }
            catch (ThreadAbortException) { }
        }

    }
}
