using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using Azure;
using static Elastic.Clients.Elasticsearch.JoinField;
using static SB_Parser_API.MicroServices.Utils;
using AngleSharp.Common;
//using System.Configuration;
//using Microsoft.Extensions.Configuration;

namespace SB_Parser_API.Models
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class JsonPathConverter : JsonConverter
    {
        /// <inheritdoc />
        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object? existingValue,
            JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            object? targetObj = Activator.CreateInstance(objectType);

            foreach (PropertyInfo prop in objectType.GetProperties().Where(p => p.CanRead && p.CanWrite))
            {
                JsonPropertyAttribute? att = prop.GetCustomAttributes(true)
                                                .OfType<JsonPropertyAttribute>()
                                                .FirstOrDefault();

                string? jsonPath = att != null ? att.PropertyName : prop.Name;

                if (serializer.ContractResolver is DefaultContractResolver)
                {
                    var resolver = (DefaultContractResolver)serializer.ContractResolver;
                    jsonPath = resolver.GetResolvedPropertyName(jsonPath!);
                }

                if (!Regex.IsMatch(jsonPath!, @"^[a-zA-Z0-9_.-]+$"))
                {
                    throw new InvalidOperationException($"JProperties of JsonPathConverter can have only letters, numbers, underscores, hiffens and dots but name was ${jsonPath}."); // Array operations not permitted
                }

                JToken? token = jo.SelectToken(jsonPath!);
                if (token != null && token.Type != JTokenType.Null)
                {
                    object? value = token.ToObject(prop.PropertyType, serializer);
                    prop.SetValue(targetObj, value, null);
                }
            }

            return targetObj!;
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            // CanConvert is not called when [JsonConverter] attribute is used
            return objectType.GetCustomAttributes(true).OfType<JsonPathConverter>().Any();
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var properties = value?.GetType().GetRuntimeProperties().Where(p => p.CanRead && p.CanWrite);
            JObject main = new();
            foreach (PropertyInfo prop in properties!)
            {
                JsonPropertyAttribute att = prop.GetCustomAttributes(true)!
                    .OfType<JsonPropertyAttribute>()!
                    .FirstOrDefault()!;

                string jsonPath = att != null ? att.PropertyName! : prop.Name!;

                if (serializer.ContractResolver is DefaultContractResolver)
                {
                    var resolver = (DefaultContractResolver)serializer.ContractResolver;
                    jsonPath = resolver.GetResolvedPropertyName(jsonPath!);
                }

                var nesting = jsonPath!.Split('.');
                JObject lastLevel = main;

                for (int i = 0; i < nesting.Length; i++)
                {
                    if (i == nesting.Length - 1)
                    {
                        lastLevel[nesting[i]] = new JValue(prop.GetValue(value));
                    }
                    else
                    {
                        if (lastLevel[nesting[i]] == null)
                        {
                            lastLevel[nesting[i]] = new JObject();
                        }

                        lastLevel = (JObject)lastLevel[nesting[i]]!;
                    }
                }
            }

            serializer.Serialize(writer, main);
        }
    }

    //_________________________ JSON Models______________________________________//

    //_________________________PISB_DB Models______________________________________//

    // Retailer model

    [JsonConverter(typeof(JsonPathConverter))]
    public class Retailer
    {
        public int id { get; set; }
        public string? name { get; set; }
        public string? slug { get; set; }
        public bool? available { get; set; }
        public string? key { get; set; }
        public int position { get; set; }
        public DateTime? created_at { get; set; }
        public string? legal_name { get; set; }
        public string? inn { get; set; }
        public string? phone { get; set; }
        public string? legal_address { get; set; }
        public string? contract_type { get; set; }
        public int? home_page_departments_depth { get; set; }
        public string? short_name { get; set; }
        public string? description { get; set; }
        public string? secondary_color { get; set; }
        public int? retailer_category_id { get; set; }
        public string? seo_category { get; set; }

        //public Appearance appearance { get; set; }
        //public MasterRetailer master_retailer { get; set; }

        [JsonProperty("master_retailer.id")]
        public int? master_retailer_id { get; set; }

        [JsonProperty("master_retailer.name")]
        public string? master_retailer_name { get; set; }

        [JsonProperty("appearance.logo_image")]
        public string? logo_image { get; set; }

        [JsonProperty("appearance.mini_logo_image")]
        public string? mini_logo_image { get; set; }

        [JsonProperty("appearance.side_image")]
        public string? side_image { get; set; }

        [JsonProperty("key_account_manager.id")]
        public int? key_account_manager_id { get; set; }

        [JsonProperty("key_account_manager.fullname")]
        public string? key_account_manager_fullname { get; set; }
    }

    // Store model

    [JsonConverter(typeof(JsonPathConverter))]
    public class Store
    {
        [NotMapped]
        public bool point0 { get; set; } = false;

        public int id { get; set; }
        public string? uuid { get; set; }
        public string? name { get; set; }
        //public int city_id { get; set; }
        public string? time_zone { get; set; }
        public DateTime available_on { get; set; }
        public bool active { get; set; }
        public string? retailer_store_id { get; set; }

        //public string? pharmacy_legal_info { get; set; }
        [JsonProperty("pharmacy_legal_info.license")]
        public string? pharmacy_license { get; set; }
        public string? phone { get; set; }
        public string? orders_api_integration_type { get; set; }
        public string? opening_time { get; set; }
        public string? closing_time { get; set; }

        [JsonProperty("retailer.is_alcohol")]
        public bool is_alcohol { get; set; }

        [JsonProperty("retailer.id")]
        public int retailer_id { get; set; }
        
        [JsonProperty("location.id")]
        public int location_id { get; set; }

        [JsonProperty("location.full_address")]
        public string? full_address { get; set; }

        [JsonProperty("location.city")]
        public string? city { get; set; }

        [JsonProperty("location.street")]
        public string? street { get; set; }

        [JsonProperty("location.building")]
        public string? building { get; set; }

        [JsonProperty("location.phone")]
        public string? location_phone { get; set; }

        [JsonProperty("location.lat")]
        public double lat { get; set; }

        [JsonProperty("location.lon")]
        public double lon { get; set; }

        [NotMapped]
        public double? distanceToMyPoint { get; set; }

        [JsonProperty("operational_zone.id")]
        public int operational_zone_id { get; set; }

        [JsonProperty("operational_zone.name")]
        public string? operational_zone_name { get; set; }

        [JsonProperty("city.id")]
        public int city_id { get; set; }

        [NotMapped]
        [JsonProperty("city_id")]
        public int _city_id { get; set; }

        [JsonProperty("city.name")]
        public string? city_name { get; set; }

        [JsonProperty("city.slug")]
        public string? city_slug { get; set; }

        public int? last_product_count { get; set; }
        public DateTime? lpc_dt { get; set; }
    }

    [JsonConverter(typeof(JsonPathConverter))]
    public class StoreSet_SB
    {
        public List<Store>? stores { get; set; }
        [JsonProperty("meta.total_count")]
        public int total_count { get; set; }
    }

        //[JsonConverter(typeof(JsonPathConverter))]
        public class Category_SB_V2
    {
        public int id { get; set; }
        public string? type { get; set; }
        public string? name { get; set; }
        public int products_count { get; set; }
        //public List<object>? promo_services { get; set; }
        public int position { get; set; }
        public int depth { get; set; }
        public string? description { get; set; }

        //[JsonProperty("icon.mini_url")]
        //public string? icon_mini { get; set; }

        //[JsonProperty("icon.normal_url")]
        //public string? icon_normal { get; set; }
        //public object alt_icon { get; set; }
        public record iconR(string? mini_url, string? normal_url);
        public iconR? icon { get; set; }
        public List<Category_SB_V2>? children { get; set; }
        //public List<Requirement> requirements { get; set; }
    }
    
    
    [JsonConverter(typeof(JsonPathConverter))]
    public class Product_SB_V2
    {
        public long id { get; set; }
        public long? sku { get; set; }
        public bool? active { get; set; }
        public long? retailer_sku { get; set; }
        public string? name { get; set; }

        [NotMapped]
        public double? price { get; set; }
        [NotMapped]
        public double? original_price { get; set; }
        [NotMapped]
        public double? discount { get; set; }
        public string? human_volume { get; set; }
        public double? volume { get; set; }
        public string? volume_type { get; set; }
        public int? items_per_pack { get; set; }
        [NotMapped]
        public DateTime? discount_ends_at { get; set; }
        public string? price_type { get; set; }
        public int? grams_per_unit { get; set; }
        [NotMapped]
        public double? unit_price { get; set; }
        [NotMapped]
        public double? original_unit_price { get; set; }
        //public List<object> promo_badge_ids { get; set; }
        public double? score { get; set; }
        //public ScoreDetails score_details { get; set; }
        //public List<object> labels { get; set; }

        [NotMapped]
        public List<SB_Image>? images { get; set; }
        //public List<object> requirements { get; set; }
        //public bool with_options { get; set; }
        //public int? max_per_order { get; set; }
        [NotMapped]
        public double stock { get; set; }
        //public List<string> marking_systems { get; set; }
        //public List<object>? retailer_price { get; set; }
        [NotMapped]
        public List<string>? eans { get; set; }
        //public string shipping_category_slug { get; set; }

        public int? retailer { get; set; }
        public int? store { get; set; }
        public DateTime? dt_created { get; set; } = DateTime.Now;
        public DateTime? dt_updated { get; set; } = DateTime.Now;
        public DateTime? dt_property_updated { get; set; } = DateTime.Now;
    }
    class Product_SB_V2_IdComparer : IEqualityComparer<Product_SB_V2>
    {
        // Products are equal if their id are equal.
        public bool Equals(Product_SB_V2? x, Product_SB_V2? y)
        {
            //Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(x, y)) return true;
            //Check whether any of the compared objects is null.
            if (x is null || y is null) return false;
            //Check whether the products'id are equal.
            return x.id == y.id;
        }

        // If Equals() returns true for a pair of objects
        // then GetHashCode() must return the same value for these objects.

        public int GetHashCode(Product_SB_V2 x)
        {
            //Check whether the object is null
            if (Object.ReferenceEquals(x, null)) return 0;

            //Get hash code for the id field if it is not null.
            int hashId = x.id.GetHashCode();

            //Calculate the hash code.
            return hashId;
        }
    }

    public record Price_SB_V2_Base
    {
        //[JsonProperty("notMapped")]
        //public int id { get; set; }
        public DateTime? dt { get; set; }
        public DateTime? dt_created { get; set; }
        public int? retailer { get; set; }
        public int? store { get; set; }
        //[Column("product_id")]
        [JsonProperty("id")]
        public long? product_id { get; set; }
        public long? product_sku { get; set; }
        public double? price { get; set; }
        public double? original_price { get; set; }
        public double? discount { get; set; }
        public DateTime? discount_ends_at { get; set; }
        public double? unit_price { get; set; }
        public double? original_unit_price { get; set; }
        public double stock { get; set; }
        public int update_counter { get; set; }
    }
    public record Price_SB_V2 : Price_SB_V2_Base
    {
        [JsonProperty("notMapped")]
        public int id { get; set; }
    }

    public record Price_SB_V2_Archive : Price_SB_V2_Base
    {
        [JsonProperty("notMapped")]
        public int id { get; set; }
        public Price_SB_V2_Archive() { }
        public Price_SB_V2_Archive(Price_SB_V2 parent)
        {
            foreach (PropertyInfo prop in parent.GetType().GetProperties())
                GetType()?.GetProperty(prop.Name)?.SetValue(this, prop.GetValue(parent, null), null);
        }
    }
    //public record Price_SB_V2_Archive : Price_SB_V2 
    //{
    //    public Price_SB_V2_Archive() { }
    //    public Price_SB_V2_Archive(Price_SB_V2 parent)
    //    {
    //        foreach (PropertyInfo prop in parent.GetType().GetProperties())
    //            GetType()?.GetProperty(prop.Name)?.SetValue(this, prop.GetValue(parent, null), null);
    //    }
    //    //public new int id { get; set; }
    //}

    public class Meta_Products_SB_V2
    {
        public int current_page { get; set; }
        public int? next_page { get; set; }
        public int? previous_page { get; set; }
        public int total_pages { get; set; }
        public int per_page { get; set; }
        public int total_count { get; set; }
    }

    [JsonConverter(typeof(JsonPathConverter))]
    public class SB_Image
    {
        public int id { get; set; }

        [NotMapped]
        public long product_id { get; set; } = 0;

        //public string? mini_url { get; set; }
        //public string? small_url { get; set; }
        //public string? product_url { get; set; }
        //public string? preview_url { get; set; }

        [JsonProperty("original_url")]
        public string url { get; set; } = "";
    }
    public class SB_Barcode
    {
        public int id { get; set; }
        [NotMapped]
        public long product_id { get; set; } = 0;
        public string barcode { get; set; } = "";
    }
    public class SB_barcode_product
    {
        public int id { get; set; }
        public long product_id { get; set; } = 0;
        public long product_sku { get; set; } = 0;
        public int barcode_id { get; set; } = 0;
    }
    public class SB_image_product
    {
        public int id { get; set; }
        public long product_id { get; set; } = 0;
        public long product_sku { get; set; } = 0;
        public int image_id { get; set; } = 0;
    }

    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);

    [JsonConverter(typeof(JsonPathConverter))]
    public class Product_Details_SB_V2
    {
        public long id { get; set; }
        public long? sku { get; set; }
        public bool active { get; set; }
        public long? retailer_sku { get; set; }
        public string? name { get; set; }
        public double? price { get; set; }
        public double? original_price { get; set; }
        public double? discount { get; set; }
        public string? human_volume { get; set; }
        public double? volume { get; set; }
        public string? volume_type { get; set; }
        public int items_per_pack { get; set; }
        public DateTime? discount_ends_at { get; set; }
        public string? price_type { get; set; }
        public int grams_per_unit { get; set; }
        public double? unit_price { get; set; }
        public double? original_unit_price { get; set; }
        public double? score { get; set; }
        public List<SB_Image>? images { get; set; }
        public List<Property_Product_SB_V2>? properties { get; set; }
        public string? description { get; set; }
        public List<Product_SB_V2>? related_products { get; set; }
        public Main_Taxon_SB_V2? main_taxon { get; set; }
        [JsonProperty("brand.id")]
        public int brand_id { get; set; }
        [JsonProperty("brand.name")]
        public string? brand_name { get; set; }
        [JsonProperty("store_id")]
        public int? store { get; set; }
        public int? retailer { get; set; }
        public int max_per_order { get; set; }
        public int stock { get; set; }
        public string? vertical { get; set; }
        //public ScoreDetails score_details { get; set; }
        //public List<object> labels { get; set; }
    }
    public class Property_Product_SB_V2
    {
        public string? name { get; set; }
        public string? presentation { get; set; }
        public string? value { get; set; }
    }
    public class Property_Product_DB : Property_Product_SB_V2
    {
        public int id { get; set; }
        public long product_id { get; set; }
        public long product_sku { get; set; }
    }

    [JsonConverter(typeof(JsonPathConverter))]
    public class Main_Taxon_SB_V2
    {
        public int id { get; set; }
        public string type { get; set; } = "";
        public string name { get; set; } = "";
        public int products_count { get; set; }
        public string? description { get; set; }
        [JsonProperty("icon.normal_url")]
        public string? icon_url { get; set; }
        public string? alt_icon { get; set; }
    }

    // JSON Models for ZC_API

    public class Product_From_Barcode_ZC
    {
        public long id { get; set; }
        public long? sku { get; set; }
        public bool? active { get; set; }
        public string? name { get; set; }
        public string? human_volume { get; set; }
        public double? volume { get; set; }
        public string? volume_type { get; set; }
        public int? items_per_pack { get; set; }
        public string? price_type { get; set; }
        public int? grams_per_unit { get; set; }
        public double? score { get; set; }
        public List<string>? image_urls { get; set; }
        public double stock { get; set; }
        public List<string>? barcodes { get; set; } = new();
        public List<Property_Product_SB_V2>? properties { get; set; }
        public List<Price_ZC>? prices { get; set; }
        public double sort_rate { get; set; }
        public double match_rate { get; set; }
        public double price_rate { get; set; }

        //public int user_id { get; set; } = 0;
        //public long query_id { get; set; } = 0;
    }
    class Product_From_Barcode_ZC_Comparer : IEqualityComparer<Product_From_Barcode_ZC>
    {
        // Products are equal if their names and product numbers are equal.
        public bool Equals(Product_From_Barcode_ZC? x, Product_From_Barcode_ZC? y)
        {
            //Check whether the compared objects reference the same data.
            if (Object.ReferenceEquals(x, y)) return true;
            //Check whether any of the compared objects is null.
            if (x is null || y is null) 
                return false;
            //Check whether the products are equal.
            if(!(x.barcodes ?? new()).Intersect(y.barcodes ?? new()).Any())
                return false;
            if(x.grams_per_unit !=y.grams_per_unit)
                return false;
            if (CompResultFast(x.name ?? "xxxx", y.name ?? "yyyy") < 8000)
                return false;
            /*
            JsonConvert.DeserializeObject(x.properties?.FirstOrDefault(x => x.name == "main_taxon")?.value ?? "{'main_taxon':'none'}").ToDictionary().TryGetValue("name",out var xCat);
            JsonConvert.DeserializeObject(y.properties?.FirstOrDefault(x => x.name == "main_taxon")?.value ?? "{'main_taxon':'none'}").ToDictionary().TryGetValue("name", out var yCat);
            if ((xCat ?? "null") != (yCat ?? "null"))
                return false;
            */

            return true;   //x.protocol == y.protocol && x.ip == y.ip && x.port == y.port;
        }

        // If Equals() returns true for a pair of objects
        // then GetHashCode() must return the same value for these objects.

        public int GetHashCode(Product_From_Barcode_ZC p)
        {
            //Check whether the object is null
            if (Object.ReferenceEquals(p, null)) return 0;

            //Get hash code for the protocol field if it is not null.
            //int hashProductProtocol = ptd.protocol == null ? 0 : ptd.protocol.GetHashCode();

            //Get hash code for the ip field.
            //int hashProductIp = ptd.ip == null ? 0 : ptd.ip.GetHashCode();

            //Get hash code for the port field.
            //int hashProductPort = ptd.port == null ? 0 : ptd.port.GetHashCode();

            //Calculate the hash code for the product.
            return p.grams_per_unit == null ? 0 : p.grams_per_unit.GetHashCode(); //hashProductProtocol ^ hashProductIp ^ hashProductPort;
        }
    }


    public record class Products_List_From_Barcode_ZC(long query_id, int user_id, bool isRequestCompleted, List<Product_From_Barcode_ZC> products, string error="");
    public record class Products_List_From_Barcode_Table_ZC(long query_id, int user_id, bool isRequestCompleted, List<string> Columns, List<List<object>> Rows, string error = "");

    public record class Price_ZC
    {
        public DateTime? dt { get; set; }
        public int? retailer { get; set; }
        public string? retailer_name { get; set; }
        public string? retailer_logo_url { get; set; }
        public string? retailer_mini_logo_url { get; set; }
        public int? store { get; set; }
        public double? lat { get; set; }
        public double? lon { get; set; }
        public string? address { get; set; }
        public long? product_id { get; set; }
        public long? product_sku { get; set; }
        public double? price { get; set; }
        public double? original_price { get; set; }
        public double? discount { get; set; }
        public DateTime? discount_ends_at { get; set; }
        public double? unit_price { get; set; }
        public double? original_unit_price { get; set; }
        public double stock { get; set; }
        public double? distanceToMyPoint { get; set; }
        public bool is_flagman { get; set; } = false;
        public bool is_local { get; set; } = false;
        public bool is_just_updated { get; set; } = false;

    }
    public class Query_ZC
    {
        public long id { get; set; }
        public int user_id { get; set; }
        public QueryType query_type { get; set; }
        public string query { get; set; } = "";
        public DateTime dt { get; set; } = DateTime.Now;
    }
    public enum QueryType : int
    {
        BarcodeSearch = 0, TextSearch = 1
    }
    public class Query_Type_ZC
    {
        public QueryType id { get; set; }
        public string? type { get; set; }
    }

    [JsonConverter(typeof(JsonPathConverter))]
    public class Multi_Search_Result
    {
        public string id { get; set; } = "";
        public int store_id { get; set; }
        public string name { get; set; } = "";
        public double min_order_amount { get; set; }
        public double min_order_amount_pickup { get; set; }
        public double min_first_order_amount { get; set; }
        public double min_first_order_amount_pickup { get; set; }
        public string delivery_forecast_text { get; set; } = "";
        public bool on_demand { get; set; }
        public bool express_delivery { get; set; }
        public double minimum_order_amount { get; set; }
        public double minimum_order_amount_pickup { get; set; }
        public bool is_planned_delivery_available { get; set; }
        //public List<ClosestShippingOption> closest_shipping_options { get; set; }
        //public List<ShippingMethod> shipping_methods { get; set; }
        [JsonProperty("retailer.id")]
        public int retailer_id { get; set; }
    }
    public class ActiveRequest
    {
        public long? query_id { get; set; }
        public int? user_id { get; set; }
        public string? user_name { get; set; }
        public string? user_phone { get; set; }
        public string? user_email { get; set; }
        public string? query { get; set; }
        public string? barcode { get; set; }
        public double? lat { get; set; }
        public double? lon { get; set; }
        public double? radius { get; set; } // km
        public int? take { get; set; }
        public bool? properties { get; set; }


        //__ For internal use
        public ZC_User? user { get; set; }
        public string? action { get; set; }
        public string? controller { get; set; }
    }
    public record ProductName
    {
        public long Id { get; set; }
        public string name { get; set; } = "";
        public string namepp { get; set; } = "";
        public int rate { get; set; }
    }
}
