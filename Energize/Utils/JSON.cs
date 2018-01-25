using Newtonsoft.Json;
using System;

namespace Energize.Utils
{
    public class JSON
    {
        public static T Deserialize<T>(string json,EnergizeLog log)
        {
            try
            {
                T deserialized = JsonConvert.DeserializeObject<T>(json);
                return deserialized;
            }
            catch
            {
                log.Nice("JSON", ConsoleColor.Red, "Couldn't deserialize a string!");

                return default(T);
            }
        }

        public static string Serialize(Object obj,EnergizeLog log)
        {
            try
            {
                string json = JsonConvert.SerializeObject(obj);
                return json;
            }
            catch
            {
                log.Nice("JSON", ConsoleColor.Red, "Couldn't serialize a string!");

                return "";
            }
        }

    }
}
