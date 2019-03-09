namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Administration")>]
module Administration =
    open Energize.Commands.Context
    open Energize.Commands.UserHelper
    open Discord.WebSocket
    open Energize.Commands.AsyncHelper
    open System
    open Discord
    open System.Collections.Generic
    open Energize.Toolkit
    open Energize.Interfaces.Services
    open Energize.Commands
    open Discord.Rest
    open System.Linq

    [<AdminCommand>]
    [<GuildCommand>]
    [<CommandParameters(2)>]
    [<Command("op", "Makes a user able or unable to use admin commands", "op <user|userid>,<0|1>")>]
    let op (ctx : CommandContext) = async {
        return 
            match findUser ctx ctx.arguments.[0] true with
            | Some user ->
                try
                    let guser = user :?> IGuildUser
                    let role = getOrCreateRole guser "EnergizeAdmin"
                    let out = ref 0
                    if Int32.TryParse(ctx.arguments.[1], out) then
                        if out.Value.Equals(0) then
                            await (guser.RemoveRoleAsync(role))
                            let display = sprintf "User %s was succesfully demoted" (user.ToString())
                            [ ctx.sendOK None display ]
                        else
                            await (guser.AddRoleAsync(role))
                            let display = sprintf "User %s was succesfully promoted" (user.ToString())
                            [ ctx.sendOK None display ]
                    else
                        [ ctx.sendWarn None "Incorrect value as second argument (Needs to be 0 or 1)" ]
                with _ ->
                    let display = 
                        sprintf "There was an issue when promoting %s, it is most likely due to missing permissions" (user.ToString())
                    [ ctx.sendWarn None display ]
            | None ->
                [ ctx.sendWarn None "Could not find any user for your input" ]
    }
    
    let private isMsgOld (msg : IMessage) =
        let diff = (DateTime.Now.Date.Ticks - msg.CreatedAt.Date.Ticks)
        diff > (DateTime()).AddDays(15.0).Ticks

    let private clearCmdBase (ctx : CommandContext) (input : string) (predicate : IMessage -> bool) =
        let amount =
            let out = ref 0
            if ctx.hasArguments && Int32.TryParse(input, out) then
                Math.Clamp(out.Value, 1, 200)
            else
                25
        let opts = RequestOptions()
        opts.AuditLogReason <- sprintf "Energize: %s from %s" ctx.commandName (ctx.message.Author.ToString())
        opts.RetryMode <- Nullable(RetryMode.AlwaysRetry)
        opts.Timeout <- Nullable(10)
        let msgsAsync = ctx.message.Channel.GetMessagesAsync(amount, CacheMode.AllowDownload, opts)
        let msgsFlattened = (msgsAsync :?> IAsyncEnumerable<IEnumerable<IMessage>>).Flatten() 
        let msgs = msgsFlattened.ToEnumerable() |> Seq.toList
        let toDelete = 
            (if msgs.Length > amount then msgs.[..amount] else msgs) 
            |> List.filter (fun msg -> if isMsgOld msg then false else predicate msg)
        try
            let chan = ctx.message.Channel :?> ITextChannel
            await (chan.DeleteMessagesAsync(toDelete))
            [ ctx.sendOK None (sprintf "Cleared %d messages among %d checked messages" toDelete.Length msgs.Length) ]
        with _ ->
            [ ctx.sendWarn None "There was an issue when deleting messages, it is most likely due to missing permissions" ]

    [<AdminCommand>]
    [<GuildCommand>]
    [<Command("clear", "Clear the bot messages", "clear <amounttoremove|nothing>")>]
    let clear (ctx : CommandContext) = async {
        return clearCmdBase ctx ctx.input (fun msg -> msg.Author.Id.Equals(Config.BOT_ID_MAIN))
    }

    [<AdminCommand>]
    [<GuildCommand>]
    [<Command("clearbots", "Clear bot messages", "clearbots <amounttoremove|nothing>")>]
    let clearBots (ctx : CommandContext) = async {
        return clearCmdBase ctx ctx.input (fun msg -> msg.Author.IsBot)
    }

    [<AdminCommand>]
    [<GuildCommand>]
    [<CommandParameters(1)>]
    [<Command("clearuser", "Clear a user messages", "clearuser <user|userid>,<amounttoremove|nothing>")>]
    let clearUser (ctx : CommandContext) = async {
        return 
            match findUser ctx ctx.arguments.[0] true with
            | Some user ->
                clearCmdBase ctx (if ctx.arguments.Length > 1 then ctx.arguments.[1] else "25") (fun msg -> msg.Author.Id.Equals(user.Id))
            | None ->   
                [ ctx.sendWarn None "No user could be found for your input" ]
    }

    [<AdminCommand>]
    [<GuildCommand>]
    [<Command("clearaw", "Clear a specified amount of messages", "clearaw <amounttoremove|nothing>")>]
    let clearRaw (ctx : CommandContext) = async {
        return clearCmdBase ctx ctx.input (fun _ -> true)
    }

    [<AdminCommand>]
    [<GuildCommand>]
    [<Command("delinvs", "Deletes messages containing discord invites (toggleable)", "delinvs <nothing>")>]
    let delInvs (ctx : CommandContext) = async {
        let guser = ctx.message.Author :?> SocketGuildUser
        let db = ctx.serviceManager.GetService<IDatabaseService>("Database")
        let dbctx = awaitResult (db.GetContext())
        let dbguild = awaitResult (dbctx.Instance.GetOrCreateGuild(guser.Guild.Id))
        dbguild.ShouldDeleteInvites <- not dbguild.ShouldDeleteInvites
        dbctx.Dispose()

        return
            if dbguild.ShouldDeleteInvites then
                [ ctx.sendOK None "Invite links will now be automatically deleted" ]
            else
                [ ctx.sendOK None "Invite links will now be permitted" ]
    }

    let private createHOFChannel (ctx : CommandContext) = 
        let name = "⭐hall-of-fames"
        let desc = sprintf "Where %s will post unique messages" (ctx.client.CurrentUser.ToString())
        let guser = ctx.message.Author :?> SocketGuildUser
        let created = awaitResult (guser.Guild.CreateTextChannelAsync(name))
        let everyoneperms = OverwritePermissions(mentionEveryone = PermValue.Deny, sendMessages = PermValue.Deny, sendTTSMessages = PermValue.Deny)
        let botperms = OverwritePermissions(sendMessages = PermValue.Allow, addReactions = PermValue.Allow)
        await (created.AddPermissionOverwriteAsync(guser.Guild.EveryoneRole, everyoneperms))
        await (created.AddPermissionOverwriteAsync(ctx.client.CurrentUser, botperms))
        await (created.ModifyAsync(Action<TextChannelProperties>(fun prop -> prop.Topic <- Optional(desc))))
        created :> ITextChannel

    let private getOrCreateHOFChannel (ctx : CommandContext) = 
        let db = ctx.serviceManager.GetService<IDatabaseService>("Database")
        let dbctx = awaitResult (db.GetContext())
        let guild = (ctx.message.Author :?> SocketGuildUser).Guild :> IGuild
        let dbguild = awaitResult (dbctx.Instance.GetOrCreateGuild(guild.Id))
        let chan = 
            if dbguild.HasHallOfShames then
                Some ((awaitResult (guild.GetChannelAsync(dbguild.HallOfShameID))) :?> ITextChannel)
            else
                try
                    let c = createHOFChannel ctx
                    dbguild.HallOfShameID <- c.Id
                    dbguild.HasHallOfShames <- true
                    Some c
                with _ ->
                    None
        dbctx.Dispose()
        chan

    let private trySendFameMsg (ctx : CommandContext) (chan : ITextChannel option) (msg : IMessage) = 
        let builder = EmbedBuilder()
        builder
            .WithAuthor(msg.Author)
            .WithDescription(msg.Content)
            .WithFooter("#" + msg.Channel.Name)
            .WithTimestamp(msg.CreatedAt)
            .WithColor(ctx.messageSender.ColorNormal)
            |> ignore
        match ImageUrlProvider.getLastImgUrl msg with
        | Some url ->
            builder.WithImageUrl(url) |> ignore
        | None -> ()

        match chan.Value with
        | :? SocketChannel as chan ->
            Some (awaitResult (ctx.messageSender.Send(chan, builder.Build())))
        | :? RestChannel as chan ->
            Some (awaitResult (ctx.messageSender.Send(chan, builder.Build())))
        | _ -> None

    [<AdminCommand>]
    [<GuildCommand>]
    [<CommandParameters(1)>]
    [<Command("fame", "Adds a message to the hall of fames", "fame <messageid>")>]
    let fame (ctx : CommandContext) = async {
        let chan = getOrCreateHOFChannel ctx
        let msgId = ref 0UL
        return 
            if UInt64.TryParse(ctx.arguments.[0], msgId) && chan.IsSome then
                let msg = awaitResult (ctx.message.Channel.GetMessageAsync(msgId.Value))
                match msg with
                | null ->
                    [ ctx.sendWarn None "Could not find any message for your input" ]
                | m ->
                    match trySendFameMsg ctx chan m with
                    | None ->
                        [ ctx.sendWarn None "There was an error when posting the message into hall of fames" ]
                    | Some p when not (p.Equals(null)) ->
                        try
                            await (p.AddReactionAsync(Emoji("🌟")))
                        with _ -> ()
                        [ ctx.sendOK None "Message successfully added to hall of fames" ]
                    | _ -> []
            else
                [ ctx.sendWarn None "This command is expecting a number (message id)" ]
    }