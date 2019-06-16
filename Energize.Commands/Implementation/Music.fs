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
open System

[<CommandModule("Music")>]
module Voice =
    let private ytIdRegex = Regex(@"(?:youtube(?:-nocookie)?\.com\/(?:[^\/\n\s]+\/\S+\/|(?:v|e(?:mbed)?)\/|\S*?[?&]v=)|youtu\.be\/)([a-zA-Z0-9_-]{11})\W", RegexOptions.Compiled ||| RegexOptions.IgnoreCase)
    let private spotifyRegex = Regex(@"https?:\/\/open\.spotify\.com\/track\/([^\/\s]+)", RegexOptions.Compiled ||| RegexOptions.IgnoreCase)

    let private musicAction (ctx : CommandContext) (cb : IMusicPlayerService -> IVoiceChannel -> IGuildUser -> IUserMessage list) =
        let music = ctx.serviceManager.GetService<IMusicPlayerService>("Music")
        let guser = ctx.message.Author :?> IGuildUser
        match guser.VoiceChannel with
        | null -> [ ctx.sendWarn None "Not in a voice channel" ]
        | vc -> cb music vc guser

    let private handleSearchResult (music : IMusicPlayerService) (ctx : CommandContext) (res : SearchResult) (vc : IVoiceChannel) (isRadio : bool) =
        match res.LoadType with
        | LoadType.LoadFailed ->
            [ ctx.sendWarn None "Could not load the specified track" ]
        | LoadType.NoMatches ->
            [ ctx.sendWarn None "Could not find any matches for the specified track" ]
        | LoadType.PlaylistLoaded ->
            let playlistName = res.PlaylistInfo.Name
            let textChan = ctx.message.Channel :?> ITextChannel
            awaitResult (music.AddPlaylistAsync(vc, textChan, playlistName, res.Tracks)) |> Seq.toList
        | _ ->
            let tracks = res.Tracks |> Seq.toList 
            if tracks.Length > 0 then
                let tr = tracks.[0]
                let textChan = ctx.message.Channel :?> ITextChannel
                if isRadio then
                    [ awaitResult (music.PlayRadioAsync(vc, textChan, tr)) ]
                else
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
        if url.Contains("youtu") then
            let ytMatch = ytIdRegex.Match(url)
            if ytMatch.Success then
                let ytIdentifier = ytMatch.Groups.[1].Value
                sprintf "https://www.youtube.com/watch?v=%s" ytIdentifier
            else
                url
        else
            url

    let private spotifyToYtUrl (ctx : CommandContext) (url : string) =
        if url.Contains("spotify") then
            let spotifyMatch = spotifyRegex.Match(url)
            if spotifyMatch.Success then
                let id = spotifyMatch.Groups.[1].Value
                let music = ctx.serviceManager.GetService<IMusicPlayerService>("Music")
                let track = awaitResult (music.ConvertSpotifyTrackToYoutubeAsync(id))
                track.Uri.AbsoluteUri
            else
                url
        else
            url

    let private playUrl (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            let input = (spotifyToYtUrl ctx ctx.input) |> sanitizeYtUrl
            let res = awaitResult (music.LavaRestClient.SearchTracksAsync(input))
            handleSearchResult music ctx res vc false
        )
    }

    let private playFile (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            let enumerator = ctx.message.Attachments.GetEnumerator()
            if enumerator.MoveNext() then
                let attachment = enumerator.Current :?> Attachment
                if attachment.IsPlayableAttachment() then
                    let res = awaitResult (music.LavaRestClient.SearchTracksAsync(attachment.Url))
                    handleSearchResult music ctx res vc false
                else
                    [ ctx.sendWarn None "The given file format is not supported"]
            else
                [ ctx.sendWarn None "Could not read the given file" ]
        )
    }

    let private tryPlay (ctx : CommandContext) (cb : CommandContext -> Async<IUserMessage list>) =
        match ctx with
        | _ when ctx.message.Attachments.Count > 0 -> playFile ctx
        | _ when ctx.arguments.Length > 0 && HttpClient.IsURL(ctx.input) -> playUrl ctx
        | _ -> cb ctx
            
    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<Command("play", "Plays a track/stream from youtube, from a link or from a file", "play <song name|url|FILE>")>]
    let play (ctx : CommandContext) = 
        tryPlay ctx (fun ctx -> async {
            return musicAction ctx (fun music vc _ ->   
                if ctx.arguments.Length > 0 then
                    let res = awaitResult (music.LavaRestClient.SearchYouTubeAsync(ctx.input))
                    handleSearchResult music ctx res vc false
                else
                    [ ctx.sendWarn None "Expected a song name, a link (url) or a file" ]
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
    [<Command("stop", "Stops the current track and empties the queue", "stop <nothing>")>]
    let stop (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ -> 
            await (music.StopTrackAsync(vc, ctx.message.Channel :?> ITextChannel))
            [ ctx.sendOK None "Stopped the music player" ]
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
    [<Command("autoplay", "Sets the music player in youtube autoplay mode", "autoplay <nothing>")>]
    let autoplay (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            let autoplaying = awaitResult (music.AutoplayTrackAsync(vc, ctx.message.Channel :?> ITextChannel))
            if autoplaying then
                [ ctx.sendOK None "Youtube autoplay enabled" ]
            else
                [ ctx.sendOK None "Youtube autoplay disabled" ]
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
                [ ctx.sendOK None (sprintf "Setting the volume to %d" (Math.Clamp(vol, 0 , 200))) ]
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
                    sprintf "%s #%d out of %d results for `%s`\n%s" ctx.authorMention (page.Value + 1) len ctx.arguments.[0] (track.Uri.ToString()) 
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
                        let page = streamUrls |> List.tryFindIndex (fun url -> url.Equals(streamUrl))
                        sprintf "%s #%d out of %d results for `%s`\n%s" ctx.authorMention (page.Value + 1) len ctx.arguments.[0] streamUrl 
                    )) ]
                else
                    [ ctx.sendWarn None "Could not find any streams" ]
        })

    [<CommandParameters(1)>]
    [<Command("spotify", "Searches spotify for a song", "spotify <search>")>]
    let spotify (ctx : CommandContext) = 
        tryPlay ctx (fun ctx -> async {
            let music = ctx.serviceManager.GetService<IMusicPlayerService>("Music")
            let songItems = awaitResult (music.SearchSpotifyAsync(ctx.input)) |> Seq.toList
            let len = songItems.Length
            return
                if len > 0 then
                    let paginator = ctx.serviceManager.GetService<IPaginatorSenderService>("Paginator")
                    [ awaitResult (paginator.SendPlayerPaginator(ctx.message, songItems, fun songItem ->
                        let page = songItems |> List.tryFindIndex (fun url -> url.Equals(songItem))
                        sprintf "%s #%d out of %d results for `%s`\n%s" ctx.authorMention (page.Value + 1) len ctx.arguments.[0] songItem.DisplayURL
                    )) ]
                else
                    [ ctx.sendWarn None "Could not find any songs" ]
        })

    let private toMap dictionary = 
        (dictionary :> seq<_>)
        |> Seq.map (|KeyValue|)
        |> Map.ofSeq

    [<CommandParameters(1)>]
    [<Command("radio", "Adds a radio stream of the specified genre to the track queue", "radio <genre>")>]
    let radio (ctx : CommandContext) = async {
        return musicAction ctx (fun music vc _ ->
            let genre = ctx.arguments.[0].ToLower().Trim()
            let radios = toMap StaticData.Instance.RadioSources |> Map.toList
            let radioOpt = radios |> List.tryFind (fun (radioGenre, _) -> genre.Equals(radioGenre))
            match radioOpt with
            | Some (_, url) -> 
                let searchResult = awaitResult (music.LavaRestClient.SearchTracksAsync(url))
                handleSearchResult music ctx searchResult vc true
            | None ->
                let genres = radios |> List.map (fun (radioGenre, _) -> sprintf "`%s`" radioGenre)
                [ ctx.sendWarn None (sprintf "Currently available radio genres are:\n%s" (String.Join(',', genres))) ]
        )
    }