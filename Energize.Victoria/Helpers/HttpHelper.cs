using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Victoria.Helpers
{
    public sealed class HttpHelper
    {
        private static readonly Lazy<HttpHelper> LazyHelper
            = new Lazy<HttpHelper>(() => new HttpHelper());

        private HttpClient _client;

        public static HttpHelper Instance
            => LazyHelper.Value;

        private void CheckClient()
        {
            if (!(this._client is null))
                return;

            this._client = new HttpClient(new HttpClientHandler
            {
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });
            this._client.DefaultRequestHeaders.Clear();
            this._client.DefaultRequestHeaders.Add("User-Agent", "Victoria");
        }

        public async Task<string> GetStringAsync(string url)
        {
            this.CheckClient();

            var get = await this._client.GetAsync(url).ConfigureAwait(false);
            if (!get.IsSuccessStatusCode)
                return string.Empty;

            using var content = get.Content;
            var read = await content.ReadAsStringAsync().ConfigureAwait(false);
            return read;
        }

        public HttpHelper WithCustomHeader(string key, string value)
        {
            this.CheckClient();

            if (this._client.DefaultRequestHeaders.Contains(key))
                return this;

            this._client.DefaultRequestHeaders.Add(key, value);
            return this;
        }
    }
}