namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Utils")>]
module Util =
    open Energize.Commands.AsyncHelper
    open System.Diagnostics
    open Energize.Commands.Context
    open System
    open Energize.Interfaces.Services
    open Energize.Toolkit
    open Discord

    [<Command("ping", "ping <nothing>", "Pings the bot")>]
    let ping (ctx : CommandContext) = async {
        let timestamp = ctx.message.Timestamp
        let diff = timestamp.Millisecond / 10
        let res = sprintf "⏰ Discord: %dms\n🕐 Bot: %dms" diff ctx.client.Latency
        awaitIgnore (ctx.messageSender.Good(ctx.message, "Pong!", res))
    }

    [<Command("mem", "mem <nothing>", "Gets the current memory usage")>]
    let mem (ctx : CommandContext) = async {
        let proc = Process.GetCurrentProcess()
        let mbused = proc.WorkingSet64 / 1024L / 1024L
        awaitIgnore (ctx.messageSender.Good(ctx.message, ctx.commandName, sprintf "Currently using %dMB of memory" mbused))
    }    

    [<Command("uptime", "uptime <nothing>", "Gets the current uptime")>]
    let uptime (ctx : CommandContext) = async {
        let diff = (DateTime.Now - Process.GetCurrentProcess().StartTime).Duration();
        let res = sprintf "%dd%dh%dm" diff.Days diff.Hours diff.Minutes
        awaitIgnore (ctx.messageSender.Good(ctx.message, ctx.commandName, "The current instance has been up for " + res))
    }

    [<CommandParameters(1)>]
    [<Command("say", "Makes me say something", "say <sentence>")>]
    let say (ctx : CommandContext) = async {
        awaitIgnore (ctx.messageSender.Good(ctx.message, ctx.commandName, ctx.input))   
    }

    [<CommandParameters(1)>]
    [<Command("l", "Runs your lua code in a sandbox", "l <luastring>")>]
    let lua (ctx : CommandContext) = async {
        let env = ctx.serviceManager.GetService<ILuaService>("Lua")
        let returns : Collections.Generic.List<obj> ref = ref (Collections.Generic.List<obj>())
        let error = ref String.Empty
        if env.Run(ctx.message, ctx.input, returns, error, ctx.logger) then
            let display = String.Join('\t', returns)
            if String.IsNullOrWhiteSpace display then
                awaitIgnore (ctx.messageSender.Good(ctx.message, "lua", "👌 (nil or no value was returned)"))
            else
                if display |> String.length > 2000 then
                    awaitIgnore (ctx.messageSender.Warning(ctx.message, "lua", "Output was too long to be sent"))
                else
                    awaitIgnore (ctx.messageSender.Good(ctx.message, "lua", display))
        else
            awaitIgnore (ctx.messageSender.Danger(ctx.message, "lua", sprintf "```\n%s```" (error.Value.Replace("`",""))))
    }

    [<Command("lr", "Reset the channel's lua environment", "lr <nothing>")>]
    let luaReset (ctx : CommandContext) = async {
        let env = ctx.serviceManager.GetService<ILuaService>("Lua")
        env.Reset(ctx.message.Channel.Id, ctx.logger)
        awaitIgnore (ctx.messageSender.Good(ctx.message, "lua", "Lua environment reset in this channel"))
    }

    [<CommandParameters(1)>]
    [<Command("feedback", "Send feedback to the owner (suggestion, bug, etc...)", "feedback <sentence>")>]
    let feedback (ctx : CommandContext) = async {
        let sender = ctx.serviceManager.GetService<IWebhookSenderService>("Webhook")
        let feedback = ctx.input
        let name = ctx.message.Author.Username
        let avatar = ctx.message.Author.GetAvatarUrl(ImageFormat.Auto)
        let chan = ctx.client.GetChannel(Config.FEEDBACK_CHANNEL_ID)
        let log = 
            if ctx.isPrivate then
                let c = ctx.message.Channel :?> IGuildChannel
                sprintf "%s#%s" c.Guild.Name c.Name
            else
                ctx.message.Author.ToString()

        awaitIgnore (ctx.messageSender.Good(ctx.message, ctx.commandName, "Successfully sent your feedback"))

        let builder = EmbedBuilder()
        builder
            .WithDescription(feedback)
            .WithTimestamp(ctx.message
            .CreatedAt).WithFooter(log)
            |> ignore

        match chan :> IChannel with
        | :? ITextChannel as textChan ->
            awaitIgnore (sender.SendEmbed(textChan, builder.Build(), name, avatar))
        | _ ->
            ctx.logger.Warning("Feedback channel wasnt a text channel?!")
    }