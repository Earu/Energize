using Discord;
using Discord.WebSocket;
using Energize.Essentials;
using Energize.Interfaces.Services;
using Energize.Interfaces.Services.Listeners;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Victoria.Entities;

namespace Energize.Services.Listeners
{
    [Service("MusicUsability")]
    public class MusicPlayerUsabilityService : ServiceImplementationBase, IServiceImplementation
    {
        private static readonly Emoji Emote = new Emoji("⏯");

        private readonly Logger _Logger;
        private readonly ServiceManager _ServiceManager;

        public MusicPlayerUsabilityService(EnergizeClient client)
        {
            this._Logger = client.Logger;
            this._ServiceManager = client.ServiceManager;
        }

        private bool IsYoutubeURL(string url)
        {
            string pattern = @"^(http(s)?:\/\/)?((w){3}.)?youtu(be|.be)?(\.com)?\/.+";
            return Regex.IsMatch(url, pattern);
        }

        private bool IsSoundcloudURL(string url)
        {
            string pattern = @"^https?:\/\/(soundcloud\.com|snd\.sc)\/(.*)";
            return Regex.IsMatch(url, pattern);
        }

        private bool IsTwitchURL(string url)
        {
            string pattern = @"^https?://www.twitch.tv/.+";
            return Regex.IsMatch(url, pattern);
        }

        private bool IsValidURL(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return this.IsYoutubeURL(url) || this.IsSoundcloudURL(url) || this.IsTwitchURL(url);
        }

        private bool IsValidMessage(IMessage msg)
        {
            if (msg.Author.Id == Config.Instance.Discord.BotID) return false;
            if (msg.Embeds.Count < 1) return false;

            return true;
        }

        private string SanitizeYoutubeUrl(string url)
        {
            string pattern = @"(?:youtube(?:-nocookie)?\.com\/(?:[^\/\n\s]+\/\S+\/|(?:v|e(?:mbed)?)\/|\S*?[?&]v=)|youtu\.be\/)([a-zA-Z0-9_-]{11})\W";
            if (Regex.IsMatch(url, pattern))
            {
                string identifier = Regex.Match(url, pattern).Groups[1].Value;
                return $"https://www.youtube.com/watch?v={identifier}";
            }
            else
            {
                return url;
            }
        }

        [Event("MessageReceived")]
        public async Task OnMessageReceived(SocketMessage msg)
        {
            if (!this.IsValidMessage(msg)) return;

            Embed embed = msg.Embeds.Last();
            if (this.IsValidURL(embed.Url))
            {
                try
                {
                    IUserMessage usermsg = (IUserMessage)msg;
                    await usermsg.AddReactionAsync(Emote);
                }
                catch
                {
                    this._Logger.Nice("MusicPlayer", ConsoleColor.Red, "Could not create reactions, message was deleted or missing permissions");
                }
            }
        }

        [Event("ReactionAdded")]
        public async Task OnReactionAdded(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReaction(cache, chan, reaction);

        [Event("ReactionRemoved")]
        public async Task OnReactionRemoved(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
            => await this.OnReaction(cache, chan, reaction);

        private async Task OnReaction(Cacheable<IUserMessage, ulong> cache, ISocketMessageChannel chan, SocketReaction reaction)
        {
            if (!reaction.Emote.Name.Equals(Emote.Name)) return;
            if (reaction.UserId == Config.Instance.Discord.BotID) return;
            if (!(chan is IGuildChannel) || reaction.User.Value == null) return;
            IGuildUser guser = (IGuildUser)reaction.User.Value;
            if (guser.VoiceChannel == null) return;
            ITextChannel textchan = (ITextChannel)chan;

            IUserMessage msg = await cache.GetOrDownloadAsync();
            if (!this.IsValidMessage(msg)) return;
            IEmbed embed = msg.Embeds.Last();
            if (this.IsValidURL(embed.Url))
            {
                IMusicPlayerService music = this._ServiceManager.GetService<IMusicPlayerService>("Music");
                SearchResult result = await music.LavaRestClient.SearchTracksAsync(this.SanitizeYoutubeUrl(embed.Url));
                List<LavaTrack> tracks = result.Tracks.ToList();
                switch(result.LoadType)
                {
                    case LoadType.SearchResult:
                    case LoadType.TrackLoaded:
                        if (tracks.Count > 0)
                            await music.AddTrack(guser.VoiceChannel, textchan, tracks[0]);
                        break;
                    case LoadType.PlaylistLoaded:
                        if (tracks.Count > 0)
                            await music.AddPlaylist(guser.VoiceChannel, textchan, result.PlaylistInfo.Name, tracks);
                        break;
                }
            }
        }
    }
}
