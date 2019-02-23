namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Information")>]
module Info =
    open Energize.Commands.Context
    open Discord
    open Discord.WebSocket
    open Energize.Commands
    open AsyncHelper
    open System.Text
    open Energize.Toolkit
    open Energize.Commands.UserHelper
    open System

    [<GuildOnlyCommand>]
    [<Command("server", "Gets information about the server", "server <nothing>")>]
    let server (ctx : CommandContext) = async {
        let guild = (ctx.message.Channel :?> IGuildChannel).Guild :?> SocketGuild
        let owner = awaitResult (ctx.restClient.GetUserAsync(guild.Owner.Id))
        let createdAt =
            let time = guild.CreatedAt.ToString()
            time.Remove(time.Length - 7)
        let region = guild.VoiceRegionId.ToString().ToUpper()
        let builder = StringBuilder()
        builder
            .Append(sprintf "**ID:** %d\n" guild.Id)
            .Append(sprintf "**OWNER:** %s\n" (match owner with null -> "Unknown" | _ -> owner.Mention))
            .Append(sprintf "**MEMBERS:** %d\n" guild.MemberCount)
            .Append(sprintf "**REGION:** %s\n" region)
            .Append(sprintf "**CREATED ON:** %s\n" createdAt)
            .Append(sprintf "**MAIN CHANNEL:** %s\n" guild.DefaultChannel.Name)
            |> ignore

        let len = guild.Emotes |> Seq.length
        if len > 0 then
            builder.Append("\n--- Emotes ---\n") |> ignore
            for i in 0..(len - 1) do
                let emoji = guild.Emotes |> Seq.tryItem i
                match emoji with
                | Some e ->
                    builder.Append(" " + e.ToString() + " ") |> ignore
                    if (i % 10).Equals(0) then
                        builder.Append('\n') |> ignore
                | None -> ()
        
        let result = builder.ToString()
        awaitIgnore (ctx.messageSender.Send(ctx.message, guild.Name, result, ctx.messageSender.ColorGood, guild.IconUrl))
    }

    [<Command("info", "Gets information about the bot", "info <nothing>")>]
    let info (ctx : CommandContext) = async {
        let invite = sprintf "<https://discordapp.com/oauth2/authorize?client_id=%d&scope=bot&permissions=8>" Config.BOT_ID_MAIN
        let server = Config.SERVER_INVITE
        let github = Config.GITHUB
        let owner = match ctx.client.GetUser(Config.OWNER_ID) with null -> ctx.client.CurrentUser :> SocketUser | o -> o
        let usercount = ctx.client.Guilds |> Seq.map (fun g -> g.Users) |> Seq.length
        let builder = StringBuilder()
        builder
            .Append(sprintf "**NAME:** %s\n" (ctx.client.CurrentUser.ToString()))
            .Append(sprintf "**PREFIX:** %s\n" ctx.prefix)
            .Append(sprintf "**COMMANDS:** %d\n" ctx.commandCount)
            .Append(sprintf "**SERVERS:** %d\n" ctx.client.Guilds.Count)
            .Append(sprintf "**USERS: ** %d\n" usercount)
            .Append(sprintf "**OWNER: ** %s\n" (owner.ToString()))
            |> ignore

        awaitIgnore (ctx.messageSender.Send(ctx.message, "info", builder.ToString(), ctx.messageSender.ColorGood, ctx.client.CurrentUser.GetAvatarUrl()))
        awaitIgnore (ctx.messageSender.SendRaw(ctx.message, sprintf "Official server: %s\nInvite link: %s\nGithub: %s" server invite github))
    }

    [<Command("invite", "Gets the bot invite links", "invite <nothing>")>]
    let invite (ctx : CommandContext) = async {
        let invite = sprintf "<https://discordapp.com/oauth2/authorize?client_id=%d&scope=bot&permissions=8>" Config.BOT_ID_MAIN
        awaitIgnore (ctx.messageSender.SendRaw(ctx.message, invite + "\n" + Config.SERVER_INVITE))
    }

    [<Command("user", "Gets information about a specific user", "user <user|userid|nothing>")>]
    let user (ctx : CommandContext) = async {
        let user =
            if ctx.hasArguments then
                findUser ctx ctx.arguments.[0] true
            else
                Some (ctx.message.Author)
        match user with
        | Some user ->
            let max = 15
            let guildNames = (ctx.client.Guilds |> Seq.map (fun g -> g.Name)) |> Seq.toList
            let leftGuilds = match (guildNames |> Seq.length) - max  with n when n < 0 -> 0 | n -> n
            let time = user.CreatedAt.ToString()
            let createdTime = time.Remove(time.Length - 7)
            let moreGuilds = if leftGuilds > 0 then (sprintf " and %d more...") leftGuilds else String.Empty
            let clampGuild = (if max >= guildNames.Length then guildNames.Length - 1 else max)
            let userGuilds = (String.Join(',', guildNames.[..clampGuild])) + moreGuilds
            let builder = StringBuilder()
            builder
                .Append(sprintf "**ID:** %d\n" user.Id)
                .Append(sprintf "**NAME:** %s\n" user.Username)
                .Append(sprintf "**BOT:** %b\n" user.IsBot)
                .Append(sprintf "**STATUS:** %s\n" (user.Status.ToString()))
                .Append(sprintf "**CREATED ON:** %s\n" createdTime)
                .Append(sprintf "**SEEN ON:** %s\n" userGuilds)
                |> ignore

            if not (ctx.isPrivate) then
                let guser = user :> IUser :?> IGuildUser
                let time = guser.JoinedAt.ToString()
                let joinedTime = if time.Length >= 7 then time.Remove(time.Length - 7) else time
                let roleNames = guser.RoleIds |> Seq.map (fun id -> guser.Guild.GetRole(id).Name) |> Seq.toList
                let leftRoles = match (roleNames |> Seq.length) - max with n when n < 0 -> 0 | n -> n
                let nick = match guser.Nickname with null -> "none" | name -> name
                let moreNames = if leftRoles > 0 then (sprintf " and %d more..." leftRoles) else String.Empty
                let clampRoles = (if max >= roleNames.Length then roleNames.Length - 1 else max)
                let userNames = (String.Join(',', roleNames.[..clampRoles])) + moreNames
                builder
                    .Append("\n--- Guild Related Info ---\n")
                    .Append(sprintf "**NICKNAME:** %s\n" nick)
                    .Append(sprintf "**JOINED ON:** %s\n" joinedTime)
                    .Append(sprintf "**ROLES:** %s\n" userNames)
                    |> ignore

            let res = builder.ToString()
            if res.Length > 2000 then
                ctx.sendWarn None "Output was too long to be displayed"
            else
                awaitIgnore (ctx.messageSender.Send(ctx.message, ctx.commandName, builder.ToString(), ctx.messageSender.ColorGood, user.GetAvatarUrl()))
        | None ->
            ctx.sendWarn None "No user could be found for your input"
    }

    [<GuildOnlyCommand>]
    [<CommandParameters(1)>]
    [<Command("isadmin", "Shows if a user is an admin", "isadmin <user|userid>")>]
    let isAdmin (ctx : CommandContext) = async {
        match findUser ctx ctx.arguments.[0] true with
        | Some user  ->
            let guser = user :?> SocketGuildUser
            let res = sprintf "%s is %san administrator" (user.ToString()) (if guser.GuildPermissions.Administrator then String.Empty else "not ")
            ctx.sendOK None res
        | None ->
            ctx.sendWarn None "No user could be found for your input"
    }

    [<GuildOnlyCommand>]
    [<CommandParameters(1)>]
    [<Command("roles", "Gets a user roles and role ids", "roles <user|userid>")>]
    let roles (ctx : CommandContext) = async {
        match findUser ctx ctx.arguments.[0] true with
        | Some user ->
            let guser = user :?> SocketGuildUser
            let builder = StringBuilder()
            builder.Append("```") |> ignore
            for role in guser.Roles do
                builder.Append(sprintf "%s\t\t%d\n" role.Name role.Id) |> ignore
            builder.Append("```") |> ignore
            ctx.sendOK None (builder.ToString())
        | None ->
            ctx.sendWarn None "No user could be found for your input"
    }

    [<Command("snipe", "Snipes the last deleted message in the channel", "snipe <nothing>")>]
    let snipe (ctx : CommandContext) = async {
        match ctx.cache.lastDeletedMessage with
        | Some msg ->
            let avurl = msg.Author.GetAvatarUrl(ImageFormat.Auto, 32us)
            let builder = EmbedBuilder()
            ctx.messageSender.BuilderWithAuthor(ctx.message, builder)
            builder
                .WithColor(ctx.messageSender.ColorGood)
                .WithFooter("message sniped from " + msg.Author.ToString(), avurl)
                .WithTimestamp(msg.CreatedAt)
                .WithDescription(msg.Content)
                |> ignore
            match ImageUrlProvider.getLastImgUrl msg with
            | Some url -> builder.WithImageUrl(url) |> ignore
            | None -> ()

            awaitIgnore (ctx.messageSender.Send(ctx.message, builder.Build()))
        | None ->
            ctx.sendWarn None "There was nothing to snipe"
    }

    type private CommitInfo = { message: string }
    type private Commit = { commit : CommitInfo }
    [<Command("lastchanges", "Gets the last changes published on GitHub", "lastchanges <nothing>")>]
    let lastChanges (ctx : CommandContext) = async {
        let endpoint = "https://api.github.com/repos/Earu/Energize/commits"
        let json = awaitResult (HttpClient.Fetch(endpoint, ctx.logger))
        let commits = JsonPayload.Deserialize<Commit list>(json, ctx.logger)
        if not (commits.Equals(null)) && commits |> List.length > 0 then
            match commits |> List.tryHead with
            | Some entry ->
                ctx.sendOK None (sprintf "```\n%s\n```" entry.commit.message)
            | None ->
                ctx.sendWarn None "Could not find any commits"
        else
            ctx.sendWarn None "There was a problem fetching last changes"
    }

    [<CommandParameters(1)>]
    [<Command("playing", "Gets the amount of player playing a game", "playing <game>")>]
    let playing (ctx : CommandContext) = async {
        let sum = 
            ctx.client.Guilds |> Seq.map
                (fun guild ->
                    guild.Users |> Seq.filter 
                        (fun u -> 
                            let act = 
                                match u.Activity with
                                | null -> String.Empty
                                | a -> a.Name.ToLower()
                            act.Equals(ctx.input.ToLower())
                        )
                        |> Seq.length
                ) |> Seq.sum
        let display = 
            if sum > 1 then
                sprintf "There are %d users playing %s" sum ctx.input
            else
                sprintf "There is %d user playing %s" sum ctx.input
        ctx.sendOK None display
    }