namespace Energize.Commands.Implementation

open Energize.Commands.Command
open Energize.Commands.Context
open Energize.Commands.AsyncHelper
open Energize.Essentials
open Discord
open System.Xml
open System
open Energize.Interfaces.Services.Senders
open System.Net
open Energize.Essentials.Helpers

[<CommandModule("NSFW")>]
module Nsfw =
    let private buildNsfwEmbed (builder : EmbedBuilder) (ctx : CommandContext) (pic : string) (url : string) = 
        let uri = Uri(url)
        builder
            .WithAuthorNickname(ctx.message)
            .WithColorType(EmbedColorType.Good)
            .WithImageUrl(pic)
            .WithFooter(ctx.commandName)
            .WithTitle(sprintf "**CHECK ON %s**" (uri.Host.ToUpper()))
            .WithUrl(url)
            |> ignore

    type E621Obj = { sample_url : string; id : string }
    [<CommandConditions(CommandCondition.NsfwOnly)>]
    [<CommandParameters(1)>]
    [<Command("e621", "Searches e621", "e621 <tag|search>")>]
    let e621 (ctx : CommandContext) = async {
        let endpoint = "https://e621.net/post/index.json?tags=" + WebUtility.UrlEncode(ctx.input)
        let json = awaitResult (HttpHelper.GetAsync(endpoint, ctx.logger))
        let mutable e621Objs = []
        return 
            if JsonHelper.TryDeserialize<E621Obj list>(json, ctx.logger, &e621Objs) then
                if e621Objs.IsEmpty then
                    [ ctx.sendWarn None "Nothing was found" ]
                else
                    let paginator = ctx.serviceManager.GetService<IPaginatorSenderService>("Paginator")
                    [ awaitResult (paginator.SendPaginatorAsync(ctx.message, ctx.commandName, e621Objs, Action<E621Obj, EmbedBuilder>(fun obj builder ->
                        buildNsfwEmbed builder ctx obj.sample_url (sprintf "https://e621.net/post/show/%s/" obj.id)
                    ))) ]
            else
                [ ctx.sendWarn None "There was a problem processing the result" ]
    }

    let private getDApiResult (ctx : CommandContext) (uri : string) = 
        let endpoint = 
            sprintf "http://%s/index.php?page=dapi&s=post&q=index&tags=%s" uri (WebUtility.UrlEncode(ctx.input))
        let xml = awaitResult (HttpHelper.GetAsync(endpoint, ctx.logger))
        if String.IsNullOrWhiteSpace xml then
            None
        else
            let doc = XmlDocument()
            doc.LoadXml(xml)
            let nodes = doc.SelectNodes("//post")
            if nodes.Count < 1 then
                None
            else
                let results =
                    nodes 
                    |> Seq.cast<XmlNode>
                    |> Seq.map (fun node -> 
                        let url = 
                            let n = node.SelectSingleNode("@file_url")
                            if n.Value.StartsWith("//") then
                                sprintf "http:%s" n.Value
                            else 
                                n.Value
                        let id = node.SelectSingleNode("@id").Value
                        let page = sprintf "http://%s/index.php?page=post&s=view&id=%s" uri id
                        (url, page)
                    )
                    |> Seq.distinct
                    |> Seq.toList

                Some results

    let private callDApiCmd (ctx : CommandContext) (uri : string) = async {
        let result = getDApiResult ctx uri
        return 
            match result with
            | Some results ->
                let paginator = ctx.serviceManager.GetService<IPaginatorSenderService>("Paginator")
                [ awaitResult (paginator.SendPaginatorAsync(ctx.message, ctx.commandName, results, Action<(string * string), EmbedBuilder>(fun (url, page) builder ->
                     buildNsfwEmbed builder ctx url page
                ))) ]
            | None ->
                [ ctx.sendWarn None "Nothing was found" ]
    }

    [<CommandConditions(CommandCondition.NsfwOnly)>]
    [<CommandParameters(1)>]
    [<Command("furb", "Searches furrybooru", "furb <tags|search>")>]
    let furb (ctx : CommandContext) = 
        callDApiCmd ctx "furry.booru.org"

    [<CommandConditions(CommandCondition.NsfwOnly)>]
    [<CommandParameters(1)>]
    [<Command("r34", "Searches r34", "r34 <tags|search>")>]
    let r34 (ctx : CommandContext) =
        callDApiCmd ctx "rule34.xxx"

    [<CommandConditions(CommandCondition.NsfwOnly)>]
    [<CommandParameters(1)>]
    [<Command("gelb", "Searches gelbooru", "gelb <tags|search>")>]
    let gelb (ctx : CommandContext) =
        callDApiCmd ctx "gelbooru.com"

    [<CommandConditions(CommandCondition.NsfwOnly)>]
    [<CommandParameters(1)>]
    [<Command("xbooru", "Searches xbooru", "xbooru <tags|search>")>]
    let xbooru (ctx : CommandContext) =
        callDApiCmd ctx "xbooru.com"

    [<CommandConditions(CommandCondition.NsfwOnly)>]
    [<CommandParameters(1)>]
    [<Command("realb", "Searches realbooru", "realb <tags|search>")>]
    let realb (ctx : CommandContext) =
        callDApiCmd ctx "realbooru.com"

    [<CommandConditions(CommandCondition.NsfwOnly)>]
    [<CommandParameters(1)>]
    [<Command("safeb", "Searches safebooru", "safeb <tags|search>")>]
    let safeb (ctx : CommandContext) =
        callDApiCmd ctx "safebooru.org"

    let private nlTags = [ 
        "femdom"; "tickle"; "ngif"; "erofeet"; "erok"; "les"; "hololewd"; "lewdk"; "keta";
        "feetg"; "eroyuri"; "kiss"; "kuni"; "tits"; "pussy"; "lewdkemo"; "lewd"; "cum"; "spank";
        "smallboobs"; "fox_girl"; "boobs"; "kemonomimi"; "solog"; "bj"; "yuri"; "trap"; "anal";
        "blowjob"; "holoero"; "neko"; "gasm"; "hentai"; "futanari"; "ero"; "solo"; "waifu"; "pwankg";
        "eron"; "erokemo"; "classic";
    ]
    type NLObj = { url : string }
    [<CommandConditions(CommandCondition.NsfwOnly)>]
    [<Command("nl", "Searches nekos.life with a tag", "nl <tag|nothing>")>]
    let nl (ctx : CommandContext) = async {
        let tag = if ctx.arguments.IsEmpty then "classic" else ctx.arguments.[0]
        return 
            if nlTags |> List.contains tag then
                let json = awaitResult (HttpHelper.GetAsync(sprintf "https://nekos.life/api/v2/img/%s" tag, ctx.logger))
                let mutable result = { url = String.Empty }
                if JsonHelper.TryDeserialize(json, ctx.logger, &result) then
                    let builder = EmbedBuilder()
                    buildNsfwEmbed builder ctx result.url result.url
                    [ ctx.sendEmbed (builder.Build()) ]
                else
                    [ ctx.sendWarn None "There was a problem processing the result" ]
            else
                let tagList = String.Join(',', nlTags |> List.map (fun t -> sprintf "`%s`" t))
                [ ctx.sendWarn None (sprintf "Valid tags are: %s" tagList) ]
    }

    type ObuttsObj = { preview : string }
    [<CommandConditions(CommandCondition.NsfwOnly)>]
    [<Command("ass", "Gets a random ass picture", "ass <nothing>")>]
    let ass (ctx : CommandContext) = async {
        let json = awaitResult (HttpHelper.GetAsync("http://api.obutts.ru/butts/0/1/random", ctx.logger))
        let mutable results : ObuttsObj list = []
        return 
            if JsonHelper.TryDeserialize(json, ctx.logger, &results) then
                if not results.IsEmpty then
                    let builder = EmbedBuilder()
                    let picUrl = sprintf "http://media.obutts.ru/%s" results.[0].preview
                    buildNsfwEmbed builder ctx picUrl picUrl
                    [ ctx.sendEmbed (builder.Build()) ]
                else
                    [ ctx.sendWarn None "Nothing was found" ]
            else
                [ ctx.sendWarn None "There was a problem processing the result" ]
    }