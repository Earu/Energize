using Discord;
using Discord.WebSocket;
using Energize.Interfaces.Services.Listeners;
using Energize.Toolkit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;
using Victoria.Queue;

namespace Energize.Services.Listeners
{
    [Service("Music")]
    public class MusicPlayer : IMusicPlayerService
    {
        private readonly DiscordShardedClient _Client;
        private readonly LavaShardClient _LavaClient;
        private readonly Logger _Logger;
        private readonly MessageSender _MessageSender;

        private bool _Initialized;
        private bool _LoopMode;

        public MusicPlayer(EnergizeClient client)
        {
            this._Initialized = false;
            this._LoopMode = false;

            this._Client = client.DiscordClient;
            this._Logger = client.Logger;
            this._MessageSender = client.MessageSender;
            this._LavaClient = new LavaShardClient();

            this._LavaClient.OnTrackException += async (ply, track, _)
                => await this.OnTrackIssue(ply, track);
            this._LavaClient.OnTrackStuck += async (ply, track, _)
                => await this.OnTrackIssue(ply, track);
            this._LavaClient.OnTrackFinished += this.OnTrackFinished;
            this._LavaClient.Log += async (logmsg) 
                => this._Logger.Nice("Lavalink", ConsoleColor.Magenta, logmsg.Message);
        }

        public LavaRestClient LavaRestClient { get; private set; }

        public async Task<LavaPlayer> ConnectAsync(IVoiceChannel vc, ITextChannel chan)
        {
            LavaPlayer ply = this._LavaClient.GetPlayer(vc.GuildId);
            if (ply == null)
                ply = await this._LavaClient.ConnectAsync(vc, chan);

            return ply;
        }

        public async Task DisconnectAsync(IVoiceChannel vc)
        {
            LavaPlayer ply = this._LavaClient.GetPlayer(vc.GuildId);
            if (ply != null)
                await this._LavaClient.DisconnectAsync(vc);
        }

        public async Task AddTrack(IVoiceChannel vc, ITextChannel chan, LavaTrack track)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, chan);
            if (ply.IsPlaying)
                ply.Queue.Enqueue(track);
            else
                await ply.PlayAsync(track, false);
        }

        public async Task ClearTracks(IVoiceChannel vc, ITextChannel chan)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, chan);
            ply.Queue.Clear();
            if (ply.IsPlaying)
                await ply.StopAsync();
        }

        public async Task PauseTrack(IVoiceChannel vc, ITextChannel chan)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, chan);
            await ply.PauseAsync();
        }

        public async Task ResumeTrack(IVoiceChannel vc, ITextChannel chan)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, chan);
            await ply.ResumeAsync();
        }

        public async Task SkipTrack(IVoiceChannel vc, ITextChannel chan)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, chan);
            if (ply.IsPlaying)
                await ply.StopAsync();
        }

        public async Task<IUserMessage> SendQueue(IVoiceChannel vc, ITextChannel chan)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, chan);
            Embed embed = this.GetQueueEmbed(ply);

            return await this._MessageSender.Send(ply.TextChannel, embed);
        }

        public async Task<IUserMessage> SendQueue(IVoiceChannel vc, IMessage msg)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, msg.Channel as ITextChannel);
            Embed embed = this.GetQueueEmbed(ply, msg);

            return await this._MessageSender.Send(ply.TextChannel, embed);
        }

        private Embed GetQueueEmbed(LavaPlayer ply, IMessage msg = null)
        {
            EmbedBuilder builder = new EmbedBuilder();
            if (msg != null)
                this._MessageSender.BuilderWithAuthor(msg, builder);
                
            builder.WithColor(this._MessageSender.ColorGood);
            builder.WithFooter("track queue");

            LavaTrack newtrack = ply.CurrentTrack;
            List<EmbedFieldBuilder> fieldbuilders = new List<EmbedFieldBuilder>();
            if (newtrack == null)
            {
                builder.WithDescription("The track queue is empty");
                return builder.Build();
            }
            else
            {
                EmbedFieldBuilder fieldbuilder = new EmbedFieldBuilder();
                fieldbuilder.WithIsInline(false);
                fieldbuilder.WithName("🎶 Currently Playing");
                fieldbuilder.WithValue($"**{newtrack.Title}** from **{newtrack.Author}** | {newtrack.Position}/{newtrack.Length}");
                fieldbuilders.Add(fieldbuilder);
            }

            if (ply.Queue.Count > 0)
            {
                EmbedFieldBuilder fieldbuilder = new EmbedFieldBuilder();
                fieldbuilder.WithIsInline(false);
                fieldbuilder.WithName("Music Queue");

                string queuedisplay = string.Empty;
                int count = 1;
                foreach (IQueueObject obj in ply.Queue.Items)
                {
                    LavaTrack tr = obj as LavaTrack;
                    queuedisplay += $"{count} - **{tr.Title}** from **{tr.Author}** | {tr.Length}\n";
                    count++;
                }
                fieldbuilder.WithValue(queuedisplay);
                fieldbuilders.Add(fieldbuilder);
            }

            builder.WithFields(fieldbuilders);
            return builder.Build();
        }

        private async Task OnTrackFinished(LavaPlayer ply, LavaTrack track, TrackEndReason reason)
        {
            if (this._LoopMode)
            {
                await ply.PlayAsync(track, false);
                return;
            }

            if (ply.Queue.Count > 0)
            {
                LavaTrack newtrack = ply.Queue.Dequeue() as LavaTrack;
                await ply.PlayAsync(newtrack);
            }

            Embed embed = this.GetQueueEmbed(ply);
            await this._MessageSender.Send(ply.TextChannel, embed);
        }

        private async Task OnTrackIssue(LavaPlayer ply, LavaTrack track)
        {
            string msg = $"There was a problem with a track, skipped \'{track.Title}\'";
            this._Logger.Nice("MusicPlayer", ConsoleColor.Red, msg);
            await this._MessageSender.Warning(ply.TextChannel, "music player", msg);
            if (ply.IsPlaying)
                await ply.StopAsync();
        }

        [Event("ShardReady")]
        public async Task OnShardReady(DiscordSocketClient clientshard)
        {
            if (this._Initialized) return;
            Configuration config = new Configuration
            {
                ReconnectInterval = TimeSpan.FromSeconds(15.0),
                ReconnectAttempts = 3,
                Host = Config.Instance.Lavalink.Host,
                Port = Config.Instance.Lavalink.Port,
                Password = Config.Instance.Lavalink.Password,
                SelfDeaf = false,
                BufferSize = 8192,
            };

            this.LavaRestClient = new LavaRestClient(config);
            await this._LavaClient.StartAsync(this._Client, config);
            this._Initialized = true;
        }

        public void Initialize() { }

        public Task InitializeAsync()
            => Task.CompletedTask;
    }
}