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
    
    let private getProperPage (input : string) (min : int) (max : int) : int =
        let unclamped = if String.IsNullOrWhiteSpace input then 0 else (int input) - 1
        match unclamped with
        | n when n < min -> min
        | n when n > max -> max
        | n -> n

    type WordObj = { example: string; definition : string; permalink : string; thumbs_up : int; thumbs_down: int }
    type UrbanObj = { list : WordObj list }
    [<NsfwCommand>]
    [<CommandParameters(1)>]
    [<Command("urban", "Searches urban for a definition", "urban <term>,<pagenumber|nothing>")>]
    let urban (ctx : CommandContext) = async {
        let search = ctx.arguments.[0]
        let json = awaitResult (HttpClient.Fetch("http://api.urbandictionary.com/v0/define?term=" + search, ctx.logger))
        let urbanObj = JsonPayload.Deserialize<UrbanObj>(json, ctx.logger)
        if urbanObj.list |> Seq.isEmpty then
            ctx.sendWarn None "Could not find anything"
        else
            let page = 
                if ctx.arguments |> List.length > 1 then 
                    getProperPage ctx.arguments.[1] 0 (urbanObj.list.Length - 1) 
                else 
                    0
            let wordObj = urbanObj.list.[page]
            let hasExample = not (String.IsNullOrWhiteSpace wordObj.example)
            let definition =
                if wordObj.definition |> String.length > 300 then 
                    wordObj.definition.Remove(300) + "..." 
                else 
                    wordObj.definition
            let builder = StringBuilder()
            let example = if hasExample then "\n**EXAMPLE:**\n" + wordObj.example else String.Empty
            builder
                .Append(sprintf "**%s**\n\n" wordObj.permalink)
                .Append(definition + "\n")
                .Append(example + "\n")
                .Append(sprintf "\n👍 x%d\t👎 x%d" wordObj.thumbs_up wordObj.thumbs_down)
                |> ignore
            let head = sprintf "Definition %d/%d" (page + 1) (urbanObj.list |> Seq.length)
            awaitIgnore (ctx.messageSender.Good(ctx.message, head, builder.ToString())) 
    }

    [<CommandParameters(1)>]
    [<Command("yt", "Searches youtube for a video", "yt <search>,<pagenumber|nothing>")>]
    let yt (ctx : CommandContext) = async {
        let items = VideoSearch()
        let videos =
            try
                items.SearchQuery(ctx.arguments.[0], 1)
            with _ ->
                Collections.Generic.List()
        if videos.Count > 0 then
            let page = 
                if ctx.arguments |> List.length > 1 then 
                    getProperPage ctx.arguments.[1] 0 (videos.Count - 1) 
                else 
                    0
            let vid = videos.[page]
            let display = sprintf "%s #%d out of %d results for \"%s\"\n%s" ctx.authorMention (page + 1) videos.Count ctx.arguments.[0] vid.Url 
            awaitIgnore (ctx.messageSender.SendRaw(ctx.message, display))
        else
            ctx.sendWarn None "Could not find any videos matching"
    }