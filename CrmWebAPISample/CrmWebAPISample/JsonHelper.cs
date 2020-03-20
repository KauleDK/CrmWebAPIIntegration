using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace CrmWebAPISample
{
    public partial class BasicEntityCollection<T>
    {
        [JsonProperty("value")]
        public List<T> Value { get; set; }
    }

    public static class JsonHelper
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        private static readonly JsonSerializerSettings JsonSettingsCollection = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            // Add all generated converters to this array
            Converters = {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            }
        };

        public static T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static List<T> FromJsonCollection<T>(string json)
        {
            return JsonConvert.DeserializeObject<BasicEntityCollection<T>>(json, JsonSettingsCollection).Value;
        }

        public static string ToJson<T>(T Object)
        {
            var json = JsonConvert.SerializeObject(Object, JsonSettings);
            return json;
        }

        public static string GetFields(Type modelType)
        {
            return string.Join(",",
                modelType.GetProperties()
                .SelectMany(p => p.GetCustomAttributes(typeof(JsonPropertyAttribute))
                .Cast<JsonPropertyAttribute>())
                .Select(jp => jp.PropertyName)
                .Where(p => p.Contains("@") == false)
                .ToArray());
        }

        public static string GetField(Type modelType, string name)
        {
            return modelType.GetProperties()
                .Where(p => p.Name == name)
                .SelectMany(p => p.GetCustomAttributes(typeof(JsonPropertyAttribute))
                .Cast<JsonPropertyAttribute>())
                .Select(p => p.PropertyName)
                .FirstOrDefault();
        }

        public static string CreateLookup(string entityName, string id)
        {
            return $"/{entityName}({id})";
        }

        public static string CreateFilter(Type modelType, string propertyName, object value)
        {
            if (value is string)
            {
                return $"{GetField(modelType, propertyName)} eq '{value}'";
            }
            else
            {
                return $"{GetField(modelType, propertyName)} eq {value}";
            }
        }

        public static string CreateFilters(Type modelType, Dictionary<string, object> propertyValuePair)
        {
            var filterList = new List<string>();
            foreach (var propertyValue in propertyValuePair)
            {
                filterList.Add(CreateFilter(modelType, propertyValue.Key, propertyValue.Value));
            }

            return String.Join(" and ", filterList);
        }
    }
}
