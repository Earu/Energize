namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Search")>]
module Search =
    open System
    open Energize.Commands.AsyncHelper
    open Energize.Toolkit
    open Energize.Commands.Context
    open YoutubeSearch
    open Discord
    open Energize.Interfaces.Services.Senders

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

    [<CommandParameters(1)>]
    [<Command("yt", "Searches youtube for a video", "yt <search>")>]
    let yt (ctx : CommandContext) = async {
        let items = VideoSearch()
        let videos =
            try
                items.SearchQuery(ctx.arguments.[0], 1)
            with _ ->
                Collections.Generic.List()
        return 
            if videos.Count > 0 then
                let paginator = ctx.serviceManager.GetService<IPaginatorSenderService>("Paginator")
                [ awaitResult (paginator.SendPaginatorRaw(ctx.message, videos, fun vid ->
                    let page = videos |> Seq.tryFindIndex (fun v -> v.Url.Equals(vid.Url))
                    sprintf "%s #%d out of %d results for \"%s\"\n%s" ctx.authorMention (page.Value + 1) videos.Count ctx.arguments.[0] vid.Url 
                )) ]
            else
                [ ctx.sendWarn None "Could not find any videos matching" ]
    }