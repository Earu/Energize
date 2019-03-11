﻿namespace Energize.Commands.Implementation

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

    [<GuildCommand>]
    [<Command("server", "Gets information about the server", "server <nothing>")>]
    let server (ctx : CommandContext) = async {
        let guild = (ctx.message.Channel :?> IGuildChannel).Guild :?> SocketGuild
        let owner = awaitResult (ctx.restClient.GetUserAsync(guild.OwnerId))
        let createdAt =
            let time = guild.CreatedAt.ToString()
            time.Remove(time.Length - 7)
        let region = guild.VoiceRegionId.ToString().ToUpper()

        let aemotes =
            guild.Emotes |> Seq.filter (fun e -> e.Animated) |> Seq.map(fun e -> e.ToString())
        let emotes = 
            guild.Emotes |> Seq.filter (fun e -> not e.Animated) |> Seq.map(fun e -> e.ToString())
        
        let fields = [
            ctx.embedField "ID" guild.Id true
            ctx.embedField "Owner" (match owner with null -> "Unknown" | _ -> owner.Mention) true
            ctx.embedField "Members" guild.MemberCount true
            ctx.embedField "Region" region true
            ctx.embedField "Creation Date" createdAt true
            ctx.embedField "Main Channel" guild.DefaultChannel.Name true
            ctx.embedField "Emotes" (String.Join(String.Empty, emotes)) true
            ctx.embedField "Animated Emotes" (String.Join(String.Empty, aemotes)) true
        ]

        let builder = EmbedBuilder()
        ctx.messageSender.BuilderWithAuthor(ctx.message, builder)
        builder
            .WithFields(fields)
            .WithThumbnailUrl(guild.IconUrl)
            .WithColor(ctx.messageSender.ColorGood)
            .WithFooter(guild.Name)
            |> ignore

        return [ ctx.sendEmbed (builder.Build()) ]
    }

    [<Command("info", "Gets information about the bot", "info <nothing>")>]
    let info (ctx : CommandContext) = async {
        let invite = sprintf "<https://discordapp.com/oauth2/authorize?client_id=%d&scope=bot&permissions=8>" Config.BOT_ID_MAIN
        let github = Config.GITHUB
        let owner = match ctx.client.GetUser(Config.OWNER_ID) with null -> ctx.client.CurrentUser :> SocketUser | o -> o
        let usercount = ctx.client.Guilds |> Seq.map (fun g -> g.Users.Count) |> Seq.sum
        let fields = [
            ctx.embedField "Name" ctx.client.CurrentUser true
            ctx.embedField "Prefix" ctx.prefix true
            ctx.embedField "Command Count" ctx.commandCount true
            ctx.embedField "Server Count" ctx.client.Guilds.Count true
            ctx.embedField "User count" usercount true
            ctx.embedField "Owner" owner true
        ]

        let builder = EmbedBuilder()
        ctx.messageSender.BuilderWithAuthor(ctx.message, builder)
        builder
            .WithFields(fields)
            .WithThumbnailUrl(ctx.client.CurrentUser.GetAvatarUrl())
            .WithColor(ctx.messageSender.ColorGood)
            .WithFooter("info")
            |> ignore

        return [
            ctx.sendEmbed (builder.Build())
            ctx.sendRaw (sprintf "Invite link: %s\nGithub: %s" invite github)
        ]
    }

    [<Command("invite", "Gets the bot invite links", "invite <nothing>")>]
    let invite (ctx : CommandContext) = async {
        let invite = sprintf "<https://discordapp.com/oauth2/authorize?client_id=%d&scope=bot&permissions=8>" Config.BOT_ID_MAIN
        return [ ctx.sendRaw (invite + "\n" + Config.SERVER_INVITE) ]
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
                    let nick = match guser.Nickname with null -> " - " | name -> name
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
                    
            ctx.messageSender.BuilderWithAuthor(ctx.message, builder)
            builder
                .WithFields(fields)
                .WithThumbnailUrl(user.GetAvatarUrl())
                .WithColor(ctx.messageSender.ColorGood)
                .WithFooter("info")
                |> ignore
            return [ ctx.sendEmbed (builder.Build()) ]
        | None ->
            return [ ctx.sendWarn None "No user could be found for your input" ]
    }

    [<GuildCommand>]
    [<CommandParameters(1)>]
    [<Command("isadmin", "Shows if a user is an admin", "isadmin <user|userid>")>]
    let isAdmin (ctx : CommandContext) = async {
        return 
            match findUser ctx ctx.arguments.[0] true with
            | Some user  ->
                let guser = user :?> SocketGuildUser
                let res = sprintf "%s is %san administrator" (user.ToString()) (if guser.GuildPermissions.Administrator then String.Empty else "not ")
                [ ctx.sendOK None res ]
            | None ->
                [ ctx.sendWarn None "No user could be found for your input" ]
    }

    [<GuildCommand>]
    [<CommandParameters(1)>]
    [<Command("roles", "Gets a user roles and role ids", "roles <user|userid>")>]
    let roles (ctx : CommandContext) = async {
        return 
            match findUser ctx ctx.arguments.[0] true with
            | Some user ->
                let guser = user :?> SocketGuildUser
                let builder = StringBuilder()
                builder.Append("```") |> ignore
                for role in guser.Roles do
                    builder.Append(sprintf "%s\t\t%d\n" role.Name role.Id) |> ignore
                builder.Append("```") |> ignore
                [ ctx.sendOK None (builder.ToString()) ]
            | None ->
                [ ctx.sendWarn None "No user could be found for your input" ]
    }

    [<Command("snipe", "Snipes the last deleted message in the channel", "snipe <nothing>")>]
    let snipe (ctx : CommandContext) = async {
        return 
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

                [ ctx.sendEmbed (builder.Build()) ]
            | None ->
                [ ctx.sendWarn None "There was nothing to snipe" ]
    }

    type CommitInfo = { message: string }
    type Commit = { commit : CommitInfo }
    [<Command("lastchanges", "Gets the last changes published on GitHub", "lastchanges <nothing>")>]
    let lastChanges (ctx : CommandContext) = async {
        let endpoint = "https://api.github.com/repos/Earu/Energize/commits"
        let json = awaitResult (HttpClient.GetAsync(endpoint, ctx.logger))
        let commits = JsonPayload.Deserialize<Commit list>(json, ctx.logger)
        return 
            if not (commits.Equals(null)) && commits |> List.length > 0 then
                match commits |> List.tryHead with
                | Some entry ->
                    [ ctx.sendOK None (sprintf "```\n%s\n```" entry.commit.message) ]
                | None ->
                    [ ctx.sendWarn None "Could not find any commits" ]
            else
                [ ctx.sendWarn None "There was a problem fetching last changes" ]
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
        return [ ctx.sendOK None display ]
    }