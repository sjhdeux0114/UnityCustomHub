using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace UnityHubCustom.Helpers
{
    public static class JsonHelper
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer
        {
            MaxJsonLength = int.MaxValue
        };

        public static string Serialize(object obj)
        {
            try
            {
                return Serializer.Serialize(obj);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Serialize error: " + ex.Message);
                return "{}";
            }
        }

        public static T Deserialize<T>(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return default(T);
                return Serializer.Deserialize<T>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Deserialize error: " + ex.Message);
                return default(T);
            }
        }

        public static Dictionary<string, object> ParseDictionary(string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, object>();
                return Serializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("ParseDictionary error: " + ex.Message);
                return new Dictionary<string, object>();
            }
        }
    }
}
