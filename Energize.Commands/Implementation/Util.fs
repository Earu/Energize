namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Utils")>]
module Util =
    open Energize.Commands.AsyncHelper
    open System.Diagnostics
    open Energize.Commands.Context

    [<Command("ping", "ping <nothing>", "Pings the bot")>]
    let ping (ctx : CommandContext) = async {
        let timestamp = ctx.message.Timestamp
        let diff = timestamp.Millisecond / 10
        awaitResult 
            (ctx.messageSender.Good(ctx.message, "Pong!", sprintf ":alarm_clock: Discord: %dms\n:clock1: Bot: %dms" diff ctx.client.Latency))
            |> ignore
    }

    [<Command("mem", "mem <nothing>", "Gets the current memory usage")>]
    let mem (ctx : CommandContext) = async {
        let proc = Process.GetCurrentProcess()
        let mbused = int proc.WorkingSet64 / 1024 / 1024
        awaitResult 
            (ctx.messageSender.Good(ctx.message, ctx.commandName, sprintf "Currently using %dMB of memory" mbused))
            |> ignore
    }    