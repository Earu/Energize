using Discord;

namespace Energize.Essentials
{
    public static class EmbedExtensions
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
    }
}
