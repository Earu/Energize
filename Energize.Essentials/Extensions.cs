using Discord;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Energize.Essentials
{
    public enum EmbedColorType
    {
        Good = 0,
        Warning = 1,
        Danger = 2,
        Normal = 3,
    }

    public static class Extensions
    {
        public static EmbedBuilder WithField(this EmbedBuilder builder, string title, object value, bool inline = true)
        {
            if (string.IsNullOrWhiteSpace(title)) return builder;

            if (value == null) return builder;
            string val = value.ToString();
            if (string.IsNullOrWhiteSpace(val)) return builder;

            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();
            fieldBuilder
                .WithIsInline(inline)
                .WithName(title)
                .WithValue(val.Length > 1024 ? $"{val.Substring(0, 1021)}..." : val);

            return builder.WithFields(fieldBuilder);
        }

        public static EmbedBuilder WithLimitedTitle(this EmbedBuilder builder, string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return builder;
            else
                return builder.WithTitle(title.Length > 256 ?  $"{title.Substring(0,253)}..." : title);
        }

        public static EmbedBuilder WithLimitedDescription(this EmbedBuilder builder, string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                return builder;
            else
                return builder.WithDescription(description.Length > 2048 ? $"{description.Substring(0, 2045)}..." : description);
        }

        public static EmbedBuilder WithAuthorNickname(this EmbedBuilder builder, IMessage msg)
            => builder.WithAuthorNickname(msg.Author);

        public static EmbedBuilder WithAuthorNickname(this EmbedBuilder builder, IUser user)
        {
            if (user is IGuildUser author)
            {
                string nick = author.Nickname != null ? $"{author.Nickname} ({author})" : author.ToString();
                string url = author.GetAvatarUrl(ImageFormat.Auto, 32);
                builder.WithAuthor(nick, url);
            }
            else
            {
                builder.WithAuthor(user);
            }

            return builder;
        }

        public static EmbedBuilder WithColorType(this EmbedBuilder builder, EmbedColorType colorType)
        {
            switch(colorType)
            {
                case EmbedColorType.Good:
                    return builder.WithColor(MessageSender.SColorGood);
                case EmbedColorType.Warning:
                    return builder.WithColor(MessageSender.SColorWarning);
                case EmbedColorType.Danger:
                    return builder.WithColor(MessageSender.SColorDanger);
                case EmbedColorType.Normal:
                default:
                    return builder.WithColor(MessageSender.SColorNormal);
            }
        }

        private static readonly string[] ValidExtensions = new string[] { "mp3", "mp4", "ogg", "wav", "webm", "mov" };
        private static readonly Regex URLExtensionRegex = new Regex(@"https?:\/\/[^\s\/]+\/[^\s\.]+\.([A-Za-z0-9]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool IsPlayableURL(this string url)
        {
            if (HttpClient.IsURL(url))
            {
                Match match = URLExtensionRegex.Match(url);
                if (match == null) return false;

                string extension = match.Groups[1].Value;
                return ValidExtensions.Any(ext => ext.Equals(extension));
            }
            else
            {
                FileInfo fileInfo = new FileInfo(url);
                if (string.IsNullOrWhiteSpace(fileInfo.Extension) || fileInfo.Extension.Length < 2) return false; // 2 = ".|xxxx" 
                return ValidExtensions.Any(ext => ext.Equals(fileInfo.Extension.Substring(1)));
            }
        }

        public static bool IsPlayableAttachment(this Attachment attachment)
            => attachment.Filename.IsPlayableURL();
    }
}
