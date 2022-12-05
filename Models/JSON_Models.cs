using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
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

        [JsonProperty("city.name")]
        public string? city_name { get; set; }

        [JsonProperty("city.slug")]
        public string? city_slug { get; set; }
    }
}
