using EBot.Logs;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace EBot.Utils
{
    class HTTP
    {
        public static async Task<string> Fetch(string Url,BotLog log)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                request.Method = "GET";
                request.Headers[HttpRequestHeader.UserAgent] = "EBot Discord(Earu's Bot)";
                WebResponse answer = await request.GetResponseAsync();
                StreamReader reader = new StreamReader(answer.GetResponseStream(), Encoding.UTF8);
                string result = reader.ReadToEnd();
                reader.Dispose();
                answer.Dispose();

                return result;
            }
            catch(Exception e)
            {
                log.Nice("HTTP", ConsoleColor.Red, "Couldn't fetch URL body");
                log.Danger(e.ToString());

                return "";
            }
        }
    }
}
