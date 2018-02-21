namespace Flamerize

open System
open System.Threading.Tasks
open DSharpPlus
open DSharpPlus.CommandsNext
open DSharpPlus.Entities
open DSharpPlus.EventArgs

module Initializer =
    let DiscordConfig = DiscordConfiguration()
    DiscordConfig.set_AutoReconnect true
    DiscordConfig.set_LogLevel LogLevel.Debug
    DiscordConfig.set_Token "<YOUR TOKEN HERE>"
    DiscordConfig.set_TokenType TokenType.Bot

    let Discord = new DiscordClient(DiscordConfig)

    let CommandConfig = CommandsNextConfiguration()
    CommandConfig.set_StringPrefix "&"
    CommandConfig.set_SelfBot false
    CommandConfig.set_CaseSensitive false
    CommandConfig.set_EnableMentionPrefix true
    CommandConfig.set_IgnoreExtraArguments true
    CommandConfig.set_EnableDms false

    let Commands = Discord.UseCommandsNext(CommandConfig)
    Commands.RegisterCommands<Commands>()

    let OnLog(_: obj) (e: DebugLogMessageEventArgs) =
        let color = match e.Level with
                    | LogLevel.Debug -> ConsoleColor.Cyan
                    | LogLevel.Warning -> ConsoleColor.Yellow
                    | LogLevel.Info -> ConsoleColor.Blue
                    | LogLevel.Error -> ConsoleColor.Magenta
                    | LogLevel.Critical -> ConsoleColor.Red
                    | _ -> ConsoleColor.White

        Console.Write (sprintf "%s - [" (e.Timestamp.ToString("HH:mm")))
        Console.ForegroundColor <- color
        Console.Write (sprintf "%s" e.Application)
        Console.ForegroundColor <- ConsoleColor.White
        Console.Write (sprintf "] >> %s\n" e.Message)
    
    let OnError(e: Exception) =
        let err = e.ToString()
        Console.Write (sprintf "%s - [" (DateTime.Now.ToString("HH:mm")))
        Console.ForegroundColor <- ConsoleColor.Red
        Console.Write (sprintf "Error")
        Console.ForegroundColor <- ConsoleColor.White
        Console.Write (sprintf "] >> %s\n" err)

    Discord.DebugLogger.LogMessageReceived.AddHandler(EventHandler<DebugLogMessageEventArgs>(OnLog))

    let OnCommandError (e: CommandErrorEventArgs) =
        let embed =  DiscordEmbedBuilder()
        embed.Color <- DiscordColor(0xe24444)
        embed.Author <- DiscordEmbedBuilder.EmbedAuthor()
        embed.Author.Name <- "Internal error"
        embed.Footer <- DiscordEmbedBuilder.EmbedFooter()
        embed.Footer.Text <- sprintf "%s#%s" e.Context.User.Username e.Context.User.Discriminator
        embed.Footer.IconUrl <- e.Context.User.AvatarUrl
        embed.Timestamp <- new Nullable<DateTimeOffset>(DateTimeOffset.UtcNow)
        embed.Description <- sprintf "```cs\n%s: %s\n```" (e.Exception.GetType().ToString()) e.Exception.Message

        OnError e.Exception
        e.Context.Channel.SendMessageAsync("", false, embed.Build()) :> Task

    Commands.add_CommandErrored(new AsyncEventHandler<CommandErrorEventArgs>(OnCommandError))

    let OnReady (_: ReadyEventArgs) =
        let game = DiscordGame("a holy fire")
        game.StreamType <- GameStreamType.Twitch
        game.Url <- "https://www.twitch.tv/earu_rawr"
        Discord.UpdateStatusAsync(game) |> Async.AwaitTask |> Async.RunSynchronously
        Task.CompletedTask

    Discord.add_Ready(new AsyncEventHandler<ReadyEventArgs>(OnReady))

