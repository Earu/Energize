namespace Energize.Commands

open Command
open Discord.WebSocket
open System.Text.RegularExpressions
open Discord
open Cache
open Discord.Rest
open Context
open System.Threading.Tasks
open System
open AsyncHelper
open Energize.Essentials
open System.Reflection
open Energize.Interfaces.Services
open System.Diagnostics
open Energize.Interfaces.Services.Senders
open System.Collections.Generic
open Energize.Interfaces.Services.Listeners

[<CommandModule("Core")>]
module CommandHandler =
    type private CommandHandlerState =
        {
            client : DiscordShardedClient
            restClient : DiscordRestClient
            caches : Map<uint64, CommandCache>
            commands : Map<string, Command>
            logger : Logger
            messageSender : MessageSender
            prefix : string
            separator: char
            serviceManager : IServiceManager
            commandCache : (uint64 * IUserMessage list) list
        }

    // I had to.
    let mutable private handlerState : CommandHandlerState option = None

    let private registerCmd (state : CommandHandlerState) (cmd : Command) =
        Some { state with commands = state.commands.Add (cmd.name, cmd) }

    let private postCmdHelp (cmd : Command) (ctx : CommandContext) (iswarn : bool) =
        let fields = [
            ctx.embedField "Usage" (sprintf "`%s`" cmd.usage) true
            ctx.embedField "Help" (sprintf "`%s`" cmd.help) true
        ]
        let builder = EmbedBuilder()
        builder
            .WithAuthorNickname(ctx.message)
            .WithFields(fields)
            .WithColorType(if iswarn then EmbedColorType.Warning else EmbedColorType.Good)
            .WithFooter(sprintf (if iswarn then "bad usage [ %s ]" else "help [ %s ]") cmd.name)
            |> ignore
        ctx.sendEmbed (builder.Build())

    [<Command("help", "This command", "help <cmd|nothing>")>]
    let help (ctx : CommandContext) = async {
        return
            if ctx.hasArguments then
                let cmdName = ctx.arguments.[0].Trim()
                match handlerState.Value.commands |> Map.tryFind cmdName with
                | Some cmd ->
                    [ postCmdHelp cmd ctx false ]
                | None ->
                    let matchingCmds =
                        handlerState.Value.commands 
                        |> Map.toList 
                        |> List.map (fun (cmd, _) -> 
                            let mutable score1 : int32 = 0
                            let mutable score2 : int32 = 0
                            cmd.FuzzyMatch(cmdName, &score1) |> ignore
                            cmdName.FuzzyMatch(cmd, &score2) |> ignore
                            if score1 > score2 then (cmd, score1) else (cmd, score2)
                        ) |> List.filter(fun (_, score) -> score > 0)
                        |> List.sortBy (fun (_, score) -> score) 
                        |> List.rev
                        |> List.map (fun (cmd, _) -> sprintf "`%s`" cmd)
                    if matchingCmds.Length > 0 then
                        let cmdsDisplay = String.Join('\n', matchingCmds)
                        let warning = 
                            sprintf "Could not find any command named `%s`, did you mean one of the following:\n%s\n\nFind out more at **%s**, or join our server **%s**" 
                                cmdName cmdsDisplay Config.Instance.URIs.WebsiteURL Config.Instance.URIs.DiscordURL
                        [ ctx.sendWarn None warning ]
                    else
                        let warning = 
                            sprintf "Could not find any command named `%s`, find out more at **%s**, or join our server **%s**" 
                                cmdName Config.Instance.URIs.WebsiteURL Config.Instance.URIs.DiscordURL
                        [ ctx.sendWarn None warning ]
            else
                let paginator = ctx.serviceManager.GetService<IPaginatorSenderService>("Paginator")
                let commands = 
                    handlerState.Value.commands 
                    |> Map.toSeq 
                    |> Seq.groupBy (fun (_, cmd) -> cmd.moduleName) 
                    |> Seq.filter (fun (name, _) -> not (name.Equals("Deprecated"))) 
                    |> Seq.sortBy (fun (name, _) -> name)
                [ awaitResult (paginator.SendPaginatorAsync(ctx.message, ctx.commandName, commands, Action<string * seq<string * Command>, EmbedBuilder>(fun (moduleName, cmds) builder ->
                    let cmdsDisplay = 
                        cmds |> Seq.map (fun (cmdName, _) -> sprintf "`%s`" cmdName)
                    let tip = StaticData.Instance.Tips.[ctx.random.Next(0, StaticData.Instance.Tips.Count)]
                    builder.WithFields([
                        ctx.embedField "Documentation" (sprintf "**%s**" Config.Instance.URIs.WebsiteURL) false
                        ctx.embedField "Support Server" (sprintf "**%s**" Config.Instance.URIs.DiscordURL) false
                        ctx.embedField moduleName (String.Join(',', cmdsDisplay)) false
                        ctx.embedField "Tip" tip false
                    ])
                    |> ignore
                ))) ]
    }

    [<MaintenanceFreeCommand>]
    [<CommandConditions(CommandCondition.DevOnly)>]
    [<Command("maintenance", "Triggers or stops a maintenance on the bot", "maintenance <nothing>")>]
    let maintenance (ctx : CommandContext) = async {
        return
            if Config.Instance.Maintenance then
                Config.Instance.Maintenance <- false
                let prefix = Config.Instance.Discord.Prefix
                let game = StreamingGame(sprintf "%shelp | %sinfo | %sdocs" prefix prefix prefix, Config.Instance.URIs.TwitchURL)
                await (ctx.client.SetActivityAsync(game))
                ctx.logger.Warning("STOPPED MAINTENANCE\n\n")
                [ ctx.sendWarn None "Stopped the on-going maintenance" ]
            else
                Config.Instance.Maintenance <- true
                let game = StreamingGame("maintenance", Config.Instance.URIs.TwitchURL)
                await (ctx.client.SetActivityAsync(game))
                let music = ctx.serviceManager.GetService<IMusicPlayerService>("Music")
                await (music.DisconnectAllPlayersAsync("A maintenance started, disconnecting"))
                ctx.logger.Warning("STARTED MAINTENANCE\n\n")
                [ ctx.sendWarn None "Triggered a maintenance" ]
    }

    let private enableCmd (state : CommandHandlerState) (cmdName : string) (enabled : bool) = 
        match state.commands |> Map.tryFind cmdName with
        | Some cmd -> cmd.isEnabled <- enabled
        | None -> ()
    
    [<MaintenanceFreeCommand>]
    [<CommandParameters(2)>]
    [<CommandConditions(CommandCondition.DevOnly)>]
    [<Command("enable", "Enables or disables a command", "enable <cmd>")>]
    let enable (ctx : CommandContext) = async {
        let cmdName = ctx.arguments.[0].Trim()
        let value = int (ctx.arguments.[1].Trim())
        return
            match handlerState with
            | Some state ->
                if state.commands |> Map.containsKey cmdName then
                    if value.Equals(0) then
                        enableCmd state cmdName false
                        [ ctx.sendOK None (sprintf "Successfully disabled command `%s`" cmdName) ]
                    else
                        enableCmd state cmdName true
                        [ ctx.sendOK None (sprintf "Successfully enabled command `%s`" cmdName) ]
                else
                    [ ctx.sendWarn None (sprintf "Could not find any command named `%s`" cmdName) ]
            | None -> []
    }

    [<MaintenanceFreeCommand>]
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
            .WithLimitedDescription(feedback)
            .WithTimestamp(ctx.message.CreatedAt)
            .WithField("Channel ID", ctx.message.Channel.Id, false)
            .WithFooter(log)
            |> ignore

        match chan :> IChannel with
        | :? ITextChannel as textChan ->
            awaitIgnore (sender.SendEmbedAsync(textChan, builder.Build(), name, avatar))
        | _ ->
            ctx.logger.Warning("Feedback channel wasnt a text channel?!")

        return [ ctx.sendOK None "Successfully sent your feedback" ]
    }

    [<MaintenanceFreeCommand>]
    [<CommandParameters(1)>]
    [<Command("bug", "Report a bug to the developer","bug <sentence>")>]
    let bug (ctx : CommandContext) = feedback ctx

    [<MaintenanceFreeCommand>]
    [<CommandParameters(2)>]
    [<CommandConditions(CommandCondition.DevOnly)>]
    [<Command("sendmsg", "Send a message to a specified channnel", "sendmsg <channelid> <sentence>")>]
    let sendMsg (ctx : CommandContext) = async {
        return
            try
                let chan = ctx.client.GetChannel(uint64 ctx.arguments.[0])
                match chan |> Option.ofObj with
                | None -> 
                    [ ctx.sendWarn None "Could not find a channel for the specified ID" ]
                | Some chan ->
                    let header = "dev message (answer with the bug or feedback commands)"
                    awaitIgnore (ctx.messageSender.SendGoodAsync(chan, header, String.Join(Config.Instance.Discord.Separator, ctx.arguments.[1..])))
                    [ ctx.sendOK None "Message sent successfully" ]
            with ex ->
                printfn "%s" (ex.ToString())
                [ ctx.sendWarn None "Expected a channel ID" ]
    }

    let private loadCmd (state : CommandHandlerState) (callback : CommandCallback) (moduleType : Type) =
        let infoAtr = callback.Method.GetCustomAttribute<CommandAttribute>()
        let moduleAtr = moduleType.GetCustomAttribute<CommandModuleAttribute>()
        let paramAtr = callback.Method.GetCustomAttributes<CommandParametersAttribute>() |> Seq.tryHead
        let permsAtr = callback.Method.GetCustomAttributes<CommandPermissionsAttribute>() |> Seq.tryHead
        let condsAtr = callback.Method.GetCustomAttributes<CommandConditionsAttribute>() |> Seq.tryHead
        let maintAtr = callback.Method.GetCustomAttributes<MaintenanceFreeCommand>() |> Seq.tryHead
        let paramCount = match paramAtr with Some atr -> atr.parameters | None -> 0
        let permissions = match permsAtr with Some atr -> atr.permissions | None -> []
        let conditions = match condsAtr with Some atr -> atr.conditions | None -> []
        let maintFree = match maintAtr with Some atr -> true | None -> false

        let cmd : Command =
            {
                name = infoAtr.name
                callback = callback
                isEnabled = true
                usage = infoAtr.usage
                help = infoAtr.help
                moduleName = moduleAtr.name
                parameters = paramCount
                permissions = permissions
                conditions = conditions
                maintenanceFree = maintFree
            }

        registerCmd state cmd

    let private loadCmds (state : CommandHandlerState) =
        let moduleTypes = 
            Assembly.GetExecutingAssembly().GetTypes() 
            |> Seq.rev
            |> Seq.filter 
                (fun t ->
                    let atr = t.GetCustomAttributes<CommandModuleAttribute>() |> Seq.tryHead
                    if t.FullName.StartsWith("Energize.Commands") && atr.IsSome then
                        state.logger.Nice("Commands", ConsoleColor.Green, sprintf "Registered command module [ %s ]" atr.Value.name)
                        true
                    else
                        false
                )

        for moduleType in moduleTypes do
            let funcs = moduleType.GetMethods() |> Seq.filter (fun func -> Attribute.IsDefined(func, typedefof<CommandAttribute>))
            try 
                for func in funcs do
                    let dlg = func.CreateDelegate(typedefof<CommandCallback>) :?> CommandCallback
                    handlerState <- loadCmd (match handlerState with Some s -> s | None -> state) dlg moduleType
            with ex ->
                state.logger.Danger(ex.ToString())

    let Initialize (client : DiscordShardedClient) (restClient : DiscordRestClient) (logger : Logger) 
        (messageSender : MessageSender) (serviceManager : IServiceManager) =
        let newState : CommandHandlerState =
            {
                client = client
                restClient = restClient
                caches = Map.empty
                commands = Map.empty
                logger = logger
                messageSender = messageSender
                prefix = Config.Instance.Discord.Prefix
                separator = Config.Instance.Discord.Separator
                serviceManager = serviceManager
                commandCache = List.empty
            }
        
        logger.Nice("Commands", ConsoleColor.Yellow, sprintf "Registering commands with prefix \'%s\'" newState.prefix)
        loadCmds newState
        logger.Notify("Commands Initialized")

    let private getChannelCache (state : CommandHandlerState) (id : uint64) : CommandCache =
        match state.caches |> Map.tryFind id with
        | Some cache ->
            cache
        | None ->
            let cache = 
                {
                    lastImageUrl = None
                    lastMessage = None
                    lastDeletedMessage = None
                }
            let newCaches = state.caches |> Map.add id cache
            handlerState <- Some { state with caches = newCaches }
            cache

    let private startsWithBotMention (state : CommandHandlerState) (input : string) : bool =
        match state.client.CurrentUser |> Option.ofObj with
        | None -> false
        | Some user -> Regex.IsMatch(input,"^<@!?" + user.Id.ToString() + ">")

    let private getPrefixLength (state : CommandHandlerState) (input : string) : int =
        if startsWithBotMention state input then
            state.client.CurrentUser.Mention.Length
        else
            state.prefix.Length

    let private sanitizeInput (input : string) =
        input
            .Replace('\n', ' ')
            .Replace('\t', ' ')
    
    let private getCmdName (state : CommandHandlerState) (input : string) : string =
        let offset = getPrefixLength state input
        if offset >= input.Length then String.Empty else (sanitizeInput input).[offset..].Split(' ').[0]

    let private getCmdArgs (state : CommandHandlerState) (input : string) : string list =
        let offset = (getPrefixLength state input) + (getCmdName state input).Length
        let argInputs = input.[offset..].Trim() |> sanitizeInput
        
        //TODO: Convert this to a more F# idiomatic algorithm
        let mutable args = []
        let mutable inArg = false
        let mutable curArg = String.Empty
        for i in [0..argInputs.Length - 1] do
            let char = argInputs.[i]
            if char.Equals('"') then
                inArg <- not inArg
            else
                curArg <- sprintf "%s%c" curArg char

            if (not inArg && char.Equals(state.separator)) || i.Equals(argInputs.Length - 1) then
                let arg = curArg.Trim()
                if not (arg |> String.IsNullOrWhiteSpace) then
                    args <- [ arg ] |> List.append args
                curArg <- String.Empty

        args

    let private buildCmdContext (state : CommandHandlerState) (cmdName : string) (msg : SocketMessage) (args : string list)
        (isPrivate : bool): CommandContext =
        let users = 
            if isPrivate then
                List.empty
            else
                let chan = msg.Channel :?> IGuildChannel
                seq { for u in (chan.Guild :?> SocketGuild).Users -> u :> IGuildUser }
                |> Seq.toList
        {
            client = state.client
            restClient = state.restClient
            message = msg
            prefix = state.prefix
            arguments = args
            cache = getChannelCache state msg.Channel.Id
            messageSender = state.messageSender
            logger = state.logger
            isPrivate = isPrivate
            commandName = cmdName
            serviceManager = state.serviceManager
            random = Random()
            guildUsers = users
            commandCount = state.commands.Count
        }

    let private registerCmdCacheEntry (msgId : uint64) (msgs : IUserMessage list) =
        match handlerState with
        | Some state ->
            let filterMsg msg = match msg with null -> false | _ -> true 
            let newCommandCache = 
                let cache = (msgId, msgs |> List.filter filterMsg) :: state.commandCache
                if cache.Length > 50 then cache.[..50] else cache
            handlerState <- Some { state with commandCache = newCommandCache }
        | None -> ()
    
    let private reportCmdError (state : CommandHandlerState) (ex : exn) (msg : SocketMessage) (cmd : Command) (input : string) =
        let realEx = match ex.InnerException |> Option.ofObj with None -> ex | Some exIn -> exIn
        state.logger.Warning(realEx.ToString())
        
        let caseId = Guid.NewGuid()
        let err = 
            (sprintf "Something went wrong when using `%s` a report has been sent.\n" cmd.name)
            + "If you wish to contact the developer use the `bug` or `feedback` commands, don't forget to mention your case id!" 
        let msgs = [ awaitResult (state.messageSender.SendWarningAsync(msg, sprintf "command error | case id: %s" (caseId.ToString()), err)) ]
        registerCmdCacheEntry msg.Id msgs
        
#if !DEBUG 
        let args = input.Trim()
        let argDisplay = if String.IsNullOrWhiteSpace args then "none" else args
        let frame = StackTrace(realEx, true).GetFrame(0)
        let source = sprintf "@File: %s | Method: %s | Line: %d" (frame.GetFileName()) (frame.GetMethod().Name) (frame.GetFileLineNumber())
        let builder = EmbedBuilder()
        builder
            .WithTitle("Bug Report")
            .WithField("User", msg.Author)
            .WithField("Channel ID", msg.Channel.Id)
            .WithField("Command", cmd.name)
            .WithField("Arguments", argDisplay)
            .WithField("Error", realEx.Message)
            .WithField("Case ID", caseId)
            .WithTimestamp(msg.CreatedAt)
            .WithFooter(source)
            .WithColorType(EmbedColorType.Warning)
            |> ignore
        match state.client.GetChannel(Config.Instance.Discord.BugReportChannelID) |> Option.ofObj with
        | None -> ()
        | Some c ->
            let chan = c :> IChannel :?> ITextChannel
            awaitIgnore (state.messageSender.SendAsync(chan, builder.Build())) 
#endif

    let private handleTimeOut (state : CommandHandlerState) (msg : SocketMessage) (cmd : Command) (ctx : CommandContext) : Task<Task> =
        async {
            let asyncOp = cmd.callback.Invoke(ctx)
            let tcallback = (toTaskResult asyncOp).ContinueWith(fun (t : Task<_>) -> 
                if t.Status.Equals(TaskStatus.Faulted) then  
                    reportCmdError state t.Exception msg cmd ctx.input
                else
                    registerCmdCacheEntry msg.Id (awaitResult t)
            )
            if not tcallback.IsCompleted then
                let tres = awaitResult (Task.WhenAny(tcallback, Task.Delay(10000)))
                if not tcallback.IsCompleted then
                    awaitResult (state.messageSender.SendWarningAsync(msg, "time out", sprintf "Your command `%s` is timing out!" cmd.name)) |> ignore
                    state.logger.Nice("Commands", ConsoleColor.Yellow, sprintf "Time out of command <%s>" cmd.name)
                return tres
            else
                return Task.CompletedTask
        } |> toTaskResult

    let private logCmd (ctx : CommandContext) =
        let color = if ctx.isPrivate then ConsoleColor.Blue else ConsoleColor.Cyan
        let head = if ctx.isPrivate then "DMCommands" else "Commands"
        let action = "used"
        let where = 
            if not ctx.isPrivate then
                let chan = ctx.message.Channel :?> IGuildChannel
                (sprintf "(%s - #%s) " chan.Guild.Name chan.Name)
            else 
                String.Empty 
        let cmdLog = sprintf "%s %s <%s>" ctx.message.Author.Username action ctx.commandName
        let args = if ctx.hasArguments then (sprintf " => [ %s ]" (String.Join(", ", ctx.arguments))) else " with no args"
        ctx.logger.Nice(head, color, where + cmdLog + args)

    let private runCmd (state : CommandHandlerState) (msg : SocketMessage) (cmd : Command) (input : string) (isPrivate : bool) =
        await (state.messageSender.TriggerTypingAsync(msg.Channel))
        let args = getCmdArgs state input
        let ctx = buildCmdContext state cmd.name msg args isPrivate
        if args.Length >= cmd.parameters then
            let task = handleTimeOut state msg cmd ctx
            task.ConfigureAwait(false) |> ignore
            logCmd ctx
        else
            let msgs = [ postCmdHelp cmd ctx true ]
            registerCmdCacheEntry msg.Id msgs
            logCmd ctx
    
    let private hasPermissions (msg : SocketMessage) (cmd : Command) =
        if cmd.permissions.Length > 0 then
            if Context.isPrivate msg then
                (true, [])
            else
                let botPerms =  
                    match msg.Channel with
                    | :? SocketGuildChannel as chan -> 
                        let botUser = chan.Guild.CurrentUser
                        botUser.GetPermissions(chan)
                    | _ ->
                        let chan = msg.Channel :?> IGuildChannel
                        let botUser = awaitResult (chan.Guild.GetCurrentUserAsync())
                        botUser.GetPermissions(chan)
                let hasAllPerms = cmd.permissions |> List.forall (fun perm -> botPerms.Has(perm))
                let missingPerms = cmd.permissions |> List.filter (fun perm -> not (botPerms.Has(perm)))
                (hasAllPerms, missingPerms)
        else
            (true, [])

    let private hasConditions (msg : SocketMessage) (cmd : Command) =
        if cmd.conditions.Length > 0 then
            let conds = 
                cmd.conditions |> List.map (fun cond ->
                    let valid = 
                        match cond with
                        | CommandCondition.DevOnly ->
                            msg.Author.Id.Equals(Config.Instance.Discord.OwnerID)
                        | CommandCondition.GuildOnly ->
                            not (Context.isPrivate msg)
                        | CommandCondition.NsfwOnly ->
                            Context.isNSFW msg
                        | CommandCondition.AdminOnly ->
                            Context.isAuthorAdmin msg
                        | _ -> false
                    (cond, valid)
                )
            let hasAllConditions = conds |> List.forall (fun (_, isvalid) -> isvalid)
            let missingConds = conds |> List.filter (fun (perm, isvalid) -> not isvalid) |> List.map (fun (perm, _) -> perm)
            (hasAllConditions, missingConds)
        else
            (true, [])

    let private handleCmd (state : CommandHandlerState) (msg : SocketMessage) (cmd : Command) (input : string) =
        let author = msg.Author.ToString()
        let hasPermissions, missingPerms = hasPermissions msg cmd
        let hasConditions, missingConds = hasConditions msg cmd
        match cmd with
        | cmd when Config.Instance.Maintenance && not cmd.maintenanceFree -> () //discard
        | cmd when not (cmd.isEnabled) ->
            state.logger.Nice("Commands", ConsoleColor.Red, sprintf "%s tried to use a disabled command <%s>" author cmd.name)
            let warnMsg = awaitResult (state.messageSender.SendWarningAsync(msg, "disabled command", "This is a disabled feature for now")) 
            registerCmdCacheEntry msg.Id [ warnMsg ]
        | cmd when not hasPermissions ->
            state.logger.Nice("Commands", ConsoleColor.Red, sprintf "%s tried to use a command with missing permissions <%s>" author cmd.name)
            let permDisplay = String.Join(", ", missingPerms |> List.map (fun perm -> sprintf "`%s`" (perm.ToString())))
            let warnMsg = awaitResult (state.messageSender.SendWarningAsync(msg, "missing permissions", sprintf "Missing the following permissions:\n%s" permDisplay))
            registerCmdCacheEntry msg.Id [ warnMsg ]
        | cmd when not hasConditions ->
            state.logger.Nice("Commands", ConsoleColor.Red, sprintf "%s tried to use a command with unmet conditions <%s>" author cmd.name)
            let condDisplay = String.Join(", ", missingConds |> List.map (fun cond -> sprintf "`%s`" (cond.ToString())))
            let warnMsg = awaitResult (state.messageSender.SendWarningAsync(msg, "unmet conditions", sprintf "The following conditions were not met:\n%s" condDisplay))
            registerCmdCacheEntry msg.Id [ warnMsg ]
        | cmd ->
            runCmd state msg cmd input (Context.isPrivate msg)

    let private deleteCmdMsgs (cmdMsgId : uint64) = 
        match handlerState with
        | Some state ->
            match state.commandCache |> List.tryFind (fun (id, _) -> cmdMsgId.Equals(id)) with
            | Some (id, msgs) -> 
                try
                    seq { for msg in msgs -> match msg |> Option.ofObj with None -> Task.CompletedTask | Some msg -> msg.DeleteAsync() } 
                    |> Task.WhenAll 
                    |> await
                with _ -> ()
                let newCmdCache = state.commandCache |> List.except [ (id, msgs) ]
                handlerState <- Some { state with commandCache = newCmdCache }
            | None -> ()
        | None -> ()

    let private isBlacklisted (id : uint64) =
        let ids = Config.Instance.Discord.Blacklist.IDs |> Seq.toList
        match ids |> List.tryFind (fun i -> i.Equals(id)) with
        | Some _ -> true
        | None -> false

    let HandleMessageDeleted (cache : Cacheable<IMessage, uint64>) (chan : ISocketMessageChannel) =
        match handlerState with
        | Some state when cache.HasValue && not (isBlacklisted cache.Value.Author.Id) ->
            let oldCache = getChannelCache state chan.Id
            let newCache = { oldCache with lastDeletedMessage = Some (cache.Value :?> SocketMessage) }
            let newCaches = state.caches.Add(chan.Id, newCache)
            handlerState <- Some { state with caches = newCaches }
            deleteCmdMsgs cache.Id
        | Some _ -> deleteCmdMsgs cache.Id
        | _ -> printfn "COMMAND HANDLER WAS NOT INITIALIZED ??!"

    let private updateChannelCache (msg : SocketMessage) (cb : CommandCache -> CommandCache) =
        match handlerState with
        | Some state ->
            let oldCache = getChannelCache state msg.Channel.Id
            let newCache = cb oldCache
            let newCaches = state.caches.Add(msg.Channel.Id, newCache)
            handlerState <- Some { state with caches = newCaches }
        | None -> ()
     
    let private findAndRunCmd (state : CommandHandlerState) (msg : SocketMessage) (content : string) (botMention : bool) =
        let cmdName = getCmdName state content
        match state.commands |> Map.tryFind cmdName with
        | Some cmd -> handleCmd state msg cmd content
        | None when botMention -> 
            let showCmd cmdName = state.prefix + cmdName
            let helper =
                sprintf "Hey there %s, looking for something? Use `%s` or `%s`, visit the online documentation or join our server!\n%s\n%s" 
                    msg.Author.Mention (showCmd "help") (showCmd "info") Config.Instance.URIs.WebsiteURL Config.Instance.URIs.DiscordURL
            let helpMsg = awaitResult (state.messageSender.SendRawAsync(msg, helper))
            registerCmdCacheEntry msg.Id [ helpMsg ]
        | None -> ()

    let HandleMessageReceived (msg : SocketMessage) =
        match handlerState with
        | Some state when not (isBlacklisted msg.Author.Id) ->
            updateChannelCache msg (fun oldCache -> 
                let lastUrl = match ImageUrlProvider.getLastImgUrl msg with Some url -> Some url | None -> oldCache.lastImageUrl
                let lastMsg = if msg.Author.IsBot then oldCache.lastMessage else Some msg
                { oldCache with lastImageUrl = lastUrl; lastMessage = lastMsg }
            )
            match msg.Content with
            | content when msg.Author.IsBot || msg.Author.IsWebhook || content |> String.IsNullOrWhiteSpace -> () // to the trash it goes
            | content when startsWithBotMention state content ->
                findAndRunCmd state msg content true
            | content when content.ToLower().StartsWith(state.prefix) ->
                findAndRunCmd state msg content false
            | _ -> ()
        | Some _ -> ()
        | None -> printfn "COMMAND HANDLER WAS NOT INITIALIZED ??!"

    let private canMsgUpdate (cache : Cacheable<IMessage, uint64>) (msg : SocketMessage) (chan : ISocketMessageChannel) =
        match chan with
        | :? SocketGuildChannel as guildChan ->
            let botUser = guildChan.Guild.CurrentUser
            if botUser.GetPermissions(guildChan).Has(ChannelPermission.ReadMessageHistory) then
                match awaitResult (cache.GetOrDownloadAsync()) |> Option.ofObj with
                | None -> false
                | Some oldMsg when not (oldMsg.Content.Equals(msg.Content)) -> true
                | _ -> false
            else
                false
        | _ -> true

    let HandleMessageUpdated (cache : Cacheable<IMessage, uint64>) (msg : SocketMessage) (chan : ISocketMessageChannel) =
        match handlerState with
        | Some _ when not (isBlacklisted msg.Author.Id) && canMsgUpdate cache msg chan ->
            let diff = DateTime.Now.ToUniversalTime() - msg.Timestamp.DateTime
            if diff.TotalHours < 1.0 then
                deleteCmdMsgs msg.Id
                HandleMessageReceived msg
        | Some _ -> ()
        | None -> printfn "COMMAND HANDLER WAS NOT INITIALIZED ??!"

    let private toDictionary (map : Map<_, _>) : Dictionary<_, _> =
        let dict = new Dictionary<_, _>()
        map |> Map.iter (fun k v -> dict.Add(k, v))
        dict

    // Useless param because otherwise not considered a method C# wise
    let GetRegisteredCommands (name : string) =
        match handlerState with
        | Some state -> 
            match name |> Option.ofObj with
            | None -> state.commands |> toDictionary
            | Some name -> 
                match state.commands |> Map.tryFind name with
                | Some cmd -> (Map.add name cmd Map.empty) |> toDictionary
                | None -> Map.empty |> toDictionary
        | None -> Map.empty |> toDictionary