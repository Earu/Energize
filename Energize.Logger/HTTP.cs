using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Energize.Toolkit
{
    public class HttpClient
    {
        private static readonly string UserAgent = "Energize Discord(Earu's Bot)";

        public static async Task<string> Fetch(string url, Logger log, string useragent = null, Action<HttpWebRequest> callback = null)
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
                    return reader.ReadToEnd();
            }
            catch(WebException e)
            {
                switch (e.Status)
                {
                    case WebExceptionStatus.Timeout:
                        log.Nice("HTTP", ConsoleColor.Red, "Request timed out for [ " + url + " ]");
                        break;
                    case WebExceptionStatus.ConnectFailure:
                        log.Nice("HTTP", ConsoleColor.Red, "(404) Couln't reach [ " + url + " ]");
                        break;
                    case WebExceptionStatus.ProtocolError:
                        log.Nice("HTTP", ConsoleColor.Red, "Protocol error (most likely 403) [ " + url + " ]");
                        log.Danger(e.Message);
                        break;
                    default:
                        log.Nice("HTTP", ConsoleColor.Red, "Unknown error [ " + url + " ]\n" + e.ToString());
                        break;
                }

                return string.Empty;
            }
        }
    }
}
