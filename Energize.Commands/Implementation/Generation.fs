namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Generation")>]
module Fun =
    open System
    open Energize.Commands.Context
    open Energize.Essentials
    open Energize.Commands
    open AsyncHelper
    open System.Text.RegularExpressions
    open Discord.WebSocket
    open Energize.Interfaces.Services.Generation
    open Energize.Interfaces.Services.Database
    open Discord

    [<CommandParameters(1)>]
    [<Command("ascii", "Turns a text/sentence into ascii art", "ascii <sentence>")>]
    let ascii (ctx : CommandContext) = async {
        let body = awaitResult (HttpClient.GetAsync("http://artii.herokuapp.com/make?text=" + ctx.input, ctx.logger))
        return 
            if body |> String.length > 2000 then
                [ ctx.sendWarn None "The word or sentence you provided is too long!" ]
            else
                [ ctx.sendRaw ("```\n" + body + "\n```") ]
    }

    [<CommandParameters(1)>]
    [<Command("8b", "Answers a question positively or negatively", "8b <question>")>]
    let eightBall (ctx : CommandContext) = async {
        let answers = StaticData.Instance.EightBallAnswers
        let answer = answers.[ctx.random.Next(0, answers.Length)]
        return [ ctx.sendOK None answer ]
    }

    [<CommandParameters(2)>]
    [<Command("pick", "Makes a choice for you", "pick <choice>,<choice>,<choice|nothing>,etc...")>]
    let pick (ctx : CommandContext) = async {
        let answers = StaticData.Instance.PickAnswers
        let choice = ctx.arguments.[ctx.random.Next(0, ctx.arguments.Length)].Trim()
        let answer = answers.[ctx.random.Next(0, answers.Length)].Replace("<answer>", choice)
        return [ ctx.sendOK None answer ]
    }

    [<CommandParameters(1)>]
    [<Command("m", "Generates a human-like sentence based on input", "m <input>")>]
    let markov (ctx : CommandContext) = async {
        let markov = ctx.serviceManager.GetService<IMarkovService>("Markov")
        let result = markov.Generate(ctx.input)
        return 
            if String.IsNullOrWhiteSpace result then
                [ ctx.sendWarn (Some "markov") "Nothing was generated ... ?!" ]
            else
                let display = Regex.Replace(result, "\\s\\s", " ")
                [ ctx.sendOK (Some "markov") display ]
    }

    [<CommandParameters(2)>]
    [<Command("style", "Sets a typing style for yourself or use one on a sentence", "style <style>,<'toggle'|sentence>")>]
    let textStyle (ctx : CommandContext) = async {
        let styleService = ctx.serviceManager.GetService<ITextStyleService>("TextStyle")
        let styleHelp = sprintf "Styles available:\n`%s`" (String.Join(',', styleService.GetStyles()));
        return 
            match styleService.GetStyles() |> Seq.tryFind (fun s -> s.Equals(ctx.arguments.[0].Trim().ToLower())) with
            | Some style ->
                match ctx.arguments.[1].Trim().ToLower() with
                | "toggle" ->
                    let user = ctx.message.Author :?> SocketGuildUser
                    let db = ctx.serviceManager.GetService<IDatabaseService>("Database")
                    let dbctx = awaitResult (db.GetContext())
                    let dbuser = awaitResult (dbctx.Instance.GetOrCreateUser(user.Id))

                    let wasToggled = dbuser.Style.Equals(style)
                    dbuser.Style <- if wasToggled then "none" else style
                    dbctx.Dispose()
                    if wasToggled then
                        [ ctx.sendOK None "Untoggling style" ]
                    else
                        [ ctx.sendOK None "Toggling style" ]
                | _ -> 
                    let input = String.Join(',', ctx.arguments.[1..])
                    if not (String.IsNullOrWhiteSpace input) then
                        let result = styleService.GetStyleResult(input, style)
                        [ ctx.sendOK None result ]
                    else
                        [ ctx.sendWarn None "The input to transform was empty" ]
            | None ->
                [ ctx.sendWarn None styleHelp ]
    }

    [<CommandParameters(3)>]
    [<Command("minesweeper", "Generates a minesweeper game", "minesweeper <width>,<height>,<mineamount>")>]
    let minesweeper (ctx : CommandContext) = async {
        let width = int ctx.arguments.[0]
        let height = int ctx.arguments.[1]
        let mineAmount = int ctx.arguments.[2]
        return 
            match (width, height) with
            | (w, h) when w > 10 || h > 10 ->
                [ ctx.sendWarn None "Maximum width and height is 10" ]
            | (w, h) when mineAmount > h * w ->
                [ ctx.sendWarn None "Cannot have more mines than squares" ]
            | (w, h) ->
                let minesweeper = ctx.serviceManager.GetService<IMinesweeperService>("Minesweeper")
                let res = minesweeper.Generate(w, h, mineAmount)
                if res.Length > 2000 then
                    [ ctx.sendWarn None "Output is too long to be displayed" ]
                else
                    [ ctx.sendOK None res ]
    }

    [<Command("inspiro", "Quotes from inspirobot", "inspiro <nothing>")>]
    let inspiro (ctx : CommandContext) = async {
        let endpoint = "http://inspirobot.me/api?generate=true"
        let url = awaitResult (HttpClient.GetAsync(endpoint, ctx.logger))
        let builder = EmbedBuilder()
        ctx.messageSender.BuilderWithAuthor(ctx.message, builder)
        builder
            .WithColor(ctx.messageSender.ColorGood)
            .WithImageUrl(url)
            .WithFooter(ctx.commandName)
            |> ignore
        return [ ctx.sendEmbed (builder.Build()) ]
    }