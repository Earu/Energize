namespace Energize.Commands.Implementation

open Energize.Commands.Command
open Energize.Commands.Context
open Energize.Commands.UserHelper
open Discord.WebSocket
open Energize.Commands.AsyncHelper
open System
open Discord
open System.Collections.Generic
open Energize.Commands
open System.Linq
open Energize.Interfaces.Services.Database

[<CommandModule("Administration")>]
module Administration =
    [<CommandParameters(2)>]
    [<CommandPermissions(ChannelPermission.ManageRoles)>]
    [<CommandConditions(CommandCondition.AdminOnly, CommandCondition.GuildOnly)>]
    [<Command("op", "Makes a user able or unable to use admin commands", "op <user|userid> <0|1>")>]
    let op (ctx : CommandContext) = async {
        return 
            match findUser ctx ctx.arguments.[0] true with
            | Some user ->
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
            | None ->
                [ ctx.sendWarn None "Could not find any user for your input" ]
    }

    [<CommandParameters(1)>]
    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<Command("isop", "Shows if a user is an admin", "isadmin <user|userid>")>]
    let isAdmin (ctx : CommandContext) = async {
        return 
            match findUser ctx ctx.arguments.[0] true with
            | Some user  ->
                let isAdmin = Context.isAdmin (user :?> SocketUser)
                let res = sprintf "%s is %san administrator" (user.ToString()) (if isAdmin then String.Empty else "not ")
                [ ctx.sendOK None res ]
            | None ->
                [ ctx.sendWarn None "No user could be found for your input" ]
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

        let chan = ctx.message.Channel :?> ITextChannel
        await (chan.DeleteMessagesAsync(toDelete))
        [ ctx.sendOK None (sprintf "Cleared %d messages among %d checked messages" toDelete.Length msgs.Length) ]

    let private clearTypes = [
        "bots", (fun (ctx : CommandContext) -> clearCmdBase ctx ctx.arguments.[1] (fun msg -> msg.Author.IsBot));
        "raw", (fun (ctx : CommandContext) -> clearCmdBase ctx ctx.arguments.[1] (fun _ -> true));
        "user", (fun (ctx : CommandContext) ->
            if ctx.arguments.Length < 3 then
                clearCmdBase ctx ctx.arguments.[1] (fun msg -> not msg.Author.IsBot && not msg.Author.IsWebhook)
            else
                if String.IsNullOrWhiteSpace ctx.arguments.[2] then
                    [ ctx.sendWarn None "Expected a username as 3rd argument" ]
                else
                    match findUser ctx ctx.arguments.[2] true with
                    | Some user ->
                        clearCmdBase ctx ctx.arguments.[1] (fun msg -> msg.Author.Id.Equals(user.Id))
                    | None ->   
                        [ ctx.sendWarn None "No user could be found for your input" ]
        );
    ]

    [<CommandParameters(2)>]
    [<CommandPermissions(ChannelPermission.ManageMessages)>]
    [<CommandConditions(CommandCondition.AdminOnly, CommandCondition.GuildOnly)>]
    [<Command("clear", "Clear a specified amount of messages in the current channel", "clear <cleartype> <amounttoremove> <extra>")>]
    let clear (ctx : CommandContext) = async {
        let cb = clearTypes |> List.tryFind (fun (_type, _) -> _type.Equals(ctx.arguments.[0])) 
        return
            match cb with
            | None ->
                let typeDisplay = String.Join(", ", clearTypes |> List.map (fun (name, _) -> sprintf "`%s`" name))
                [ ctx.sendWarn None (sprintf "Unknown clear type, available types are:\n%s" typeDisplay) ] 
            | Some (_, cb) -> cb ctx
    }

    [<CommandPermissions(ChannelPermission.ManageMessages)>]
    [<CommandConditions(CommandCondition.AdminOnly, CommandCondition.GuildOnly)>]
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