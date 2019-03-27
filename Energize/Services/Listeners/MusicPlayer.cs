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
using Energize.Essentials.MessageConstructs;

namespace Energize.Services.Listeners
{
    internal class EnergizePlayer : IEnergizePlayer
    {
        internal EnergizePlayer(LavaPlayer ply)
        {
            this.Lavalink = ply;
            this.IsLooping = false;
        }

        public LavaPlayer Lavalink { get; private set; }

        public bool IsLooping { get; set; }
        public TrackPlayer TrackPlayer { get; set; }

        public bool IsPlaying { get => this.Lavalink.IsPlaying; }
        public bool IsPaused { get => this.Lavalink.IsPaused; }
        public LavaQueue<IQueueObject> Queue { get => this.Lavalink.Queue; }
        public LavaTrack CurrentTrack { get => this.Lavalink.CurrentTrack; }
        public IVoiceChannel VoiceChannel { get => this.Lavalink.VoiceChannel; }
        public ITextChannel TextChannel { get => this.Lavalink.TextChannel; }
        public int Volume { get => this.Lavalink.CurrentVolume; }
    }

    [Service("Music")]
    public class MusicPlayer : IMusicPlayerService
    {
        private readonly DiscordShardedClient _Client;
        private readonly LavaShardClient _LavaClient;
        private readonly Logger _Logger;
        private readonly MessageSender _MessageSender;
        private readonly Dictionary<ulong, EnergizePlayer> _Players;

        private bool _Initialized;

        public MusicPlayer(EnergizeClient client)
        {
            this._Initialized = false;
            this._Players = new Dictionary<ulong, EnergizePlayer>();

            this._Client = client.DiscordClient;
            this._Logger = client.Logger;
            this._MessageSender = client.MessageSender;
            this._LavaClient = new LavaShardClient();

            this._LavaClient.OnTrackException += async (ply, track, _) => await this.OnTrackIssue(ply, track);
            this._LavaClient.OnTrackStuck += async (ply, track, _) => await this.OnTrackIssue(ply, track);
            this._LavaClient.OnTrackFinished += this.OnTrackFinished;
            this._LavaClient.Log += async (logmsg) => this._Logger.Nice("Lavalink", ConsoleColor.Magenta, logmsg.Message);
            this._LavaClient.OnPlayerUpdated += this.OnPlayerUpdated;
        }

        private async Task OnPlayerUpdated(LavaPlayer lply, LavaTrack track, TimeSpan position)
        {
            if (this._Players.ContainsKey(lply.VoiceChannel.GuildId))
            {
                IEnergizePlayer ply = this._Players[lply.VoiceChannel.GuildId];
                if (ply.TrackPlayer != null)
                    await ply.TrackPlayer.Update(track, ply.Volume, ply.IsPaused, ply.IsLooping);
            }

            IGuild guild = lply.VoiceChannel.Guild;
            string msg = $"{DateTime.Now} - Updated track <{track.Title}> ({position}) for player in guild <{guild.Name}>";
            this._Logger.LogTo("victoria.log", msg);
        }

        public LavaRestClient LavaRestClient { get; private set; }

        public async Task<IEnergizePlayer> ConnectAsync(IVoiceChannel vc, ITextChannel chan)
        {
            EnergizePlayer ply;
            if (this._Players.ContainsKey(vc.GuildId))
            {
                ply = this._Players[vc.GuildId];
            }
            else
            {
                ply = new EnergizePlayer(await this._LavaClient.ConnectAsync(vc, chan));
                this._Players.Add(vc.GuildId, ply);
            }

            if (vc.Id != ply.Lavalink.VoiceChannel.Id)
                await this._LavaClient.MoveChannelsAsync(vc);

            return ply;
        }

        public async Task DisconnectAsync(IVoiceChannel vc)
        {
            await this._LavaClient.DisconnectAsync(vc);
            if (this._Players.ContainsKey(vc.GuildId))
            {
                EnergizePlayer ply = this._Players[vc.GuildId];
                this._Players.Remove(vc.GuildId);
                await ply.TrackPlayer.DeleteMessage();
            }
        }

        public async Task<IUserMessage> AddTrack(IVoiceChannel vc, ITextChannel chan, LavaTrack track)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply.IsPlaying)
            {
                ply.Queue.Enqueue(track);
                return await this.SendNewTrack(vc, chan, track);
            }
            else
            {
                await ply.Lavalink.PlayAsync(track, false);
                return await this.SendPlayer(vc, chan, track);
            }
        }

        public async Task<bool> LoopTrack(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            bool islooping = ply.IsLooping;
            ply.IsLooping = !islooping;
            return !islooping;
        }

        public async Task ShuffleTracks(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            ply.Queue.Shuffle();
        }

        public async Task ClearTracks(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            ply.Queue.Clear();
            if (ply.IsPlaying)
                await ply.Lavalink.StopAsync();
        }

        public async Task PauseTrack(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply.CurrentTrack != null)
                await ply.Lavalink.PauseAsync();
        }

        public async Task ResumeTrack(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply.CurrentTrack != null)
                await ply.Lavalink.ResumeAsync();
        }

        public async Task SkipTrack(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            if (ply.IsPlaying)
                await ply.Lavalink.StopAsync();
        }

        public async Task SetTrackVolume(IVoiceChannel vc, ITextChannel chan, int vol)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            await ply.Lavalink.SetVolumeAsync(vol);
        }

        public async Task<string> GetTrackLyrics(IVoiceChannel vc, ITextChannel chan)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
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
            IEnergizePlayer ply = await this.ConnectAsync(vc, msg.Channel as ITextChannel);
            Embed embed = this.GetQueueEmbed(ply, msg);

            return await this._MessageSender.Send(ply.TextChannel, embed);
        }

        private async Task<Embed> GetNewTrackEmbed(LavaTrack track, IMessage msg=null)
        {
            string thumbnailurl;
            try
            {
                thumbnailurl = await track.FetchThumbnailAsync();
            }
            catch
            {
                thumbnailurl = string.Empty;
            }
            EmbedBuilder builder = new EmbedBuilder();
            if(msg != null)
                this._MessageSender.BuilderWithAuthor(msg, builder);
            string desc = "🎶 Added the following track to the queue:";
            if (!string.IsNullOrWhiteSpace(thumbnailurl))
                builder.WithThumbnailUrl(thumbnailurl);
            return builder
                .WithDescription(desc)
                .WithColor(this._MessageSender.ColorGood)
                .WithFooter("music player")
                .WithField("Title", track.Title)
                .WithField("Author", track.Author)
                .WithField("Length", track.IsStream ? " - " : track.Length.ToString())
                .WithField("Stream", track.IsStream)
                .Build();
        }

        public async Task<IUserMessage> SendNewTrack(IVoiceChannel vc, IMessage msg, LavaTrack track)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, msg.Channel as ITextChannel);
            Embed embed = await this.GetNewTrackEmbed(track, msg);

            return await this._MessageSender.Send(ply.TextChannel, embed);
        }

        public async Task<IUserMessage> SendNewTrack(IVoiceChannel vc, ITextChannel chan, LavaTrack track)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            Embed embed = await this.GetNewTrackEmbed(track);

            return await this._MessageSender.Send(ply.TextChannel, embed);
        }

        private void AddPlayerReactions(IUserMessage msg)
        {
            Task.Run(async () =>
            {
                string[] unicodestrings = new string[] { "⏯", "🔁", "⬆", "⬇", "⏭" };
                foreach (string unicode in unicodestrings)
                    await msg.AddReactionAsync(new Emoji(unicode));
            }).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    this._Logger.Nice("MusicPlayer", ConsoleColor.Red, "Could not create reactions, message was deleted or missing permissions");
            });
        }

        public async Task<IUserMessage> SendPlayer(IVoiceChannel vc, ITextChannel chan, LavaTrack track=null)
        {
            IEnergizePlayer ply = await this.ConnectAsync(vc, chan);
            track = track ?? ply.CurrentTrack;


            if (ply.TrackPlayer == null)
            {
                ply.TrackPlayer = new TrackPlayer(vc.GuildId);
                await ply.TrackPlayer.Update(track, ply.Volume, ply.IsPaused, ply.IsLooping, false);
            }
            else
            {
                await ply.TrackPlayer.Update(track, ply.Volume, ply.IsPaused, ply.IsLooping, false);
                await ply.TrackPlayer.DeleteMessage();
            }

            if (track == null) return null;

            ply.TrackPlayer.Message = await this._MessageSender.Send(ply.TextChannel, ply.TrackPlayer.Embed);
            this.AddPlayerReactions(ply.TrackPlayer.Message);
            return ply.TrackPlayer.Message;
        }

        private Embed GetQueueEmbed(IEnergizePlayer ply, IMessage msg = null)
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
                if (newtrack.IsStream)
                    fieldbuilder.WithValue($"**{newtrack.Title}** from **{newtrack.Author}** | Stream");
                else
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
                    queuedisplay += $"{count} - **{tr.Title}** from **{tr.Author}** | {(tr.IsStream ? "Stream" : tr.Length.ToString())}\n";
                    count++;
                }
                fieldbuilder.WithValue(queuedisplay);
                fieldbuilders.Add(fieldbuilder);
            }

            builder.WithFields(fieldbuilders);
            return builder.Build();
        }

        private async Task OnTrackFinished(LavaPlayer lavalink, LavaTrack track, TrackEndReason reason)
        {
            EnergizePlayer ply = this._Players[lavalink.VoiceChannel.GuildId];
            if (ply.IsLooping)
            {
                track.ResetPosition();
                await ply.Lavalink.PlayAsync(track, false);
            }
            else
            {
                if (ply.Queue.TryDequeue(out IQueueObject tr))
                {
                    LavaTrack newtrack = tr as LavaTrack;
                    await ply.Lavalink.PlayAsync(newtrack);
                    await this.SendPlayer(ply.VoiceChannel, ply.TextChannel, newtrack);
                }
                else
                {
                    await ply.TrackPlayer.DeleteMessage();
                }
            }
        }

        private async Task OnTrackIssue(LavaPlayer ply, LavaTrack track)
        {
            string msg = $"There was a problem with a track, skipped \'{track.Title}\'";
            this._Logger.Nice("MusicPlayer", ConsoleColor.Red, msg);
            await this._MessageSender.Warning(ply.TextChannel, "music player", msg);
            await this.SkipTrack(ply.VoiceChannel, ply.TextChannel);
        }

        private delegate Task ReactionCallback(MusicPlayer music, IEnergizePlayer ply);
        private readonly static Dictionary<string, ReactionCallback> _ReactionCallbacks = new Dictionary<string, ReactionCallback>
        {
            ["⏯"] = async (music, ply) =>
            {
                if (!ply.IsPlaying) return;
                if (ply.IsPaused)
                    await music.ResumeTrack(ply.VoiceChannel, ply.TextChannel);
                else
                    await music.PauseTrack(ply.VoiceChannel, ply.TextChannel);
            },
            ["🔁"] = async (music, ply) => await music.LoopTrack(ply.VoiceChannel, ply.TextChannel),
            ["⬆"] = async (music, ply) => await music.SetTrackVolume(ply.VoiceChannel, ply.TextChannel, ply.Volume + 10),
            ["⬇"] = async (music, ply) => await music.SetTrackVolume(ply.VoiceChannel, ply.TextChannel, ply.Volume - 10),
            ["⏭"] = async (music, ply) => await music.SkipTrack(ply.VoiceChannel, ply.TextChannel),
        };

        private bool IsValidEmote(SocketReaction reaction)
        {
            if (reaction.UserId == this._Client.CurrentUser.Id) return false;
            return _ReactionCallbacks.ContainsKey(reaction.Emote.Name);
        }

        private async Task OnReaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (chan is IDMChannel || !cache.HasValue || !this.IsValidEmote(reaction)) return;
            IGuildChannel gchan = (IGuildChannel)chan;
            if (!this._Players.ContainsKey(gchan.GuildId)) return;
            IEnergizePlayer ply = this._Players[gchan.GuildId];
            await _ReactionCallbacks[reaction.Emote.Name](this, ply);
            await ply.TrackPlayer.Update(ply.CurrentTrack, ply.Volume, ply.IsPaused, ply.IsLooping, true);
        }

        [Event("ReactionAdded")]
        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReaction(cache, chan, reaction);

        [Event("ReactionRemoved")]
        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReaction(cache, chan, reaction);

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