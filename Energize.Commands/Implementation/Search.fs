namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Search")>]
module Search =
    open System
    open Energize.Commands.AsyncHelper
    open Energize.Toolkit
    open System.Text
    open Energize.Commands.Context
    open YoutubeSearch
    open Energize.Interfaces.Services

    type WordObj = { example: string; definition : string; permalink : string; thumbs_up : int; thumbs_down: int }
    type UrbanObj = { list : WordObj list }
    [<NsfwCommand>]
    [<CommandParameters(1)>]
    [<Command("urban", "Searches urban for a definition", "urban <term>")>]
    let urban (ctx : CommandContext) = async {
        let search = ctx.arguments.[0]
        let json = awaitResult (HttpClient.Fetch("http://api.urbandictionary.com/v0/define?term=" + search, ctx.logger))
        let urbanObj = JsonPayload.Deserialize<UrbanObj>(json, ctx.logger)
        if urbanObj.list |> Seq.isEmpty then
            ctx.sendWarn None "Could not find anything"
        else
            let paginator = ctx.serviceManager.GetService<IPaginatorSenderService>("Paginator");
            await (paginator.SendPaginator(ctx.message, ctx.commandName, urbanObj.list, fun word -> 
                let hasExample = not (String.IsNullOrWhiteSpace word.example)
                let definition =
                    if word.definition |> String.length > 300 then 
                        word.definition.Remove(300) + "..." 
                    else 
                        word.definition
                let builder = StringBuilder()
                let example = if hasExample then "\n**EXAMPLE:**\n" + word.example else String.Empty
                builder
                    .Append(sprintf "**%s**\n\n" word.permalink)
                    .Append(definition + "\n")
                    .Append(example + "\n")
                    .Append(sprintf "\n👍 x%d\t👎 x%d" word.thumbs_up word.thumbs_down)
                    .ToString()
            ))
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
        if videos.Count > 0 then
            let paginator = ctx.serviceManager.GetService<IPaginatorSenderService>("Paginator")
            await (paginator.SendPaginatorRaw(ctx.message, videos, fun vid ->
                let page = videos |> Seq.tryFindIndex (fun v -> v.Url.Equals(vid.Url))
                sprintf "%s #%d out of %d results for \"%s\"\n%s" ctx.authorMention (page.Value + 1) videos.Count ctx.arguments.[0] vid.Url 
            ))
        else
            ctx.sendWarn None "Could not find any videos matching"
    }