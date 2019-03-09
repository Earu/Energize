namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Image")>]
module Image =
    open Energize.Commands.Context
    open Energize.Commands.UserHelper
    open Discord
    open Energize.Commands.AsyncHelper
    open Discord.WebSocket
    open System.Drawing
    open Energize.Commands.ImageUrlProvider
    open System.Text.RegularExpressions
    open Energize.Toolkit

    [<CommandParameters(1)>]
    [<Command("avatar", "Gets the avatar of a user", "avatar <user|userid>")>]
    let avatar (ctx : CommandContext) = async {
        return 
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
                [ ctx.sendEmbed (builder.Build()) ]
            | None ->
                [ ctx.sendWarn None "Could not find any user for your input" ]
    }

    [<GuildCommand>]
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
        return [ ctx.sendEmbed (builder.Build()) ]
    }

    [<CommandParameters(1)>]
    [<Command("e", "Gets the picture of a guild emoji","e <guildemoji>")>]
    let emoji (ctx : CommandContext) = async {
        let e = ref null
        return
            if Emote.TryParse(ctx.arguments.[0], e) then
                let builder = EmbedBuilder()
                ctx.messageSender.BuilderWithAuthor(ctx.message, builder)
                builder
                    .WithFooter("emote")
                    .WithImageUrl(e.Value.Url)
                    .WithColor(ctx.messageSender.ColorGood)
                    |> ignore
                [ ctx.sendEmbed (builder.Build()) ]
            else
                [ ctx.sendWarn (Some "emote") "A guild emoji is expected as parameter" ]
    }

    [<Command("inspiro", "Quotes from inspirobot", "inspiro <nothing>")>]
    let inspiro (ctx : CommandContext) = async {
        let endpoint = "http://inspirobot.me/api?generate=true"
        let url = awaitResult (HttpClient.GetAsync(endpoint, ctx.logger))
        let builder = EmbedBuilder()
        ctx.messageSender.BuilderWithAuthor(ctx.message, builder)
        builder
            .WithColor(ctx.messageSender.ColorGood)
            .WithImageUrl(url)
            .WithFooter(ctx.commandName)
            |> ignore
        return [ ctx.sendEmbed (builder.Build()) ]
    }

    [<Command("wew", "wews a picture (WIP)", "wew <url|user|userid|nothing>")>]
    let wew (ctx : CommandContext) = async {
        let url = 
            if ctx.hasArguments then
                match findUser ctx ctx.input false with
                | Some user -> Some (user.GetAvatarUrl(ImageFormat.Auto))
                | None -> getLastImgUrl ctx.message
            else
                ctx.cache.lastImageUrl

        return 
            match url with
            | Some url ->
                [ ctx.sendOK None url ]
            | None ->
                [ ctx.sendWarn None "Could not find any image to use" ]
    }