namespace Energize.Commands.Implementation

open Energize.Commands.Command
open System.Text.RegularExpressions
open Energize.Commands.Context
open Discord
open Energize.Interfaces.Services.Listeners
open Energize.Commands.AsyncHelper
open Energize.Essentials
open Victoria.Entities
open Energize.Interfaces.Services.Senders
open System.Web

[<CommandModule("Music")>]
module Voice =
    let private musicAction (ctx : CommandContext) (cb : IMusicPlayerService -> IVoiceChannel -> IGuildUser -> IUserMessage list) =
        let music = ctx.serviceManager.GetService<IMusicPlayerService>("Music")
        let guser = ctx.message.Author :?> IGuildUser
        match guser.VoiceChannel with
        | null -> [ ctx.sendWarn None "Not in a voice channel" ]
        | vc -> cb music vc guser

    let private handleSearchResult (music : IMusicPlayerService) (ctx : CommandContext) (res : SearchResult) (vc : IVoiceChannel) =
        match res.LoadType with
        | LoadType.LoadFailed ->
            [ ctx.sendWarn None "Could not load the specified track" ]
        | LoadType.NoMatches ->
            [ ctx.sendWarn None "Could not find any matches for the specified track" ]
        | LoadType.PlaylistLoaded ->
            let index = res.PlaylistInfo.SelectedTrack 
            match res.Tracks |> Seq.toList |> List.tryItem index with
            | Some tr ->
                let textChan = ctx.message.Channel :?> ITextChannel
                [ awaitResult (music.AddTrackAsync(vc, textChan, tr)) ]
            | None ->
                [ ctx.sendWarn None "Could not find any matches for the specified track" ]
        | _ ->
            let tracks = res.Tracks |> Seq.toList 
            if tracks.Length > 0 then
                let tr = tracks.[0]
                let textChan = ctx.message.Channel :?> ITextChannel
                [ awaitResult (music.AddTrackAsync(vc, textChan, tr)) ]
            else
                [ ctx.sendWarn None "Could not find any matches for the specified track" ]

    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<Command("join", "Joins your voice channel", "join <nothing>")>]
    let join (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc user ->
            awaitIgnore (music.ConnectAsync(vc, ctx.message.Channel :?> ITextChannel))
            [ ctx.sendOK None (sprintf "Joining %s's voice channel" user.Mention) ]
        )
    }

    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<Command("leave", "Leaves your voice channel", "leave <nothing>")>]
    let leave (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            await (music.DisconnectAsync(vc))
            [ ctx.sendOK None "Leaving the current voice channel" ]
        )
    }

    let private sanitizeYtUrl (url : string) =
        let pattern = @"(?:youtube(?:-nocookie)?\.com\/(?:[^\/\n\s]+\/\S+\/|(?:v|e(?:mbed)?)\/|\S*?[?&]v=)|youtu\.be\/)([a-zA-Z0-9_-]{11})\W"
        if Regex.IsMatch(url, pattern) then
            let ytIdentifier = Regex.Match(url, pattern).Groups.[1].Value
            sprintf "https://www.youtube.com/watch?v=%s" ytIdentifier
        else
            url

    [<CommandParameters(1)>]
    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<Command("playurl", "Tries to play a track/stream from an url", "playurl <url>")>]
    let playUrl (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            if HttpClient.IsURL(ctx.input) then
                let input = sanitizeYtUrl ctx.input
                let res = awaitResult (music.LavaRestClient.SearchTracksAsync(input))
                handleSearchResult music ctx res vc
            else
                [ ctx.sendWarn None "Expected an url" ]
        )
    }

    // Use playurl whenever passed a link, users tend to do that
    let private tryPlay (ctx : CommandContext) (cb : CommandContext -> Async<IUserMessage list>) = 
        if HttpClient.IsURL(ctx.input) then
            playUrl ctx
        else
            cb ctx
            
    [<CommandParameters(1)>]
    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<Command("play", "Plays a track/stream from youtube", "play <song name>")>]
    let play (ctx : CommandContext) = 
        tryPlay ctx (fun ctx -> async {
            return musicAction ctx (fun music vc _ ->   
                let res = awaitResult (music.LavaRestClient.SearchYouTubeAsync(ctx.input))
                handleSearchResult music ctx res vc
            )
        })

    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<Command("playing", "Shows the track currently playing", "playing <nothing>")>]
    let playing (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            let textChan = ctx.message.Channel :?> ITextChannel
            let ply = awaitResult (music.ConnectAsync(vc, textChan))
            let msg = awaitResult (music.SendPlayerAsync(ply, null, ctx.message.Channel))
            match msg with
            | null -> 
                [ ctx.sendOK None "Nothing is playing" ]
            | _ -> [ msg ]
        )
    }

    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<Command("empty", "Empties the track queue", "empty <nothing>")>]
    let empty (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            let textChan = ctx.message.Channel :?> ITextChannel
            await (music.ClearTracksAsync(vc, textChan))
            [ ctx.sendOK None "Emptied the track queue" ]
        )
    }

    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<Command("pause", "Pauses the current track/stream", "pause <nothing>")>]
    let pause (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            await (music.PauseTrackAsync(vc, ctx.message.Channel :?> ITextChannel))
            [ ctx.sendOK None "Paused the current track" ]
        )
    }

    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<Command("resume", "Resumes the current track/stream", "resume <nothing>")>]
    let resume (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            await (music.ResumeTrackAsync(vc, ctx.message.Channel :?> ITextChannel))
            [ ctx.sendOK None "Resumed the current track" ]
        )
    }

    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<Command("skip", "Skips the current track/stream", "skip <nothing>")>]
    let skip (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            await (music.SkipTrackAsync(vc, ctx.message.Channel :?> ITextChannel))
            [ ctx.sendOK None "Skipped the current track" ]
        )
    }

    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<Command("loop", "Loops or unloop the current track/stream", "loop <nothing>")>]
    let loop (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            let looping = awaitResult (music.LoopTrackAsync(vc, ctx.message.Channel :?> ITextChannel))
            if looping then
                [ ctx.sendOK None "Looping the current track" ]
            else
                [ ctx.sendOK None "Stopped looping the current track" ]
        )
    }

    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<Command("shuffle", "Shuffles the track queue", "shuffle <nothing>")>]
    let shuffle (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            await (music.ShuffleTracksAsync(vc, ctx.message.Channel :?> ITextChannel))
            [ ctx.sendOK None "Shuffled the track queue" ]
        )
    }

    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<CommandParameters(1)>]
    [<Command("vol", "Sets the audio volume", "vol <number>")>]
    let volume (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            try
                let vol = int ctx.arguments.[0]
                await (music.SetTrackVolumeAsync(vc, ctx.message.Channel :?> ITextChannel, vol))
                [ ctx.sendOK None (sprintf "Setting the volume to %d" vol) ]
            with _ ->
                [ ctx.sendWarn None "Incorrect volume, expecting a number" ]
        )
    }

    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<CommandParameters(1)>]
    [<Command("seek", "Forwards the currently playing track by the amount of seconds specified", "seek <seconds>")>]
    let seek (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            try
                let secs = int ctx.arguments.[0]
                await (music.SeekTrackAsync(vc, ctx.message.Channel :?> ITextChannel, secs))
                [ ctx.sendOK None (sprintf "Forwarding the current track by %ds" secs) ]
            with _ ->
                [ ctx.sendWarn None "Incorrect amount of seconds, expecting a number" ]
        )
    }

    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<Command("queue", "Displays the current track queue", "queue <nothing>")>]
    let queue (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            [ awaitResult (music.SendQueueAsync(vc, ctx.message)) ]
        )
    }

    let private baseSongSearch (ctx : CommandContext) (result : SearchResult) = async {
        let len = result.Tracks |> Seq.length
        return 
            if len > 0 then
                let paginator = ctx.serviceManager.GetService<IPaginatorSenderService>("Paginator")
                [ awaitResult (paginator.SendPlayerPaginator(ctx.message, result.Tracks, fun track ->
                    let page = result.Tracks |> Seq.tryFindIndex (fun v -> v.Uri.Equals(track.Uri))
                    sprintf "%s #%d out of %d results for \"%s\"\n%s" ctx.authorMention (page.Value + 1) len ctx.arguments.[0] (track.Uri.ToString()) 
                )) ]
            else
                [ ctx.sendWarn None "Could not find any songs" ]
    }

    [<CommandParameters(1)>]
    [<Command("yt", "Searches youtube for a video", "yt <search>")>]
    let youtube (ctx : CommandContext) = 
        tryPlay ctx (fun ctx -> 
            let music = ctx.serviceManager.GetService<IMusicPlayerService>("Music")
            let res = awaitResult (music.LavaRestClient.SearchYouTubeAsync(ctx.input))
            baseSongSearch ctx res
        )


    [<CommandParameters(1)>]
    [<Command("sc", "Searches soundcloud for a song", "sc <search>")>]
    let soundcloud (ctx : CommandContext) =
        tryPlay ctx (fun ctx ->
            let music = ctx.serviceManager.GetService<IMusicPlayerService>("Music")
            let res = awaitResult (music.LavaRestClient.SearchSoundcloudAsync(ctx.input))
            baseSongSearch ctx res
        )

    type TwitchChannelObj = { url : string }
    type TwitchStreamObj = { channel : TwitchChannelObj }
    type TwitchObj = { streams : TwitchStreamObj list }
    [<CommandParameters(1)>]
    [<Command("twitch", "Searches twitch for a stream", "twitch <search>")>]
    let twitch (ctx : CommandContext) = 
        tryPlay ctx (fun ctx -> async {
            let search = HttpUtility.HtmlEncode(ctx.input)
            let json = awaitResult (HttpClient.GetAsync("https://api.twitch.tv/kraken/search/streams?query=" + search, ctx.logger, null, fun req ->
                req.Accept <- "application/vnd.twitchtv.v5+json"
                req.Headers.["Client-ID"] <- Config.Instance.Keys.TwitchKey
            ))
            let twitchObj = JsonPayload.Deserialize<TwitchObj>(json, ctx.logger)
            let streamUrls = twitchObj.streams |> Seq.map (fun stream -> stream.channel.url) |> Seq.toList
            let len = streamUrls.Length
            return
                if len > 0 then
                    let paginator = ctx.serviceManager.GetService<IPaginatorSenderService>("Paginator")
                    [ awaitResult (paginator.SendPlayerPaginator(ctx.message, streamUrls, fun streamUrl ->
                        let page = streamUrls |> Seq.tryFindIndex (fun url -> url.Equals(streamUrl))
                        sprintf "%s #%d out of %d results for \"%s\"\n%s" ctx.authorMention (page.Value + 1) len ctx.arguments.[0] streamUrl 
                    )) ]
                else
                    [ ctx.sendWarn None "Could not find any streams" ]
        })