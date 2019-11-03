namespace Energize.Commands.Implementation

open Energize.Commands.Command
open Energize.Commands.AsyncHelper
open System.Diagnostics
open Energize.Commands.Context
open System
open Energize.Essentials
open Discord
open System.Threading.Tasks
open System.Text
open Microsoft.Data.Sqlite
open Energize.Interfaces.Services.Development
open Energize.Interfaces.Services.Listeners
open Energize.Commands.UserHelper
open Discord.WebSocket
open Energize.Commands.ImageUrlProvider
open Energize.Essentials.Helpers

[<CommandModule("Utilities")>]
module Util =
    [<Command("ping", "Pings the bot", "ping <nothing>")>]
    let ping (ctx : CommandContext) = async {
        let timestamp = ctx.message.Timestamp
        let diff = timestamp.Millisecond / 10
        let res = sprintf "⏰ Discord: %dms\n🕐 Bot: %dms" diff ctx.client.Latency
        return [ ctx.sendOK None res ]
    }

    [<Command("mem", "Gets the current memory usage", "mem <nothing>")>]
    let mem (ctx : CommandContext) = async {
        let proc = Process.GetCurrentProcess()
        let mbused = proc.WorkingSet64 / 1024L / 1024L
        return [ ctx.sendOK None (sprintf "Currently using %dMB of memory" mbused) ]
    }    

    [<Command("uptime", "Gets the current uptime", "uptime <nothing>")>]
    let uptime (ctx : CommandContext) = async {
        let diff = (DateTime.Now - Process.GetCurrentProcess().StartTime).Duration();
        let res = sprintf "%dd%dh%dm" diff.Days diff.Hours diff.Minutes
        return [ ctx.sendOK None ("The current instance has been up for " + res) ]
    }

    [<CommandParameters(1)>]
    [<CommandConditions(CommandCondition.DevOnly)>]
    [<Command("to", "Timing out test", "to <seconds>")>]
    let timeOut (ctx : CommandContext) = async {
        let duration = int ctx.input
        await (Task.Delay(duration * 1000))
        return [ ctx.sendOK (Some "timeout") (sprintf "Timed out during `%d`s" duration) ]
    }

    [<CommandParameters(1)>]
    [<CommandConditions(CommandCondition.DevOnly)>]
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

    [<CommandConditions(CommandCondition.DevOnly)>]
    [<Command("err", "Throws an error for testing", "err <nothing|message>")>]
    let err (ctx : CommandContext) : Async<IUserMessage list> = async {
        let msg = if ctx.hasArguments then ctx.input else "test"
        raise (Exception(msg))
        return []
    }

    [<CommandParameters(1)>]
    [<CommandConditions(CommandCondition.DevOnly)>]
    [<Command("ev", "Evals a C# string", "ev <csharpstring>")>]
    let eval (ctx : CommandContext) = async {
        let evaluator = ctx.serviceManager.GetService<ICSharpEvaluationService>("Evaluator")
        let res = awaitResult (evaluator.EvalAsync(ctx.input, ctx))
        return
            match res.ToTuple() with
            | (0, out) -> [ ctx.sendBad None out ]
            | (1, out) -> [ ctx.sendOK None out ]
            | (_, out) -> [ ctx.sendWarn None out ]
    }

    [<CommandParameters(3)>]
    [<CommandConditions(CommandCondition.DevOnly)>]
    [<Command("mail", "Sends a mail to the given mail address with the given content and subject", "mail <mailaddress> <subject> <content>")>]
    let mail (ctx : CommandContext) = async {
        let mail = ctx.serviceManager.GetService<IMailingService>("Mail")
        let adr = ctx.arguments.[0]
        return
            try
                await (mail.SendMailAsync(adr, ctx.arguments.[1], ctx.arguments.[2]))
                [ ctx.sendOK None (sprintf "Sent a mail to `%s` successfully" adr) ]
            with ex ->
                [ ctx.sendWarn None (sprintf "Could not send a mail to `%s`: %s" adr ex.Message)]
    }

    type CodeTabObj = { language : string; files : int; linesOfCode : int; comments : int }
    [<CommandConditions(CommandCondition.DevOnly)>]
    [<Command("codestats", "Gets code stats about Energize repository", "codestats <nothing>")>]
    let codeStats (ctx : CommandContext) = async {
        let json = awaitResult (HttpHelper.GetAsync("https://api.codetabs.com/v1/loc?github=Energizers/Energize", ctx.logger))
        let mutable results : CodeTabObj list = [] 
        return
            if (JsonHelper.TryDeserialize(json, ctx.logger, &results)) then
                match results |> List.tryFind (fun obj -> obj.language.Equals("Total")) with
                | Some result ->
                    let fields = [
                        ctx.embedField "Files" result.files true
                        ctx.embedField "Comments" result.comments true
                        ctx.embedField "Lines of Code" result.linesOfCode true
                    ]

                    let builder = EmbedBuilder()
                    builder
                        .WithFields(fields)
                        .WithAuthorNickname(ctx.message)
                        .WithColorType(EmbedColorType.Good)
                        .WithFooter(ctx.commandName)
                        |> ignore
                    [ ctx.sendEmbed (builder.Build())]
                | None -> 
                    [ ctx.sendWarn None "There was a problem processing the result" ]
            else
                [ ctx.sendWarn None "There was a problem processing the result" ]
    }

    [<CommandConditions(CommandCondition.DevOnly)>]
    [<MaintenanceFreeCommand>]
    [<Command("restart", "Restarts the bot", "restart <nothing>")>]
    let restart (ctx : CommandContext) : Async<IUserMessage list> = async {
        let restart = ctx.serviceManager.GetService<IRestartService>("Restart")
        let music = ctx.serviceManager.GetService<IMusicPlayerService>("Music")
        await (music.DisconnectAllPlayersAsync("Bot is restarting, disconnecting", true))
        await (restart.WarnChannelAsync(ctx.message.Channel, "Restarting..."))
        await (restart.RestartAsync())
        return []
    }

    [<Command("lavalink", "Gets the lavalink server stats", "lavalink <nothing>")>]
    let lavalink (ctx : CommandContext) = async {
        let music = ctx.serviceManager.GetService<IMusicPlayerService>("Music")
        return
            match music.LavalinkStats |> Option.ofObj with
            | None -> [ ctx.sendWarn None "Stats not available yet" ]
            | Some stats ->
                let builder = EmbedBuilder()
                let uptime = sprintf "%dd%dh%dm" stats.Uptime.Days stats.Uptime.Hours stats.Uptime.Minutes
                let fields = [
                    ctx.embedField "CPU Load" (sprintf "%.2f%s" (match stats.Cpu |> Option.ofObj with None -> 0.0 | Some cpu -> cpu.LavalinkLoad * 100.0) "%") true
                    ctx.embedField "Frames" (match stats.Frames |> Option.ofObj with None -> 0 | Some frames -> frames.Sent) true
                    ctx.embedField "Memory (MB)" (match stats.Memory |> Option.ofObj with None -> 0L | Some mem -> mem.Used / 1024L / 1024L) true
                    ctx.embedField "Music Players" (sprintf "Energize: `%d`\nLavalink: `%d`" music.PlayerCount stats.PlayerCount) true
                    ctx.embedField "Playing Players" (sprintf "Energize: `%d`\nLavalink: `%d`" music.PlayingPlayersCount stats.PlayingPlayers) true
                    ctx.embedField "Uptime" uptime true
                ]

                builder
                    .WithAuthorNickname(ctx.message)
                    .WithColorType(EmbedColorType.Good)
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
                builder
                    .WithAuthorNickname(ctx.message)
                    .WithFooter(ctx.commandName)
                    .WithImageUrl(avurl)
                    .WithColorType(EmbedColorType.Good)
                    |> ignore
                [ ctx.sendEmbed (builder.Build()) ]
            | None ->
                [ ctx.sendWarn None "Could not find any user for your input" ]
    }

    [<Command("icon", "Gets the avatar of the guild", "icon <nothing>")>]
    let icon (ctx : CommandContext) = async {
        let guser = ctx.message.Author :?> SocketGuildUser
        let guild = guser.Guild
        let builder = EmbedBuilder()
        builder
            .WithAuthorNickname(ctx.message)
            .WithFooter(ctx.commandName)
            .WithImageUrl(guild.IconUrl)
            .WithColorType(EmbedColorType.Good)
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
                builder
                    .WithAuthorNickname(ctx.message)
                    .WithFooter("emote")
                    .WithImageUrl(e.Value.Url)
                    .WithColorType(EmbedColorType.Good)
                    |> ignore
                [ ctx.sendEmbed (builder.Build()) ]
            else
                [ ctx.sendWarn (Some "emote") "A guild emoji is expected as parameter" ]
    }

    type CommitInfo = { message: string }
    type Commit = { commit : CommitInfo }
    [<Command("lastchanges", "Gets the last changes published on GitHub", "lastchanges <nothing>")>]
    let lastChanges (ctx : CommandContext) = async {
        let endpoint = "https://api.github.com/repos/Earu/Energize/commits"
        let json = awaitResult (HttpHelper.GetAsync(endpoint, ctx.logger))
        let mutable commits = []
        return 
            if JsonHelper.TryDeserialize<Commit list>(json, ctx.logger, &commits) then
                match commits |> List.tryHead with
                | Some entry ->
                    [ ctx.sendOK None (sprintf "```\n%s\n```" entry.commit.message) ]
                | None ->
                    [ ctx.sendWarn None "Could not find any commits" ]
            else
                [ ctx.sendWarn None "There was a problem fetching last changes" ]
    }

    [<Command("snipe", "Snipes the last deleted message in the channel", "snipe <nothing>")>]
    let snipe (ctx : CommandContext) = async {
        return 
            match ctx.cache.lastDeletedMessage with
            | Some msg ->
                let avurl = msg.Author.GetAvatarUrl(ImageFormat.Auto, 32us)
                let builder = EmbedBuilder()
                builder
                    .WithAuthorNickname(ctx.message)
                    .WithColorType(EmbedColorType.Good)
                    .WithFooter("message sniped from " + msg.Author.ToString(), avurl)
                    .WithTimestamp(msg.CreatedAt)
                    .WithLimitedDescription(msg.Content)
                    |> ignore
                match getLastImgUrl msg with
                | Some url -> builder.WithImageUrl(url) |> ignore
                | None -> ()

                [ ctx.sendEmbed (builder.Build()) ]
            | None ->
                [ ctx.sendWarn None "There was nothing to snipe" ]
    }

    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<Command("server", "Gets information about the server", "server <nothing>")>]
    let server (ctx : CommandContext) = async {
        let guild = (ctx.message.Channel :?> IGuildChannel).Guild :?> SocketGuild
        let owner = awaitResult (ctx.restClient.GetUserAsync(guild.OwnerId))
        let createdAt =
            let time = guild.CreatedAt.ToString()
            time.Remove(time.Length - 7)
        let region = guild.VoiceRegionId.ToString().ToUpper()

        let aemotes =
            guild.Emotes |> Seq.filter (fun e -> e.Animated) |> Seq.map(fun e -> sprintf "<a:_:%d>" e.Id)
        let emotes = 
            guild.Emotes |> Seq.filter (fun e -> not e.Animated) |> Seq.map(fun e -> sprintf "<:_:%d>" e.Id)
        
        let fields = [
            ctx.embedField "ID" guild.Id true
            ctx.embedField "Owner" (match owner |> Option.ofObj with None -> "Unknown" | Some owner -> owner.Mention) true
            ctx.embedField "Members" guild.MemberCount true
            ctx.embedField "Region" region true
            ctx.embedField "Creation Date" createdAt true
            ctx.embedField "Main Channel" guild.DefaultChannel.Name true
            ctx.embedField "Emotes" (String.Join(String.Empty, emotes)) true
            ctx.embedField "Animated Emotes" (String.Join(String.Empty, aemotes)) true
        ]

        let builder = EmbedBuilder()
        builder
            .WithAuthorNickname(ctx.message)
            .WithFields(fields)
            .WithThumbnailUrl(guild.IconUrl)
            .WithColorType(EmbedColorType.Good)
            .WithFooter(guild.Name)
            |> ignore

        return [ ctx.sendEmbed (builder.Build()) ]
    }

    [<Command("info", "Gets information about the bot", "info <nothing>")>]
    let info (ctx : CommandContext) = async {
        let invite = Config.Instance.URIs.InviteURL
        let github = Config.Instance.URIs.GitHubURL
        let docs = Config.Instance.URIs.WebsiteURL
        let discord = Config.Instance.URIs.DiscordURL
        let owner = 
            match ctx.client.GetUser(Config.Instance.Discord.OwnerID) |> Option.ofObj with
            | None -> ctx.client.CurrentUser :> SocketUser 
            | Some owner -> owner
        let usercount = ctx.client.Guilds |> Seq.map (fun g -> g.Users.Count) |> Seq.sum
        let fields = [
            ctx.embedField "Name" ctx.client.CurrentUser true
            ctx.embedField "Prefix" ctx.prefix true
            ctx.embedField "Server Count" ctx.client.Guilds.Count true
            ctx.embedField "User count" usercount true
            ctx.embedField "Owner" owner true
            ctx.embedField "Links" (String.Join('\n', [ github; invite; docs; discord ] |> List.map (fun url -> sprintf "**%s**" url))) false
        ]

        let builder = EmbedBuilder()
        builder
            .WithAuthorNickname(ctx.message)
            .WithFields(fields)
            .WithThumbnailUrl(ctx.client.CurrentUser.GetAvatarUrl())
            .WithColorType(EmbedColorType.Good)
            .WithFooter("info")
            |> ignore

        return [ ctx.sendEmbed (builder.Build()) ]
    }

    [<Command("docs", "Gets the documentation link", "docs <nothing>")>]
    let docs (ctx : CommandContext) = async {
        let docsUrl = Config.Instance.URIs.WebsiteURL
        return [ ctx.sendRaw docsUrl ]
    }

    [<Command("invite", "Gets the bot invite links", "invite <nothing>")>]
    let invite (ctx : CommandContext) = async {
        let inviteUrl = Config.Instance.URIs.InviteURL
        return [ ctx.sendRaw inviteUrl ]
    }

    [<CommandConditions(CommandCondition.DevOnly)>]
    [<MaintenanceFreeCommand>]
    [<CommandParameters(1)>]
    [<Command("shell", "Execute a shell cmd", "cmd <bashstring>")>]
    let shell (ctx: CommandContext) = async {
        let parts = ctx.input.Split("\s")
        let proc = 
            let startInfo = ProcessStartInfo()
            startInfo.FileName <- parts.[0]
            startInfo.RedirectStandardError <- true
            startInfo.RedirectStandardOutput <- true
            if parts.Length > 1 then
                startInfo.Arguments <- String.Join("\s", parts.[1..])
            
            Process.Start(startInfo) |> Option.ofObj
        return 
            match proc with
            | Some proc ->
                proc.EnableRaisingEvents <- true
                proc.BeginOutputReadLine()
                proc.BeginErrorReadLine()

                let builder = StringBuilder()
                proc.OutputDataReceived.Add(fun out -> builder.AppendLine(out.Data) |> ignore)
                proc.ErrorDataReceived.Add(fun err -> builder.AppendLine(err.Data) |> ignore)
                proc.Exited.Add(fun _ -> 
                    let output = if builder.Length > 2000 then (sprintf "%s..." (builder.ToString())) else builder.ToString()
                    ctx.sendRaw (sprintf "```shell\n%s```" output) |> ignore
                )
                [ ctx.sendOK None "Executing shell command..." ]
            | None -> [ ctx.sendWarn None "Could not execute shell command" ]
        }

    [<Command("user", "Gets information about a specific user", "user <user|userid|nothing>")>]
    let user (ctx : CommandContext) = async {
        let user =
            if ctx.hasArguments then
                findUser ctx ctx.arguments.[0] true
            else
                Some ctx.message.Author
        match user with
        | Some user ->
            let max = 15
            let guildNames = 
                ctx.client.Guilds 
                |> Seq.filter (fun g -> 
                    let opt = 
                        g.Users |> Seq.tryFind (fun u -> u.Id.Equals(user.Id))
                    match opt with Some _ -> true | None -> false
                ) |> Seq.map (fun g -> g.Name)
                |> Seq.toList
            let leftGuilds = match (guildNames |> Seq.length) - max  with n when n < 0 -> 0 | n -> n
            let time = user.CreatedAt.ToString()
            let createdTime = time.Remove(time.Length - 7)
            let moreGuilds = if leftGuilds > 0 then (sprintf " and %d more...") leftGuilds else String.Empty
            let clampGuild = (if max >= guildNames.Length then guildNames.Length - 1 else max)
            let userGuilds = (String.Join(", ", guildNames.[..clampGuild])) + moreGuilds
            let builder = EmbedBuilder()
            let fields = 
                if not (ctx.isPrivate) then
                    let guser = user :?> IGuildUser
                    let time = guser.JoinedAt.ToString()
                    let joinedTime = if time.Length >= 7 then time.Remove(time.Length - 7) else time
                    let roleNames = guser.RoleIds |> Seq.map (fun id -> guser.Guild.GetRole(id).Name) |> Seq.toList
                    let leftRoles = match (roleNames |> Seq.length) - max with n when n < 0 -> 0 | n -> n
                    let nick = match guser.Nickname |> Option.ofObj with None -> " - " | Some name -> name
                    let moreNames = if leftRoles > 0 then (sprintf " and %d more..." leftRoles) else String.Empty
                    let clampRoles = (if max >= roleNames.Length then roleNames.Length - 1 else max)
                    let userNames = (String.Join(", ", roleNames.[..clampRoles])) + moreNames
                    [
                        ctx.embedField "Nickname" nick true
                        ctx.embedField "Join Date" joinedTime true
                        ctx.embedField "Roles" userNames true
                    ]
                else
                    List.Empty
                |> List.append ([
                    ctx.embedField "ID" user.Id true
                    ctx.embedField "Name" user.Username true
                    ctx.embedField "Bot" user.IsBot true
                    ctx.embedField "Status" user.Status true
                    ctx.embedField "Creation Date" createdTime true
                    ctx.embedField "Seen On" userGuilds true
                ])
                    
            builder
                .WithAuthorNickname(ctx.message)
                .WithFields(fields)
                .WithThumbnailUrl(user.GetAvatarUrl())
                .WithColorType(EmbedColorType.Good)
                .WithFooter("info")
                |> ignore
            return [ ctx.sendEmbed (builder.Build()) ]
        | None ->
            return [ ctx.sendWarn None "No user could be found for your input" ]
    }