namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Image")>]
module Image =
    open Energize.Commands.Context
    open Energize.Commands.UserHelper
    open Discord
    open Energize.Commands.AsyncHelper
    open Discord.WebSocket
    
    [<CommandParameters(1)>]
    [<Command("avatar", "Gets the avatar of a user", "avatar <user|userid>")>]
    let avatar (ctx : CommandContext) = async {
        match findUser ctx ctx.arguments.[0] true with
        | Some user ->
            let avurl = 
                let url = user.GetAvatarUrl(ImageFormat.Auto)
                url.Remove(url.Length - 9)
            let builder = EmbedBuilder()
            ctx.messageSender.BuilderWithAuthor(ctx.message, builder)
            builder
                .WithFooter(ctx.commandName)
                .WithImageUrl(avurl)
                .WithColor(ctx.messageSender.ColorGood)
                |> ignore
            awaitIgnore (ctx.messageSender.Send(ctx.message, builder.Build()))
        | None ->
            ctx.sendWarn None "Could not find any user for your input"
    }

    [<GuildOnlyCommand>]
    [<Command("icon", "Gets the avatar of the guild", "icon <nothing>")>]
    let icon (ctx : CommandContext) = async {
        let guser = ctx.message.Author :?> SocketGuildUser
        let guild = guser.Guild
        let builder = EmbedBuilder()
        ctx.messageSender.BuilderWithAuthor(ctx.message, builder)
        builder
            .WithFooter(ctx.commandName)
            .WithImageUrl(guild.IconUrl)
            .WithColor(ctx.messageSender.ColorGood)
            |> ignore
        awaitIgnore (ctx.messageSender.Send(ctx.message, builder.Build()))
    }

    [<CommandParameters(1)>]
    [<Command("e", "Gets the picture of a guild emoji","e <guildemoji>")>]
    let emoji (ctx : CommandContext) = async {
        let e = ref null
        if Emote.TryParse(ctx.arguments.[0], e) then
            let builder = EmbedBuilder()
            ctx.messageSender.BuilderWithAuthor(ctx.message, builder)
            builder
                .WithFooter(ctx.commandName)
                .WithImageUrl(e.Value.Url)
                .WithColor(ctx.messageSender.ColorGood)
                |> ignore
            awaitIgnore (ctx.messageSender.Send(ctx.message, builder.Build()))
        else
            ctx.sendWarn None "A guild emoji is expected as parameter"
    }

    //rework old image commands