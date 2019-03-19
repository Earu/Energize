using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Energize.Essentials
{
    public class HttpClient
    {
        private static readonly string _Useragent = "Energize Discord(Earu's Bot)";

        public static async Task<string> GetAsync(string url, Logger logger, string useragent = null, Action<HttpWebRequest> callback = null)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.Timeout = 60000;
                request.Headers[HttpRequestHeader.UserAgent] = useragent ?? _Useragent;
                callback?.Invoke(request);

                using (WebResponse answer = await request.GetResponseAsync())
                using (StreamReader reader = new StreamReader(answer.GetResponseStream(), Encoding.UTF8))
                    return reader.ReadToEnd();
            }
            catch (WebException e)
            {
                switch (e.Status)
                {
                    case WebExceptionStatus.Timeout:
                        logger.Nice("HTTP", ConsoleColor.Red, $"Request timed out for [ {url} ]");
                        break;
                    case WebExceptionStatus.ConnectFailure:
                        logger.Nice("HTTP", ConsoleColor.Red, $"(404) Couln't reach [ {url} ]");
                        break;
                    case WebExceptionStatus.ProtocolError:
                        logger.Nice("HTTP", ConsoleColor.Red, $"Protocol error (most likely 403) [ {url} ]");
                        logger.Danger(e.Message);
                        break;
                    default:
                        logger.Nice("HTTP", ConsoleColor.Red, $"Unknown error [ {url} ]\n{e.ToString()}");
                        break;
                }
            }
            catch (Exception e)
            {
                logger.Nice("HTTP", ConsoleColor.Red, e.Message);
            }

            return string.Empty;
        }
    }
}
