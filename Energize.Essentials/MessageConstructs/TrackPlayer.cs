using Discord;
using System;
using System.Threading.Tasks;
using Victoria;
using Victoria.Entities;

namespace Energize.Essentials.MessageConstructs
{
    public class TrackPlayer
    {
        public TrackPlayer(ulong guildid)
        {
            this.GuildID = guildid;
        }

        public IUserMessage Message { get; set; }
        public Embed Embed { get; private set; }
        public ulong GuildID { get; private set; }

        private string FormattedTrack(LavaTrack track)
        {
            string len = (track.IsStream ? TimeSpan.Zero : track.Length).ToString(@"hh\:mm\:ss");
            string pos = (track.IsStream ? TimeSpan.Zero : track.Position).ToString(@"hh\:mm\:ss");
            string line;
            if (track.IsStream)
            {
                line = new string('─', 24) + "⚪";
            }
            else
            {
                double perc = (double)track.Position.Ticks / track.Length.Ticks * 100;
                int circlepos = (int)Math.Ceiling(25.0 / 100.0 * perc);
                if (circlepos > 0)
                    line = new string('─', circlepos - 1) + "⚪" + new string('─', 25 - circlepos);
                else
                    line = "⚪" + new string('─', 24);
            }
            string res = $"`{len}`\n```http\n▶ {line} {pos}\n```";

            return res;
        }

        private Embed BuildEmbed(LavaTrack track, int volume, bool paused, bool looping)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder
                .WithColor(MessageSender.SColorGood)
                .WithDescription("🎶 Now playing the following track")
                .WithField("Title", track.Title)
                .WithField("Author", track.Author)
                .WithField("Stream", track.IsStream)
                .WithField("Volume", $"{volume}%")
                .WithField("Paused", paused)
                .WithField("Looping", looping)
                .WithFooter("music player");

            if (!track.IsStream)
                builder.WithField("Length", this.FormattedTrack(track), false);
            else
                builder.WithField("Length", " - ", false);

            return builder.Build();
        }

        public async Task DeleteMessage()
        {
            if (this.Message == null) return;
            try
            {
                await this.Message.DeleteAsync();
                this.Message = null;
            }
            catch
            {
                this.Message = null;
            }
        }

        public async Task Update(LavaTrack track, int volume, bool paused, bool looping, bool modify = true)
        {
            if (track == null) return;

            if (!modify)
            {
                this.Embed = this.BuildEmbed(track, volume, paused, looping);
                return;
            }

            if (this.Message == null) return;

            this.Embed = this.BuildEmbed(track, volume, paused, looping);
            await this.Message.ModifyAsync(prop => prop.Embed = this.Embed);
        }
    }
}
