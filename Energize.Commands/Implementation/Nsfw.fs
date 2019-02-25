namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("NSFW")>]
module Nsfw =
    open Energize.Commands.Context
    open Energize.Commands.AsyncHelper
    open Energize.Toolkit
    open Discord
    open System.Xml

    let private buildNsfwEmbed (ctx : CommandContext) (pic : string) (url : string) = 
        let builder = EmbedBuilder()
        ctx.messageSender.BuilderWithAuthor(ctx.message,builder)
        builder
            .WithColor(ctx.messageSender.ColorGood)
            .WithImageUrl(pic)
            .WithFooter(ctx.commandName)
            .WithDescription(sprintf "\n[**CHECK ON %s**](%s)" (ctx.commandName.ToUpper()) url)
            .Build()

    type E621Obj = { sample_url : string; id : string }
    [<NsfwCommand>]
    [<CommandParameters(1)>]
    [<Command("e621", "Searches e621", "e621 <tag|search>")>]
    let e621 (ctx : CommandContext) = async {
        let endpoint = "https://e621.net/post/index.json?tags=" + ctx.input
        let json = awaitResult (HttpClient.Fetch(endpoint, ctx.logger))
        let e621Objs = JsonPayload.Deserialize<E621Obj list>(json, ctx.logger)
        if e621Objs |> List.isEmpty then
            ctx.sendWarn None "Nothing was found"
        else
            let e621Obj = e621Objs.[ctx.random.Next(0,e621Objs.Length)]
            let embed = buildNsfwEmbed ctx e621Obj.sample_url (sprintf "https://e621.net/post/show/%s/" e621Obj.id)
            awaitIgnore (ctx.messageSender.Send(ctx.message, embed))
    }

    let private getDApiResult (ctx : CommandContext) (uri : string) = 
        let endpoint = 
            sprintf "http://%s/index.php?page=dapi&s=post&q=index&tags=%s" uri ctx.input
        let xml = awaitResult (HttpClient.Fetch(endpoint, ctx.logger))
        let doc = XmlDocument()
        doc.LoadXml(xml)
        let nodes = doc.SelectNodes("//post")
        if nodes.Count < 1 then
            None
        else
            let node = nodes.[ctx.random.Next(0, nodes.Count)]
            let url = 
                let n = node.SelectSingleNode("@file_url")
                if n.Value.StartsWith("//") then
                    sprintf "http:%s" n.Value
                else 
                    n.Value
            let id = node.SelectSingleNode("@id").Value
            let page = sprintf "http://%s/index.php?page=post&s=view&id=%s" uri id
            Some (url, page)

    let private callDApiCmd (ctx : CommandContext) (uri : string) = async {
        let result = getDApiResult ctx uri
        match result with
        | Some (url, page) ->
            let embed = buildNsfwEmbed ctx url page
            awaitIgnore (ctx.messageSender.Send(ctx.message, embed))
        | None ->
            ctx.sendWarn None "Nothing was found"
    }

    [<NsfwCommand>]
    [<CommandParameters(1)>]
    [<Command("furrybooru", "Searches furrybooru", "furrybooru <tags|search>")>]
    let furryBooru (ctx : CommandContext) = 
        callDApiCmd ctx "furry.booru.org"

    [<NsfwCommand>]
    [<CommandParameters(1)>]
    [<Command("r34", "Searches r34", "r34 <tags|search>")>]
    let r34 (ctx : CommandContext) =
        callDApiCmd ctx "rule34.xxx"

    [<NsfwCommand>]
    [<CommandParameters(1)>]
    [<Command("gelbooru", "Searches gelbooru", "gelbooru <tags|search>")>]
    let gelBooru (ctx : CommandContext) =
        callDApiCmd ctx "gelbooru.com"