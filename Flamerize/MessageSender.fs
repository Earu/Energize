namespace Flamerize

open DSharpPlus.Entities

type public EmbedColor = Error = 0xe24444 | Warning = 0xe27b44 | Normal = 0xc8c8c8 | Good = 0xdddddd

[<AbstractClass; Sealed>]
type public MessageSender() =
    
    static member CreateEmbed (user: DiscordUser) (head: string) (txt: string) (col: EmbedColor): DiscordEmbed = 
        let builder = new DiscordEmbedBuilder()
        builder.Author <- new DiscordEmbedBuilder.EmbedAuthor()
        builder.Author.Name <- user.Username
        builder.Author.IconUrl <- user.AvatarUrl
        builder.Color <- new DiscordColor(LanguagePrimitives.EnumToValue col)
        builder.Footer <- new DiscordEmbedBuilder.EmbedFooter()
        builder.Footer.Text <- head
        builder.Description <- txt
        builder.Build()
