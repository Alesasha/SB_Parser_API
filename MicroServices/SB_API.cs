using System.Text.RegularExpressions;

namespace SB_Parser_API.MicroServices
{
    public class SB_API
    {
        static readonly int errorAttempts = 5;
        static DateTime lastTokenInit = DateTime.MinValue;
        static readonly string get_API_Key_Url= "https://sbermarket.ru/"; // STOREFRONT_API_V3_CLIENT_TOKEN
        static readonly string get_retailers = "https://sbermarket.ru/api/retailers";
        static readonly string api_version = "3.0";
        static string client_token = "7ba97b6f4049436dab90c789f946ee2f";
        static Object client_token_Lock = new Object();

        static readonly Regex get_client_token = new(@"^.+STOREFRONT_API_V3_CLIENT_TOKEN[^""]+""([^""]+).+");
        public static string GeoQueryURL(double lat, double lon) => $"https://sbermarket.ru/api/stores?lat={lat.ToString().Replace(",", ".")}&lon={lon.ToString().Replace(",", ".")}&shipping_method=delivery";
        //&include=closest_shipping_options%2Clabels%2Cretailer%2Clabel_store_ids&shipping_method=delivery" &shipping_method=pickup_from_store
        public static string SearchQueryURL(string query, int store) => $"https://sbermarket.ru/api/stores/19961/products?q=4607161622308&page=1&per_page=15";
        //https://sbermarket.ru/api/stores/19961/products?q=4607161622308&page=1&per_page=15
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
                    client_token = get_client_token.Replace(result.Replace("\n", ""), "$1");
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
        public static string? Retailers() => GetStringFromUrl(get_retailers, nameof(Retailers));
        public static string? NearbyStores(double lat, double lon) => GetStringFromRequest(CreateRequestAPI_3_0(GeoQueryURL(lat, lon)),nameof(NearbyStores));
    }
}
