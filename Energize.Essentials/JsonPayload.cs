using Newtonsoft.Json;
using System;

namespace Energize.Essentials
{
    public class JsonPayload
    {
        public static T Deserialize<T>(string json, Logger log)
        {
            try
            {
                T obj = JsonConvert.DeserializeObject<T>(json);
                return obj;
            }
            catch (Exception e)
            {
                log.Nice("JSON", ConsoleColor.Red, e.Message);
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
            catch (Exception e)
            {
                log.Nice("JSON", ConsoleColor.Red, e.Message);
                return string.Empty;
            }
        }

    }
}
