namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Search")>]
module Search =
    open System
    open Energize.Commands.AsyncHelper
    open Energize.Toolkit
    open System.Text
    open Energize.Commands.Context
    
    type WordObj = { example: string; definition : string; permalink : string; thumbs_up : int; thumbs_down: int }
    type UrbanObj = { list : WordObj list }
    [<NsfwCommand>]
    [<CommandParameters(1)>]
    [<Command("urban", "Searches urban for a definition", "urban <term>,<pagenumber|nothing>")>]
    let urban (ctx : CommandContext) = async {
        let pageNum = 
            if ctx.arguments |> List.length > 1 then 
                if String.IsNullOrWhiteSpace ctx.arguments.[1] then 0 else (int ctx.arguments.[1]) - 1
            else 
                0
        let search = ctx.arguments.[0]
        let json = awaitResult (HttpClient.Fetch("http://api.urbandictionary.com/v0/define?term=" + search, ctx.logger))
        let urbanObj = JsonPayload.Deserialize<UrbanObj>(json, ctx.logger)
        if urbanObj.list |> Seq.isEmpty then
            ctx.sendWarn None "Could not find anything"
        else
            let page = 
                match urbanObj.list |> Seq.length with
                | _ when pageNum < 0 -> 0
                | n when pageNum > n - 1 -> n - 1
                | _ -> pageNum
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
    [<Command("yt", "Searches youtube for a video", "yt <search>")>]
    let yt (ctx : CommandContext) = async {
        () //impl
    }