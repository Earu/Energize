namespace Flamerize

open DSharpPlus.Entities

[<AbstractClass; Sealed>]
type public MessageSender() =
    
    static member CreateEmbed (user: DiscordUser) (head: string) (txt: string): DiscordEmbed = 
        let builder = new DiscordEmbedBuilder()
        builder.Author <- new DiscordEmbedBuilder.EmbedAuthor()
        builder.Author.Name <- user.Username
        builder.Author.IconUrl <- user.AvatarUrl
        builder.Color <- new DiscordColor(0xdddddd)
        builder.Footer <- new DiscordEmbedBuilder.EmbedFooter()
        builder.Footer.Text <- head
        builder.Description <- txt
        builder.Build()
