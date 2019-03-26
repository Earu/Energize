namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Utils")>]
module Util =
    open Energize.Commands.AsyncHelper
    open System.Diagnostics
    open Energize.Commands.Context
    open System
    open Energize.Essentials
    open Discord
    open System.Threading.Tasks
    open System.Text
    open Microsoft.Data.Sqlite
    open System.IO
    open Energize.Interfaces.Services.Eval
    open Energize.Interfaces.Services.Senders
    open Energize.Interfaces.Services.Listeners
    open Energize.Commands.UserHelper
    open Discord.WebSocket

    [<Command("ping", "ping <nothing>", "Pings the bot")>]
    let ping (ctx : CommandContext) = async {
        let timestamp = ctx.message.Timestamp
        let diff = timestamp.Millisecond / 10
        let res = sprintf "⏰ Discord: %dms\n🕐 Bot: %dms" diff ctx.client.Latency
        return [ ctx.sendOK None res ]
    }

    [<Command("mem", "mem <nothing>", "Gets the current memory usage")>]
    let mem (ctx : CommandContext) = async {
        let proc = Process.GetCurrentProcess()
        let mbused = proc.WorkingSet64 / 1024L / 1024L
        return [ ctx.sendOK None (sprintf "Currently using %dMB of memory" mbused) ]
    }    

    [<Command("uptime", "uptime <nothing>", "Gets the current uptime")>]
    let uptime (ctx : CommandContext) = async {
        let diff = (DateTime.Now - Process.GetCurrentProcess().StartTime).Duration();
        let res = sprintf "%dd%dh%dm" diff.Days diff.Hours diff.Minutes
        return [ ctx.sendOK None ("The current instance has been up for " + res) ]
    }

    [<CommandParameters(1)>]
    [<Command("l", "Runs your lua code in a sandbox", "l <luastring>")>]
    let lua (ctx : CommandContext) = async {
        let env = ctx.serviceManager.GetService<ILuaService>("Lua")
        let returns : Collections.Generic.List<obj> ref = ref (Collections.Generic.List<obj>())
        let error = ref String.Empty
        return 
            if env.Run(ctx.message, ctx.input, returns, error, ctx.logger) then
                let display = String.Join('\t', returns.contents)
                if String.IsNullOrWhiteSpace display then
                    [ ctx.sendOK (Some "lua") "👌 (nil or no value was returned)" ]
                else
                    if display |> String.length > 2000 then
                        [ ctx.sendWarn (Some "lua") "Output was too long to be sent" ]
                    else
                        [ ctx.sendOK (Some "lua") display ]
            else
                [ ctx.sendBad (Some "lua") (sprintf "```\n%s```" (error.contents.Replace("`",""))) ]
    }

    [<Command("lr", "Reset the channel's lua environment", "lr <nothing>")>]
    let luaReset (ctx : CommandContext) = async {
        let env = ctx.serviceManager.GetService<ILuaService>("Lua")
        env.Reset(ctx.message.Channel.Id, ctx.logger)
        return [ ctx.sendOK (Some "lua") "Lua environment reset in this channel" ]
    }

    [<CommandParameters(1)>]
    [<Command("feedback", "Send feedback to the owner (suggestion, bug, etc...)", "feedback <sentence>")>]
    let feedback (ctx : CommandContext) = async {
        let sender = ctx.serviceManager.GetService<IWebhookSenderService>("Webhook")
        let feedback = ctx.input
        let name = ctx.message.Author.Username
        let avatar = ctx.message.Author.GetAvatarUrl(ImageFormat.Auto)
        let chan = ctx.client.GetChannel(Config.Instance.Discord.FeedbackChannelID)
        let log = 
            if not ctx.isPrivate then
                let c = ctx.message.Channel :?> IGuildChannel
                sprintf "%s#%s" c.Guild.Name c.Name
            else
                ctx.message.Author.ToString()

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

        return [ ctx.sendOK None "Successfully sent your feedback" ]
    }

    [<OwnerCommand>]
    [<CommandParameters(1)>]
    [<Command("to", "Timing out test", "to <seconds>")>]
    let timeOut (ctx : CommandContext) = async {
        let duration = int ctx.input
        await (Task.Delay(duration * 1000))
        return [ ctx.sendOK (Some "Time Out") (sprintf "Timed out during `%d`s" duration) ]
    }

    [<OwnerCommand>]
    [<CommandParameters(1)>]
    [<Command("sql", "Runs an sql statement in the database", "sql <sqlstring>")>]
    let sql (ctx : CommandContext) = async {
        let conn = new SqliteConnection(Config.Instance.DBConnectionString)
        return
            try
                await (conn.OpenAsync())
                let cmd = new SqliteCommand(ctx.input, conn)
                let reader = cmd.ExecuteReader()
                if not (reader.HasRows) then
                    [ ctx.sendWarn None "No data was gathered for the specified statement" ]
                else
                    let builder = StringBuilder()
                    while reader.Read() do
                        let values = 
                            seq { for i in 0 .. reader.FieldCount - 1 -> reader.[i].ToString() }
                            |> Seq.toList
                        builder.Append(sprintf "%s\n%s\n" (String.Join('\t', values)) (String('-', 50))) 
                            |> ignore
                    let res = sprintf "```\n%s\n%s```" (String('-', 50)) (builder.ToString())
                    [ ctx.sendOK None res ]
            with ex ->
                [ ctx.sendBad None ("```\n" + ex.Message.Replace("`", "") + "```") ]
    }

    [<OwnerCommand>]
    [<Command("err", "Throws an error for testing", "err <nothing|message>")>]
    let err (ctx : CommandContext) : Async<IUserMessage list> = async {
        let msg = if ctx.hasArguments then ctx.input else "test"
        raise (Exception(msg))
        return []
    }

    [<OwnerCommand>]
    [<CommandParameters(1)>]
    [<Command("ev", "Evals a C# string", "ev <csharpstring>")>]
    let eval (ctx : CommandContext) = async {
        let evaluator = ctx.serviceManager.GetService<ICSharpEvaluatorService>("Evaluator")
        let res = awaitResult (evaluator.Eval(ctx.input, ctx))
        return
            match res.ToTuple() with
            | (0, out) -> [ ctx.sendBad None out ]
            | (1, out) -> [ ctx.sendOK None out ]
            | (_, out) -> [ ctx.sendWarn None out ]
    }

    [<OwnerCommand>]
    [<Command("restart", "Restarts the bot", "restart <nothing>")>]
    let restart (ctx : CommandContext) : Async<IUserMessage list> = async {
        File.WriteAllText("restartlog.txt", ctx.message.Channel.Id.ToString())
        ctx.sendWarn None "Restarting..." |> ignore
        Process.GetCurrentProcess().Kill()
        return []
    }

    [<CommandParameters(3)>]
    [<Command("vote","Creates a 5 minutes vote with up to 9 choices","vote <description>,<choice>,<choice>,<choice|nothing>,...")>]
    let vote (ctx : CommandContext) = async {
        let votes = ctx.serviceManager.GetService<IVoteSenderService>("Votes")
        let choices = if ctx.arguments.Length > 10 then ctx.arguments.[1..8] else ctx.arguments.[1..]
        let msg = awaitResult (votes.SendVote(ctx.message, ctx.arguments.[0], choices)) 
        return [ msg ]
    }

    [<Command("lavalink", "Gets the lavalink server stats", "lavalink <nothing>")>]
    let lavalink (ctx : CommandContext) = async {
        let music = ctx.serviceManager.GetService<IMusicPlayerService>("Music")
        return
            match music.GetLavalinkStats() with
            | null -> [ ctx.sendWarn None "Stats not available yet" ]
            | stats ->
                let builder = EmbedBuilder()
                ctx.messageSender.BuilderWithAuthor(ctx.message, builder)
                let uptime = sprintf "%dd%dh%dm" stats.Uptime.Days stats.Uptime.Hours stats.Uptime.Minutes
                let fields = [
                    ctx.embedField "CPU Load" (match stats.Cpu with null -> 0.0 | _ -> stats.Cpu.LavalinkLoad) true
                    ctx.embedField "Frames" (match stats.Frames with null -> 0 | _ -> stats.Frames.Sent) true
                    ctx.embedField "Memory(MB)" (match stats.Memory with null -> 0L | _ -> stats.Memory.Used / 1024L / 1024L) true
                    ctx.embedField "Music Players" stats.PlayerCount true
                    ctx.embedField "Uptime" uptime true
                ]

                builder
                    .WithColor(ctx.messageSender.ColorGood)
                    .WithFooter(ctx.commandName)
                    .WithFields(fields)
                    |> ignore
                [ ctx.sendEmbed (builder.Build()) ]
    }

    [<CommandParameters(1)>]
    [<Command("avatar", "Gets the avatar of a user", "avatar <user|userid>")>]
    let avatar (ctx : CommandContext) = async {
        return 
            match findUser ctx ctx.arguments.[0] true with
            | Some user ->
                let avurl = 
                    let url = user.GetAvatarUrl(ImageFormat.Auto)
                    url.Remove(url.Length - 9)
                let builder = EmbedBuilder()
                ctx.messageSender.BuilderWithAuthor(ctx.message, builder)
                builder
                    .WithFooter(ctx.commandName)
                    .WithImageUrl(avurl)
                    .WithColor(ctx.messageSender.ColorGood)
                    |> ignore
                [ ctx.sendEmbed (builder.Build()) ]
            | None ->
                [ ctx.sendWarn None "Could not find any user for your input" ]
    }

    [<GuildCommand>]
    [<Command("icon", "Gets the avatar of the guild", "icon <nothing>")>]
    let icon (ctx : CommandContext) = async {
        let guser = ctx.message.Author :?> SocketGuildUser
        let guild = guser.Guild
        let builder = EmbedBuilder()
        ctx.messageSender.BuilderWithAuthor(ctx.message, builder)
        builder
            .WithFooter(ctx.commandName)
            .WithImageUrl(guild.IconUrl)
            .WithColor(ctx.messageSender.ColorGood)
            |> ignore
        return [ ctx.sendEmbed (builder.Build()) ]
    }

    [<CommandParameters(1)>]
    [<Command("e", "Gets the picture of a guild emoji","e <guildemoji>")>]
    let emoji (ctx : CommandContext) = async {
        let e = ref null
        return
            if Emote.TryParse(ctx.arguments.[0], e) then
                let builder = EmbedBuilder()
                ctx.messageSender.BuilderWithAuthor(ctx.message, builder)
                builder
                    .WithFooter("emote")
                    .WithImageUrl(e.Value.Url)
                    .WithColor(ctx.messageSender.ColorGood)
                    |> ignore
                [ ctx.sendEmbed (builder.Build()) ]
            else
                [ ctx.sendWarn (Some "emote") "A guild emoji is expected as parameter" ]
    }