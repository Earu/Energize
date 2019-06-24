using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using System.Text;
using Victoria.Entities.Payloads;
using Newtonsoft.Json;
using Discord;

namespace Victoria.Helpers
{
    internal sealed class SocketHelper
    {
        private bool _isUseable;
        private TimeSpan _interval;
        private int _reconnectAttempts;
        private ClientWebSocket _clientWebSocket;
        private readonly Encoding _encoding;
        private readonly Configuration _config;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Func<LogMessage, Task> ShadowLog;

        public event Func<Task> OnClosed;
        public event Func<string, bool> OnMessage;

        public SocketHelper(Configuration configuration, Func<LogMessage, Task> log)
        {
            this.ShadowLog = log;
            this._config = configuration;
            this._encoding = new UTF8Encoding(false);
            ServicePointManager.ServerCertificateValidationCallback += (_, __, ___, ____) => true;
        }

        public async Task ConnectAsync()
        {
            this._cancellationTokenSource = new CancellationTokenSource();

            this._clientWebSocket = new ClientWebSocket();
            this._clientWebSocket.Options.SetRequestHeader("User-Id", $"{this._config.UserId}");
            this._clientWebSocket.Options.SetRequestHeader("Num-Shards", $"{this._config.Shards}");
            this._clientWebSocket.Options.SetRequestHeader("Authorization", this._config.Password);
            var url = new Uri($"ws://{this._config.Host}:{this._config.Port}");

            if (this._reconnectAttempts == this._config.ReconnectAttempts)
                return;

            try
            {
                this.ShadowLog?.WriteLog(LogSeverity.Info, $"Connecting to {url}.");
                await this._clientWebSocket.ConnectAsync(url, CancellationToken.None).ContinueWith(this.VerifyConnectionAsync);
            }
            catch { }
        }

        public Task SendPayloadAsync(BasePayload payload)
        {
            if (!this._isUseable)
                return Task.CompletedTask;

            var serialize = JsonConvert.SerializeObject(payload);
            this.ShadowLog?.WriteLog(LogSeverity.Debug, serialize);
            var seg = new ArraySegment<byte>(this._encoding.GetBytes(serialize));
            return this._clientWebSocket.SendAsync(seg, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            this._isUseable = false;

            await this._clientWebSocket
                .CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed called.", CancellationToken.None)
                .ConfigureAwait(false);

            this._cancellationTokenSource.Cancel(false);
            this._clientWebSocket.Dispose();
        }

        private async Task VerifyConnectionAsync(Task task)
        {
            if (task.IsCanceled || task.IsFaulted || task.Exception != null)
            {
                this._isUseable = false;
                await this.RetryConnectionAsync().ConfigureAwait(false);
            }
            else
            {
                this.ShadowLog?.WriteLog(LogSeverity.Info, "WebSocket connection established!");
                this._isUseable = true;
                this._reconnectAttempts = 0;
                await this.ReceiveAsync(this._cancellationTokenSource.Token).ConfigureAwait(false);
            }
        }

        private async Task RetryConnectionAsync()
        {
            this._cancellationTokenSource.Cancel(false);

            if (this._reconnectAttempts > this._config.ReconnectAttempts && this._config.ReconnectAttempts != -1)
                return;

            if (this._isUseable)
                return;

            this._reconnectAttempts++;
            this._interval += this._config.ReconnectInterval;
            this.ShadowLog?.WriteLog(LogSeverity.Warning,
                                     this._reconnectAttempts == this._config.ReconnectAttempts ?
                $"This was the last attempt at re-establishing websocket connection." :
                $"Attempt #{this._reconnectAttempts}. Next retry in {this._interval.TotalSeconds} seconds.");

            await Task.Delay(this._interval).ContinueWith(_ => this.ConnectAsync()).ConfigureAwait(false);
        }

        private async Task ReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    byte[] bytes;
                    using (var stream = new MemoryStream())
                    {
                        var buffer = new byte[this._config.BufferSize.Value];
                        var segment = new ArraySegment<byte>(buffer);
                        while (this._clientWebSocket.State == WebSocketState.Open)
                        {
                            var result = await this._clientWebSocket.ReceiveAsync(segment, cancellationToken)
                                .ConfigureAwait(false);
                            if (result.MessageType == WebSocketMessageType.Close)
                                if (result.CloseStatus == WebSocketCloseStatus.EndpointUnavailable)
                                {
                                    this._isUseable = false;
                                    await this.RetryConnectionAsync().ConfigureAwait(false);
                                    break;
                                }

                            stream.Write(buffer, 0, result.Count);
                            if (result.EndOfMessage)
                                break;
                        }

                        bytes = stream.ToArray();
                    }

                    if (bytes.Length <= 0)
                        continue;

                    var parse = this._encoding.GetString(bytes).Trim('\0');
                    this.OnMessage(parse);
                }
            }
            catch (Exception ex) when (ex.HResult == -2147467259)
            {
                this._isUseable = false;
                await this.OnClosed.Invoke();
                await this.RetryConnectionAsync().ConfigureAwait(false);
            }
        }
    }
}
