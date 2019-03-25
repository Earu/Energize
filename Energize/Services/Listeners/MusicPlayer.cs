using Discord;
using Discord.WebSocket;
using Energize.Interfaces.Services.Listeners;
using Energize.Essentials;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;
using Victoria.Queue;
using System.Linq;

namespace Energize.Services.Listeners
{
    [Service("Music")]
    public class MusicPlayer : IMusicPlayerService
    {
        private readonly DiscordShardedClient _Client;
        private readonly LavaShardClient _LavaClient;
        private readonly Logger _Logger;
        private readonly MessageSender _MessageSender;
        private readonly List<ulong> _PlayersLooping;

        private bool _Initialized;

        public MusicPlayer(EnergizeClient client)
        {
            this._Initialized = false;
            this._PlayersLooping = new List<ulong>();

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
            this._LavaClient.OnPlayerUpdated += this.OnPlayerUpdated;
        }

        private Task OnPlayerUpdated(LavaPlayer ply, LavaTrack track, TimeSpan position)
        {
            IGuild guild = ply.VoiceChannel.Guild;
            string msg = $"{DateTime.Now} - Updated track <{track.Title}> ({position}) for player in guild <{guild.Name}>";
            this._Logger.LogTo("victoria.log", msg);

            return Task.CompletedTask;
        }

        public LavaRestClient LavaRestClient { get; private set; }

        public async Task<LavaPlayer> ConnectAsync(IVoiceChannel vc, ITextChannel chan)
        {
            LavaPlayer ply = await this._LavaClient.ConnectAsync(vc, chan);
            if (vc.Id != ply.VoiceChannel.Id)
                await this._LavaClient.MoveChannelsAsync(vc);

            return ply;
        }

        public async Task DisconnectAsync(IVoiceChannel vc)
        {
            await this._LavaClient.DisconnectAsync(vc);
            if (this._PlayersLooping.Contains(vc.GuildId))
                this._PlayersLooping.Remove(vc.GuildId);
        }

        public async Task AddTrack(IVoiceChannel vc, ITextChannel chan, LavaTrack track)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, chan);
            if (ply.IsPlaying)
                ply.Queue.Enqueue(track);
            else
                await ply.PlayAsync(track, false);
        }

        public async Task<bool> LoopTrack(IVoiceChannel vc, ITextChannel chan)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, chan);
            bool islooping = this._PlayersLooping.Contains(vc.GuildId);
            if (islooping) 
                this._PlayersLooping.Remove(vc.GuildId);
            else
                this._PlayersLooping.Add(vc.GuildId);
            return !islooping;
        }

        private bool ShouldLoop(LavaPlayer ply)
        {
            ulong guildid = ply.VoiceChannel.GuildId;
            return this._PlayersLooping.Contains(guildid);
        }

        public async Task ShuffleTracks(IVoiceChannel vc, ITextChannel chan)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, chan);
            ply.Queue.Shuffle();
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
            if (ply.CurrentTrack != null)
                await ply.PauseAsync();
        }

        public async Task ResumeTrack(IVoiceChannel vc, ITextChannel chan)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, chan);
            if (ply.CurrentTrack != null)
                await ply.ResumeAsync();
        }

        public async Task SkipTrack(IVoiceChannel vc, ITextChannel chan)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, chan);
            if (ply.IsPlaying)
                await ply.StopAsync();
        }

        public async Task SetTrackVolume(IVoiceChannel vc, ITextChannel chan, int vol)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, chan);
            await ply.SetVolumeAsync(vol);
        }

        public async Task<string> GetTrackLyrics(IVoiceChannel vc, ITextChannel chan)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, chan);
            if (ply.IsPlaying)
            {
                LavaTrack track = ply.CurrentTrack;
                return await track.FetchLyricsAsync();
            }

            return "Nothing is playing";
        }

        public ServerStats GetLavalinkStats()
            => this._LavaClient.ServerStats;

        public async Task<IUserMessage> SendQueue(IVoiceChannel vc, IMessage msg)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, msg.Channel as ITextChannel);
            Embed embed = this.GetQueueEmbed(ply, msg);

            return await this._MessageSender.Send(ply.TextChannel, embed);
        }

        public async Task<IUserMessage> SendNewTack(IVoiceChannel vc, IMessage msg, LavaTrack track)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, msg.Channel as ITextChannel);
            Embed embed = await this.GetNewTrackEmbed(track, false, msg);

            return await this._MessageSender.Send(ply.TextChannel, embed);
        }

        public async Task<IUserMessage> SendNewTack(IVoiceChannel vc, ITextChannel chan, LavaTrack track)
        {
            LavaPlayer ply = await this.ConnectAsync(vc, chan);
            Embed embed = await this.GetNewTrackEmbed(track, false);

            return await this._MessageSender.Send(ply.TextChannel, embed);
        }

        private EmbedFieldBuilder Field(string title, object value)
        {
            EmbedFieldBuilder fieldbuilder = new EmbedFieldBuilder();
            fieldbuilder
                .WithIsInline(true)
                .WithName(title)
                .WithValue(value);
            return fieldbuilder;
        }

        private async Task<Embed> GetNewTrackEmbed(LavaTrack track, bool playing, IMessage msg = null)
        {
            string url = await track.FetchThumbnailAsync();
            EmbedBuilder builder = new EmbedBuilder();
            if (msg != null)
                this._MessageSender.BuilderWithAuthor(msg, builder);
            string desc = "🎶 Added the following track to the queue:";
            if (playing)
                desc = "🎶 Now playing the following track:";
            return builder
                .WithDescription(desc)
                .WithColor(this._MessageSender.ColorGood)
                .WithFooter("music player")
                .WithThumbnailUrl(url)
                .WithFields(new List<EmbedFieldBuilder>
                {
                    this.Field("Title", track.Title),
                    this.Field("Author", track.Author),
                    this.Field("Length", track.Length),
                    this.Field("Stream", track.IsStream),
                })
                .Build();
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
            if (this.ShouldLoop(ply))
            {
                track.ResetPosition();
                await ply.PlayAsync(track, false);
            }
            else
            {
                if (ply.Queue.TryDequeue(out IQueueObject tr))
                {
                    LavaTrack newtrack = tr as LavaTrack;
                    await ply.PlayAsync(newtrack);
                    Embed embed = await this.GetNewTrackEmbed(newtrack, true);
                    await this._MessageSender.Send(ply.TextChannel, embed);
                }
            }
        }

        private async Task OnTrackIssue(LavaPlayer ply, LavaTrack track)
        {
            string msg = $"There was a problem with a track, skipped \'{track.Title}\'";
            this._Logger.Nice("MusicPlayer", ConsoleColor.Red, msg);
            await this._MessageSender.Warning(ply.TextChannel, "music player", msg);
            if (ply.IsPlaying)
                await ply.StopAsync();
        }

        [Event("UserVoiceStateUpdated")] // Don't stay in a voice chat if its empty
        public async Task OnVoiceStateUpdated(SocketUser user, SocketVoiceState oldstate, SocketVoiceState newstate)
        {
            SocketVoiceChannel vc = oldstate.VoiceChannel ?? newstate.VoiceChannel;
            if (vc == null) return;

            LavaPlayer ply = this._LavaClient.GetPlayer(vc.Guild.Id);
            if (vc.Users.Count(x => !x.IsBot) < 1 && ply != null)
                await this.DisconnectAsync(vc);
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