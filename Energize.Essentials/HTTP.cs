using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Energize.Essentials
{
    public class HttpClient
    {
        private static readonly string _Useragent = "Energize Discord(Earu's Bot)";

        private static async Task<string> InternalRequest(string method, string url, string body, Logger logger, string useragent, Action<HttpWebRequest> callback = null)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = method;
                request.Timeout = 60000;
                request.Headers[HttpRequestHeader.UserAgent] = useragent ?? _Useragent;
                if (!string.IsNullOrWhiteSpace(body))
                {
                    byte[] data = Encoding.ASCII.GetBytes(body);
                    using (var stream = request.GetRequestStream())
                        await stream.WriteAsync(data, 0, data.Length);
                }

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
                        logger.Nice("HTTP", ConsoleColor.Red, $"Protocol error [ {url} ]");
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

        public static async Task<string> PostAsync(string url, string body, Logger logger, string useragent = null, Action<HttpWebRequest> callback = null)
            => await InternalRequest("POST", url, body, logger, useragent, callback);

        public static async Task<string> GetAsync(string url, Logger logger, string useragent = null, Action<HttpWebRequest> callback = null)
            => await InternalRequest("GET", url, null, logger, useragent, callback);

        public static bool IsURL(string input)
            => Regex.IsMatch(input, @"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$");
    }
}
