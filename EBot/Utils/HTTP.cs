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
        private static string UserAgent = "EBot Discord(Earu's Bot)";

        public static async Task<string> Fetch(string url,BotLog log,string useragent=null,Action<HttpWebRequest> callback=null)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 1000 * 60;
                request.Headers[HttpRequestHeader.UserAgent] = useragent != null ? useragent : UserAgent;
                callback?.Invoke(request);

                using (WebResponse answer = await request.GetResponseAsync())
                using (StreamReader reader = new StreamReader(answer.GetResponseStream(), Encoding.UTF8))
                {
                    string result = reader.ReadToEnd();
                    return result;
                }
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
