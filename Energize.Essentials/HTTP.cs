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
        private const string UserAgent = "Energize Discord(Earu's Bot)";

        private static async Task<string> InternalRequest(string method, string url, string body, Logger logger, string userAgent, Action<HttpWebRequest> callback = null)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = method;
                request.Timeout = 60000;
                request.Headers[HttpRequestHeader.UserAgent] = userAgent ?? UserAgent;
                if (!string.IsNullOrWhiteSpace(body))
                {
                    byte[] data = Encoding.ASCII.GetBytes(body);
                    using (Stream stream = request.GetRequestStream())
                        await stream.WriteAsync(data, 0, data.Length);
                }

                callback?.Invoke(request);

                using (WebResponse answer = await request.GetResponseAsync())
                using (StreamReader reader = new StreamReader(answer.GetResponseStream(), Encoding.UTF8))
                    return reader.ReadToEnd();
            }
            catch (WebException ex)
            {
                switch (ex.Status)
                {
                    case WebExceptionStatus.Timeout:
                        logger.Nice("HTTP", ConsoleColor.Red, $"Request timed out for [ {url} ]");
                        break;
                    case WebExceptionStatus.ConnectFailure:
                        logger.Nice("HTTP", ConsoleColor.Red, $"(404) Couln't reach [ {url} ]");
                        break;
                    case WebExceptionStatus.ProtocolError:
                        logger.Nice("HTTP", ConsoleColor.Red, $"Protocol error [ {url} ]");
                        logger.Danger(ex.Message);
                        break;
                    default:
                        logger.Nice("HTTP", ConsoleColor.Red, $"Unknown error [ {url} ]\n{ex}");
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.Nice("HTTP", ConsoleColor.Red, ex.Message);
            }

            return string.Empty;
        }

        public static async Task<string> PostAsync(string url, string body, Logger logger, string userAgent = null, Action<HttpWebRequest> callback = null)
            => await InternalRequest("POST", url, body, logger, userAgent, callback);

        public static async Task<string> GetAsync(string url, Logger logger, string userAgent = null, Action<HttpWebRequest> callback = null)
            => await InternalRequest("GET", url, null, logger, userAgent, callback);

        private static readonly Regex UrlRegex = new Regex(@"^(?:https?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool IsUrl(string input)
            => input.StartsWith("http") && UrlRegex.IsMatch(input);
    }
}
