using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Energize.Utils
{
    public class HTTP
    {
        private static string UserAgent = "Energize Discord(Earu's Bot)";

        public static async Task<string> Fetch(string url,EnergizeLog log,string useragent=null,Action<HttpWebRequest> callback=null)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 1000 * 60;
                request.Headers[HttpRequestHeader.UserAgent] = useragent ?? UserAgent;
                callback?.Invoke(request);

                using (WebResponse answer = await request.GetResponseAsync())
                using (StreamReader reader = new StreamReader(answer.GetResponseStream(), Encoding.UTF8))
                {
                    string result = reader.ReadToEnd();
                    return result;
                }
            }
            catch(WebException e)
            {
                if(e.Status == WebExceptionStatus.Timeout)
                {
                    log.Nice("HTTP", ConsoleColor.Red, "Request timed out for [ " + url + " ]");
                }
                else if(e.Status == WebExceptionStatus.ConnectFailure)
                {
                    log.Nice("HTTP", ConsoleColor.Red, "(404) Couln't reach [ " + url + " ]");
                }
                else if(e.Status == WebExceptionStatus.ProtocolError)
                {
                    log.Nice("HTTP", ConsoleColor.Red, "Protocol error (most likely 403) [ " + url + " ]");
                    log.Danger(e.Message);
                }
                else
                {
                    log.Nice("HTTP", ConsoleColor.Red, "Unknown error [ " + url + " ]\n" + e.ToString());
                }

                return "";
            }
        }
    }
}
