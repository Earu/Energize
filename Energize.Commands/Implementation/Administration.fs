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

    [<AdminOnlyCommand>]
    [<GuildOnlyCommand>]
    [<CommandParameters(2)>]
    [<Command("op", "Makes a user able or unable to use admin commands", "op <user|userid>,<0|1>")>]
    let op (ctx : CommandContext) = async {
        match findUser ctx ctx.arguments.[0] true with
        | Some user ->
            try
                let guser = user :?> SocketGuildUser
                let role = getOrCreateRole guser "EnergizeAdmin"
                let out = ref 0
                if Int32.TryParse(ctx.arguments.[1], out) then
                    if out.Value.Equals(0) then
                        await (guser.RemoveRoleAsync(role))
                        let display = sprintf "User %s was succesfully demoted" (user.ToString())
                        ctx.sendOK None display
                    else
                        await (guser.AddRoleAsync(role))
                        let display = sprintf "User %s was succesfully promoted" (user.ToString())
                        ctx.sendOK None display
                else
                    ctx.sendWarn None "Incorrect value as second argument (Needs to be 0 or 1)"
            with _ ->
                let display = 
                    sprintf "There was an issue when promoting %s, it is most likely due to missing permissions" (user.ToString())
                ctx.sendWarn None display
        | None ->
            ctx.sendWarn None "Could not find any user for your input"
    }

    let clearCmdBase (ctx : CommandContext) (predicate : IMessage -> bool) =
        let amount =
            let out = ref 0
            if Int32.TryParse(ctx.arguments.[0], out) then
                Math.Clamp(out.Value, 1, 100)
            else
                10
        let msgsAsync = ctx.message.Channel.GetMessagesAsync(100, CacheMode.AllowDownload)
        let msgs = (msgsAsync :?> IAsyncEnumerable<IEnumerable<IMessage>>).Flatten() :?> IEnumerable<IMessage> |> Seq.toList
        let toDelete = 
            (if msgs.Length > amount then msgs.[..amount] else msgs) 
            |> List.filter predicate
        try
            let chan = ctx.message.Channel :?> ITextChannel
            await (chan.DeleteMessagesAsync(toDelete))
            ctx.sendOK None (sprintf "Cleared %d messages among %d checked messages" toDelete.Length msgs.Length)
        with _ ->
            ctx.sendWarn None "There was an issue when deleting messages, it is most likely due to missing permissions"

    [<AdminOnlyCommand>]
    [<GuildOnlyCommand>]
    [<Command("clear", "Clear the bot messages and commands", "clear <amounttoremove|nothing>")>]
    let clear (ctx : CommandContext) = async {
        clearCmdBase ctx (fun msg -> 
            let diff = (DateTime.Now.Date.Ticks - msg.CreatedAt.Date.Ticks)
            let old = diff > (DateTime()).AddDays(15.0).Ticks
            if not old then
                msg.Author.Id.Equals(Config.BOT_ID_MAIN) 
            else
                false
        )
    }

