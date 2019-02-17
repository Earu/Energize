namespace Energize.Commands.Implementation

module Util =
    open Energize.Commands.CommandHandler
    open Energize.Commands.AsyncHelper

    let private moduleName = "Utils"

    let private ping : Command = 
        {
            name = "ping"
            callback = 
                (fun ctx -> async {
                    let timestamp = ctx.message.Timestamp
                    let diff = timestamp.Millisecond / 10
                    awaitResult 
                        (ctx.messageSender.Good(ctx.message, "Pong!", sprintf ":alarm_clock: Discord: %dms\n:clock1: Bot: %dms" diff ctx.client.Latency))
                        |> ignore
                })
            isLoaded = true
            usage = "ping <nothing>"
            help = "Pings the bot"
            moduleName = moduleName
        }

    registerCmd ping
  
