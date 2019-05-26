using Newtonsoft.Json;
using System;

namespace Energize.Essentials
{
    public class JsonPayload
    {
        public static T Deserialize<T>(string json, Logger logger)
        {
            try
            {
                T obj = JsonConvert.DeserializeObject<T>(json);
                return obj;
            }
            catch (Exception ex)
            {
                logger.Nice("JSON", ConsoleColor.Red, ex.Message);
                return default(T);
            }
        }

        public static string Serialize(object obj, Logger logger)
        {
            try
            {
                return JsonConvert.SerializeObject(obj);
            }
            catch (Exception ex)
            {
                logger.Nice("JSON", ConsoleColor.Red, ex.Message);
                return string.Empty;
            }
        }

    }
}
