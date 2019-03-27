using Discord;

namespace Energize.Essentials
{
    public static class EmbedExtensions
    {
        public static EmbedBuilder WithField(this EmbedBuilder builder, string title, object value, bool inline = true)
        {
            string val = value.ToString();
            EmbedFieldBuilder fieldbuilder = new EmbedFieldBuilder();
            fieldbuilder
                .WithIsInline(inline)
                .WithName(title)
                .WithValue(val.Length > 1024 ? $"{val.Substring(0, 1021)}..." : val);
            builder.WithFields(fieldbuilder);
            return builder;
        }
    }
}
