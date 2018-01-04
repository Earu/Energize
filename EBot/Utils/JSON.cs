using EBot.Logs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EBot.Utils
{
    public class JSON
    {
        public static T Deserialize<T>(string json,BotLog log)
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

        public static string Serialize(Object obj,BotLog log)
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
