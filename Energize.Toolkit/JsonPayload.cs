using Newtonsoft.Json;
using System;

namespace Energize.Toolkit
{
    public class JsonPayload
    {
        public static T Deserialize<T>(string json, Logger log)
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

        public static string Serialize(object obj, Logger log)
        {
            try
            {
                string json = JsonConvert.SerializeObject(obj);
                return json;
            }
            catch
            {
                log.Nice("JSON", ConsoleColor.Red, "Couldn't serialize a string!");

                return string.Empty;
            }
        }

    }
}
