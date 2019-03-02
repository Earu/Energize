namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Audio")>]
module Audio =
    open Energize.Commands.Context
    open Discord
    open Energize.Commands.AsyncHelper
    open Discord.WebSocket

    [<GuildCommand>]
    [<Command("join", "Joins the voice channel you are in", "join <nothing>")>]
    let join (ctx : CommandContext) = async {
        let guser = ctx.message.Author :?> SocketGuildUser
        match guser.VoiceChannel with
        | null -> 
            ctx.sendWarn None "You are not in a voice chat"
        | vc ->
            vc.ConnectAsync() |> ignore //needs to be awaited in another tread
            ctx.sendOK None "OK, joining"
    }

    [<GuildCommand>]
    [<Command("leave", "Leaves the voice channel", "leave <nothing>")>]
    let leave (ctx : CommandContext) = async {
        1
    }

