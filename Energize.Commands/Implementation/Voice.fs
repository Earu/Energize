namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Voice")>]
module Voice =
    open Energize.Commands.Context
    open Discord
    open Energize.Interfaces.Services.Listeners
    open Energize.Commands.AsyncHelper

    let private getVoiceChannel (user : IGuildUser) =
        match user.VoiceChannel with
        | null -> None
        | vc -> Some vc

    [<GuildCommand>]
    [<Command("join", "Joins your voice channel", "join <nothing>")>]
    let join (ctx : CommandContext) = async {
        let music = ctx.serviceManager.GetService<IMusicPlayerService>("Music")
        let guser = ctx.message.Author :?> IGuildUser
        return
            match getVoiceChannel guser with
            | None ->
                [ ctx.sendWarn None "You're not in a voice channel" ]
            | Some vc -> 
                match music.LavaClient.GetPlayer(guser.GuildId) with
                | null ->
                    awaitIgnore (music.LavaClient.ConnectAsync(vc))
                | _ ->
                    await (music.LavaClient.DisconnectAsync(vc))
                [ ctx.sendOK None (sprintf "Joining %s's voice channel" guser.Mention) ]
    }

    [<GuildCommand>]
    [<Command("leave", "Leaves your voice channel", "leave <nothing>")>]
    let leave (ctx : CommandContext) = async {
        let music = ctx.serviceManager.GetService<IMusicPlayerService>("Music")
        let guser = ctx.message.Author :?> IGuildUser
        return
            match music.LavaClient.GetPlayer(guser.GuildId) with
            | null -> 
                [ ctx.sendWarn None "No voice channel to leave" ]
            | ply ->
                await (music.LavaClient.DisconnectAsync(ply.VoiceChannel))
                [ ctx.sendOK None (sprintf "Leaving %s's voice channel" guser.Mention) ]
    }

    [<CommandParameters(1)>]
    [<GuildCommand>]
    [<Command("play", "Plays a track", "play <url>")>]
    let play (ctx : CommandContext) = async {
        let music = ctx.serviceManager.GetService<IMusicPlayerService>("Music")
        let res = awaitResult (music.LavaRestClient.SearchYouTubeAsync(ctx.arguments.[0]))
        let guser = ctx.message.Author :?> IGuildUser
        return
            match res.Tracks |> Seq.toList with
            | tracks when tracks.Length > 0 ->
                let tr = tracks.[0]
                let ply =
                    match music.LavaClient.GetPlayer(guser.GuildId) with
                    | null -> 
                        match getVoiceChannel guser with 
                        | Some vc -> Some (awaitResult (music.LavaClient.ConnectAsync(vc))) 
                        | None -> None
                    | ply -> Some ply
                match ply with
                | Some ply ->
                    await (ply.PlayAsync(tr, false))
                    [ ctx.sendOK None (sprintf "🎶 Now playing: %s by %s" tr.Title tr.Author) ]
                | None -> 
                    [ ctx.sendWarn None "You're not in a voice channel" ]
            | _ ->
                [ ctx.sendWarn None "Could not find any track for your input" ]
    }