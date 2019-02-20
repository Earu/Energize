namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Fun")>]
module Fun =
    open System
    open Energize.Commands.Context
    open Energize.Toolkit
    open Energize.Commands
    open AsyncHelper
    open UserHelper
    open System.Text
    open System.Text.RegularExpressions
    open Energize.Interfaces.Services
    open HtmlAgilityPack

    [<CommandParameters(1)>]
    [<Command("ascii", "Turns a text/sentence into ascii art", "ascii <sentence>")>]
    let ascii (ctx : CommandContext) = async {
        let body = awaitResult (HttpClient.Fetch("http://artii.herokuapp.com/make?text=" + ctx.input, ctx.logger))
        if body |> String.length > 2000 then
            ctx.messageSender.Danger(ctx.message, ctx.commandName, "The word or sentence you provided is too long!")
        else
            ctx.messageSender.SendRaw(ctx.message, "```\n" + body + "\n```")
        |> awaitIgnore
    }

    [<Command("describe", "Generates a user description", "describe <user|nothing>")>]
    let describe (ctx : CommandContext) = async {
        let adjs = StaticData.ADJECTIVES
        let nouns = StaticData.NOUNS
        let user = 
            if ctx.hasArguments then 
                findUser ctx ctx.input true 
            else
                Some ctx.message.Author
        
        match user with
        | Some u -> 
            let times = ctx.random.Next(1, 4)
            let builder = StringBuilder()
            for _ in 0..times do
                if ctx.random.Next(0,100) >= 50 then
                    builder
                        .Append(adjs.[ctx.random.Next(0, adjs.Length - 1)].ToLower() + " ")
                        .Append(nouns.[ctx.random.Next(0, nouns.Length - 1)].ToLower())
                        .Append(" of the ")
                        .Append(nouns.[ctx.random.Next(0, nouns.Length - 1)].ToLower())
                        .Append(" ")
                        |> ignore
                else
                    builder
                        .Append(adjs.[ctx.random.Next(0, adjs.Length - 1)].ToLower())
                        .Append(" ")
                        |> ignore
            let res = 
                builder
                    .Append(" ")
                    .Append(nouns.[ctx.random.Next(0, nouns.Length - 1)].ToLower())
                    .ToString()
                    .Trim()
            let isVowel = 
                match StaticData.VOWELS |> Seq.tryFind (fun vowel -> res.StartsWith(vowel)) with
                | Some _ -> true
                | None -> false
            let display = u.Mention + " is " + (if isVowel then "an" else "a") + " " + res
            awaitIgnore (ctx.messageSender.Good(ctx.message, ctx.commandName, display))
        | None ->
            awaitIgnore (ctx.messageSender.Warning(ctx.message, ctx.commandName, "No user could be found for your input"))
    }

    [<CommandParameters(1)>]
    [<Command("letters", "Turns your sentence into emojis", "letters <sentence>")>]
    let letters (ctx : CommandContext) = async {
        let indicator = ":regional_indicator_"
        let builder = StringBuilder()
        for i in 0..ctx.input.Length do
            let char = ctx.input.[i].ToString().ToLower()
            if Regex.IsMatch(char, "[A-Za-z]") then
                builder.Append(indicator + char + ": ") |> ignore
            else if Regex.IsMatch(char, "\s") then
                builder.Append('\t') |> ignore

        let res = builder.ToString()
        let len = res |> String.length
        match len with
        | 0 ->
            awaitIgnore (ctx.messageSender.Warning(ctx.message, ctx.commandName, "Your input did not contain any letter"))
        | n when n > 2000 ->
            awaitIgnore (ctx.messageSender.Warning(ctx.message, ctx.commandName, "The output was too long to be displayed"))
        | _ -> 
            awaitIgnore (ctx.messageSender.Good(ctx.message, ctx.commandName, res))
    }

    [<CommandParameters(1)>]
    [<Command("8b", "Answers a question positively or negatively", "8b <question>")>]
    let eightBall (ctx : CommandContext) = async {
        let answers = StaticData.EIGHT_BALL_ANSWERS
        let answer = answers.[ctx.random.Next(0, answers.Length - 1)]

        awaitIgnore (ctx.messageSender.Good(ctx.message, ctx.commandName, answer))
    }

    [<CommandParameters(2)>]
    [<Command("pick", "Makes a choice for you", "pick <choice>,<choice>,<choice|nothing>,etc...")>]
    let pick (ctx : CommandContext) = async {
        let answers = StaticData.PICK_ANSWERS
        let choice = ctx.arguments.[ctx.random.Next(0, ctx.arguments.Length - 1)].Trim()
        let answer = answers.[ctx.random.Next(0, answers.Length - 1)].Replace("<answer>", choice)

        awaitIgnore (ctx.messageSender.Good(ctx.message, ctx.commandName, answer))
    }

    [<Command("m", "Generates a human-like sentence based on input", "m <input|nothing>")>]
    let markov (ctx : CommandContext) = async {
        let markov = ctx.serviceManager.GetService<IMarkovService>("Markov")
        let result = markov.Generate(ctx.input)
        if String.IsNullOrWhiteSpace result then
            awaitIgnore (ctx.messageSender.Warning(ctx.message, "markov", "Nothing was generated ... ?!"))
        else
            let display = Regex.Replace(result, "\\s\\s", " ")
            awaitIgnore (ctx.messageSender.Good(ctx.message, "markov", display))
    }

    type FactObj = { value : string }
    [<Command("chuck", "Random chuck norris fact", "chuck <nothing>")>]
    let chuck (ctx : CommandContext) = async {
        let endpoint = "https://api.chucknorris.io/jokes/random"
        let json = awaitResult (HttpClient.Fetch(endpoint, ctx.logger))
        let fact = JsonPayload.Deserialize<FactObj>(json, ctx.logger)

        awaitIgnore (ctx.messageSender.Good(ctx.message, ctx.commandName, fact.value))
    }

    [<Command("gname", "Generates a random gAmER name", "gname <nothing>")>]
    let gname (ctx : CommandContext) = async {
        let builder = StringBuilder()
        let adjs = StaticData.ADJECTIVES
        let nouns = StaticData.NOUNS
        builder
            .Append(adjs.[ctx.random.Next(0, adjs.Length - 1)].ToLower())
            .Append("_")
            .Append(nouns.[ctx.random.Next(0, nouns.Length - 1)].ToLower())
            |> ignore
        
        if ctx.random.Next(1,100) < 75 then
            builder.Append(ctx.random.Next(1,1000)) |> ignore

        awaitIgnore (ctx.messageSender.Good(ctx.message, ctx.commandName, builder.ToString().Replace("-", "").ToLower()))
    }

    [<Command("oldswear", "Insults from another era", "oldswear <nothing>")>]
    let oldSwear (ctx : CommandContext) = async {
        let html = awaitResult (HttpClient.Fetch("http://www.pangloss.com/seidel/Shaker/", ctx.logger))
        let doc = HtmlDocument()
        doc.LoadHtml(html)
        let node = doc.DocumentNode.SelectNodes("//font") |> Seq.tryHead

        match node with
        | Some n ->
            ctx.messageSender.Good(ctx.message, ctx.commandName, n.InnerText)
        | None ->
            ctx.messageSender.Warning(ctx.message, ctx.commandName, "The oldswear source is down!")
        |> awaitIgnore
    }