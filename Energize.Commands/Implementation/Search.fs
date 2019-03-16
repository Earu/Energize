namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Search")>]
module Search =
    open System
    open Energize.Commands.AsyncHelper
    open Energize.Toolkit
    open Energize.Commands.Context
    open Discord
    open Energize.Interfaces.Services.Senders
    open Energize.Interfaces.Services.Listeners
    open Victoria.Entities

    type WordObj = { example: string; definition : string; permalink : string; thumbs_up : int; thumbs_down: int }
    type UrbanObj = { list : WordObj list }
    [<NsfwCommand>]
    [<CommandParameters(1)>]
    [<Command("urban", "Searches urban for a definition", "urban <term>")>]
    let urban (ctx : CommandContext) = async {
        let search = ctx.arguments.[0]
        let json = awaitResult (HttpClient.GetAsync("http://api.urbandictionary.com/v0/define?term=" + search, ctx.logger))
        let urbanObj = JsonPayload.Deserialize<UrbanObj>(json, ctx.logger)
        return
            if urbanObj.list |> Seq.isEmpty then
                [ ctx.sendWarn None "Could not find anything" ]
            else
                let paginator = ctx.serviceManager.GetService<IPaginatorSenderService>("Paginator");
                [ awaitResult (paginator.SendPaginator(ctx.message, ctx.commandName, urbanObj.list, Action<WordObj, EmbedBuilder>(fun word builder -> 
                    let hasExample = not (String.IsNullOrWhiteSpace word.example)
                    let definition =
                        if word.definition |> String.length > 300 then 
                            word.definition.Remove(300) + "..." 
                        else 
                            word.definition
                
                    let fields = [
                        ctx.embedField "Definition" definition true
                        ctx.embedField "Example" (if hasExample then word.example else " - ") false
                        ctx.embedField "👍 Up-Votes" word.thumbs_up true
                        ctx.embedField "👎 Down-Votes" word.thumbs_down true
                    ]
                    builder.WithFields(fields) |> ignore
                ))) ]
    }

    let private baseSongSearch (ctx : CommandContext) (result : SearchResult) = async {
        let len = result.Tracks |> Seq.length
        return 
            if len > 0 then
                let paginator = ctx.serviceManager.GetService<IPaginatorSenderService>("Paginator")
                [ awaitResult (paginator.SendPaginatorRaw(ctx.message, result.Tracks, fun track ->
                    let page = result.Tracks |> Seq.tryFindIndex (fun v -> v.Uri.Equals(track.Uri))
                    sprintf "%s #%d out of %d results for \"%s\"\n%s" ctx.authorMention (page.Value + 1) len ctx.arguments.[0] (track.Uri.ToString()) 
                )) ]
            else
                [ ctx.sendWarn None "Could not find any songs" ]
    }

    [<CommandParameters(1)>]
    [<Command("yt", "Searches youtube for a video", "yt <search>")>]
    let youtube (ctx : CommandContext) = 
        let music = ctx.serviceManager.GetService<IMusicPlayerService>("Music")
        let res = awaitResult (music.LavaRestClient.SearchYouTubeAsync(ctx.arguments.[0]))
        baseSongSearch ctx res

    [<CommandParameters(1)>]
    [<Command("sc", "Searches soundcloud for a song", "sc <search>")>]
    let soundcloud (ctx : CommandContext) =
        let music = ctx.serviceManager.GetService<IMusicPlayerService>("Music")
        let res = awaitResult (music.LavaRestClient.SearchSoundcloudAsync(ctx.arguments.[0]))
        baseSongSearch ctx res