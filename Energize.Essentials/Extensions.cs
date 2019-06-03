using Discord;
using System.IO;
using System.Linq;

namespace Energize.Essentials
{
    public static class Extensions
    {
        public static EmbedBuilder WithField(this EmbedBuilder builder, string title, object value, bool inline = true)
        {
            string val = value.ToString();
            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();
            fieldBuilder
                .WithIsInline(inline)
                .WithName(title)
                .WithValue(val.Length > 1024 ? $"{val.Substring(0, 1021)}..." : val);
            builder.WithFields(fieldBuilder);
            return builder;
        }

        public static EmbedBuilder WithLimitedDescription(this EmbedBuilder builder, string description)
            => builder.WithDescription(description.Length > 2048 ? $"{description.Substring(0, 2045)}..." : description);

        public static EmbedBuilder WithAuthorNickname(this EmbedBuilder builder, IMessage msg)
        {
            if (msg.Channel is IGuildChannel)
            {
                IGuildUser author = msg.Author as IGuildUser;
                string nick = author.Nickname != null ? $"{author.Nickname} ({author})" : author.ToString();
                string url = author.GetAvatarUrl(ImageFormat.Auto, 32);
                builder.WithAuthor(nick, url);
            }
            else
            {
                builder.WithAuthor(msg.Author);
            }

            return builder;
        }

        private static readonly string[] ValidExtensions = new string[] { "mp3", "mp4", "ogg", "wav", "webm" };

        public static bool IsPlayableAttachment(this Attachment attachment)
        {
            string fileName = attachment.Filename;
            FileInfo fileInfo = new FileInfo(fileName);
            if (string.IsNullOrWhiteSpace(fileInfo.Extension) || fileInfo.Extension.Length < 2) return false; // 2 = ".|xxxx" 
            return ValidExtensions.Any(ext => ext.Equals(fileInfo.Extension.Substring(1)));
        }
    }
}
