namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Voice")>]
module Voice =
    open Energize.Commands.Context
    open Discord
    open Energize.Interfaces.Services.Listeners
    open Energize.Commands.AsyncHelper

    let private musicAction (ctx : CommandContext) (cb : IMusicPlayerService -> IVoiceChannel -> IGuildUser -> IUserMessage list) =
        let music = ctx.serviceManager.GetService<IMusicPlayerService>("Music")
        let guser = ctx.message.Author :?> IGuildUser
        match guser.VoiceChannel with
        | null -> [ ctx.sendWarn None "Not in a voice channel" ]
        | vc -> cb music vc guser

    [<GuildCommand>]
    [<Command("join", "Joins your voice channel", "join <nothing>")>]
    let join (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc user ->
            awaitIgnore (music.ConnectAsync(vc, ctx.message.Channel :?> ITextChannel))
            [ ctx.sendOK None (sprintf "Joining %s's voice channel" user.Mention) ]
        )
    }

    [<GuildCommand>]
    [<Command("leave", "Leaves your voice channel", "leave <nothing>")>]
    let leave (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            await (music.DisconnectAsync(vc))
            [ ctx.sendOK None "Leaving the current voice channel" ]
        )
    }

    [<CommandParameters(1)>]
    [<GuildCommand>]
    [<Command("play", "Plays a track", "play <song name>")>]
    let play (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            let res = awaitResult (music.LavaRestClient.SearchYouTubeAsync(ctx.arguments.[0]))
            match res.Tracks |> Seq.toList with
            | tracks when tracks.Length > 0 ->
                let tr = tracks.[0]
                let textChan = ctx.message.Channel :?> ITextChannel
                awaitIgnore (music.ConnectAsync(vc, textChan))
                await (music.AddTrack(vc, textChan, tr))
                let msg = awaitResult (music.SendQueue(vc, ctx.message))
                [ msg ]
            | _ ->
                [ ctx.sendWarn None "Could not find any track for your input" ]
        )
    }

    [<GuildCommand>]
    [<Command("pause", "Pauses the current track", "pause <nothing>")>]
    let pause (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            await (music.PauseTrack(vc, ctx.message.Channel :?> ITextChannel))
            [ ctx.sendOK None "Paused the current track" ]
        )
    }

    [<GuildCommand>]
    [<Command("resume", "Resumes the current track", "resume <nothing>")>]
    let resume (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            await (music.ResumeTrack(vc, ctx.message.Channel :?> ITextChannel))
            [ ctx.sendOK None "Resumed the current track" ]
        )
    }

    [<GuildCommand>]
    [<Command("skip", "Skips the current track", "skip <nothing>")>]
    let skip (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            await (music.SkipTrack(vc, ctx.message.Channel :?> ITextChannel))
            [ ctx.sendOK None "Skipped the current track" ]
        )
    }

    [<GuildCommand>]
    [<Command("queue", "Displays the current track queue", "queue <nothing>")>]
    let queue (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            let msg = awaitResult (music.SendQueue(vc, ctx.message))
            [ msg ]
        )
    }