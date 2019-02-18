namespace Energize.Commands

module CommandHandler =
    open Discord.WebSocket
    open ImageUrlProvider
    open System.Text.RegularExpressions
    open Discord
    open Cache
    open Discord.Rest
    open Context
    open System.Threading.Tasks
    open System
    open AsyncHelper
    open Command
    open Energize.Toolkit
    open System.Reflection
    open Energize.Interfaces.Services
    open System.IO
    open System.Text

    type CommandHandlerState =
        {
            client : DiscordShardedClient
            restClient : DiscordRestClient
            caches : Map<uint64, CommandCache>
            globalCache : CommandCache
            commands : Map<string, Command>
            logger : Logger
            messageSender : MessageSender
            prefix : string
            serviceManager : IServiceManager
        }

    // I had to.
    let mutable private handlerState : CommandHandlerState option = None

    let private registerCmd (state : CommandHandlerState) (cmd : Command) =
        state.logger.Nice("Commands", ConsoleColor.Green, sprintf "Registered <%s> in module [%s]" cmd.name cmd.moduleName)
        Some { state with commands = state.commands.Add (cmd.name, cmd) }

    let private getCmdInfo (cmd : Command) : string =
        sprintf "**USAGE:**\n``%s``\n**HELP:**\n``%s``" cmd.usage cmd.help

    let generateHelpFile (state : CommandHandlerState) (path : string) =
        for cmd in state.commands do
            let lines = [
                sprintf "--------- %s ---------\n" cmd.Key
                sprintf "usage: %s\n" cmd.Value.usage
                sprintf "help: %s\n" cmd.Value.help
                sprintf "owner-only: %b\n" cmd.Value.ownerOnly
            ]
            await (File.AppendAllLinesAsync(path, lines))

    let private registerHelpCmd (state : CommandHandlerState) =
        let cmd : Command = 
            {
                name = "help"
                callback = CommandCallback(fun ctx -> async {
                    if ctx.hasArguments then
                        let cmdName = ctx.arguments.[0].Trim()
                        match handlerState.Value.commands |> Map.tryFind cmdName with
                        | Some cmd ->
                            let help = getCmdInfo cmd
                            awaitIgnore (ctx.messageSender.Good(ctx.message, ctx.commandName, help))
                        | None ->
                            let warning = sprintf "Could not find any command named \'%s\'" cmdName
                            awaitIgnore (ctx.messageSender.Warning(ctx.message, ctx.commandName, warning))
                    else
                        let path = "help.txt"
                        if File.Exists(path) then
                            awaitIgnore (ctx.messageSender.SendFile(ctx.message, path))
                        else
                            generateHelpFile state path
                            awaitIgnore (ctx.messageSender.SendFile(ctx.message, path))
                            File.Delete(path)
                        ()
                })
                isEnabled = true
                usage = "help <cmd|nothing>"
                help = "This command."
                moduleName = "Core"
                parameters = 0
                ownerOnly = false
            }
        registerCmd state cmd

    let private enableCmd (state : CommandHandlerState) (cmdName : string) (enabled : bool) = 
        match state.commands |> Map.tryFind cmdName with
        | Some cmd ->
            cmd.isEnabled <- enabled
        | None -> ()

    let private registerEnableCmd (state : CommandHandlerState) =
        let cmd : Command = 
            {
                name = "enable"
                callback = CommandCallback(fun ctx -> async {
                    let cmdName = ctx.arguments.[0].Trim()
                    let value = int (ctx.arguments.[1].Trim())
                    if handlerState.Value.commands |> Map.containsKey cmdName then
                        if value.Equals(0) then
                            enableCmd state cmdName false
                            awaitResult (ctx.messageSender.Good(ctx.message, ctx.commandName, sprintf "Successfully disabled command \'%s\'" cmdName))
                                |> ignore
                        else
                            enableCmd state cmdName true
                            awaitResult (ctx.messageSender.Good(ctx.message, ctx.commandName, sprintf "Successfully enabled command \'%s\'" cmdName))
                                |> ignore
                    else
                        awaitResult (ctx.messageSender.Warning(ctx.message, ctx.commandName, sprintf "Could not find any command named \'%s\'" cmdName))
                            |> ignore
                })
                isEnabled = true
                usage = "enable <cmd>,<value>"
                help = "Enables or disables a command"
                moduleName = "Core"
                parameters = 2
                ownerOnly = true
            }
        registerCmd state cmd

    let private loadCmd (state : CommandHandlerState) (callback : CommandCallback) (moduleType : Type) =
        let infoAtr = callback.Method.GetCustomAttribute<CommandAttribute>()
        let moduleAtr = moduleType.GetCustomAttribute<CommandModuleAttribute>()
        
        let paramAtr = callback.Method.GetCustomAttributes<CommandParametersAttribute>() |> Seq.tryHead
        let paramCount = match paramAtr with Some atr -> atr.parameters | None -> 0
        let ownerAtr = callback.Method.GetCustomAttributes<OwnerOnlyCommandAttribute>() |> Seq.tryHead
        let ownerOnly = match ownerAtr with Some _ -> true | None -> false

        let cmd : Command =
            {
                name = infoAtr.name
                callback = callback
                isEnabled = true
                usage = infoAtr.usage
                help = infoAtr.help
                moduleName = moduleAtr.name
                parameters = paramCount
                ownerOnly = ownerOnly
            }

        registerCmd state cmd

    let private loadCmds (state : CommandHandlerState) =
        let moduleTypes = 
            Assembly.GetExecutingAssembly().GetTypes() 
                |> Seq.filter 
                    (fun t -> t.FullName.StartsWith("Energize.Commands.Implementation") && Attribute.IsDefined(t, typedefof<CommandModuleAttribute>))

        for moduleType in moduleTypes do
            let funcs = moduleType.GetMethods() |> Seq.filter (fun func -> Attribute.IsDefined(func, typedefof<CommandAttribute>))
            for func in funcs do
                let dlg = func.CreateDelegate(typedefof<CommandCallback>) :?> CommandCallback
                handlerState <- loadCmd (match handlerState with Some s -> s | None -> state) dlg moduleType

    let initialize (client : DiscordShardedClient) (restClient : DiscordRestClient) (logger : Logger) 
        (messageSender : MessageSender) (prefix : string) (serviceManager : IServiceManager) =
        let newState : CommandHandlerState =
            {
                client = client
                restClient = restClient
                caches = Map.empty
                globalCache = 
                    {
                        lastMessage = None
                        lastDeletedMessage = None
                        lastImageUrl = None
                        lastMessages = List.Empty
                        guildUsers = List.Empty
                    }
                commands = Map.empty
                logger = logger
                messageSender = messageSender
                prefix = prefix
                serviceManager = serviceManager
            }
        
        logger.Nice("Commands", ConsoleColor.Yellow, sprintf "Registering commands with prefix \'%s\'" prefix)
        loadCmds newState
        match handlerState with
        | Some s ->
            handlerState <- registerEnableCmd s
            handlerState <- registerHelpCmd handlerState.Value
        | None -> ()

        logger.Notify("Commands Initialized")

    let private getChannelCache (state : CommandHandlerState) (id : uint64) : CommandCache =
        match state.caches |> Map.tryFind id with
        | Some cache ->
            cache
        | None ->
            let cache = 
                {
                    lastMessage = None
                    lastDeletedMessage = None
                    lastImageUrl = None
                    lastMessages = List.Empty
                    guildUsers = List.Empty
                }
            let newCaches = state.caches |> Map.add id cache
            handlerState <- Some { state with caches = newCaches }
            cache

    let private startsWithBotMention (state : CommandHandlerState) (input : string) : bool =
        Regex.IsMatch(input,"^<@!?" + state.client.CurrentUser.Id.ToString() + ">")

    let private getPrefixLength (state : CommandHandlerState) (input : string) : int =
        if startsWithBotMention state input then
            state.client.CurrentUser.Mention.Length
        else
            state.prefix.Length

    let private getCmdName (state : CommandHandlerState) (input : string) : string =
        input.Substring(getPrefixLength state input).Split(' ').[0]

    let private getCmdArgs (state : CommandHandlerState) (input : string) : string list =
        let offset = (getPrefixLength state input) + ((getCmdName state input) |> String.length)
        let args = input.[offset..].TrimStart().Split(',') |> Array.toList
        if args.[0] |> String.IsNullOrWhiteSpace && (args |> List.length).Equals(1) then
            []
        else
            args
    
    let private buildCmdContext (state : CommandHandlerState) (cmdName : string) (msg : SocketMessage) (args : string list) : CommandContext =
        {
            client = state.client
            restClient = state.restClient
            message = msg
            prefix = state.prefix
            arguments = args
            cache = getChannelCache state msg.Channel.Id
            messageSender = state.messageSender
            logger = state.logger
            isPrivate = match msg.Channel with :? IDMChannel -> true | _ -> false
            commandName = cmdName
            serviceManager = state.serviceManager
        }

    let private handleTimeOut (state : CommandHandlerState) (msg : SocketMessage) (cmdName : string) (asyncOp : Async<unit>) : Task =
        let tcallback = toTask asyncOp
        if not tcallback.IsCompleted then
            async {
                let tres = awaitResult (Task.WhenAny(tcallback, Task.Delay(2000)))
                if not (tres.Equals(tcallback)) then
                    awaitResult (state.messageSender.Warning(msg, "Time out", sprintf "Your command %s is timing out!" cmdName)) |> ignore
                    state.logger.Nice("Commands", ConsoleColor.Yellow, sprintf "Time out of command <%s>" cmdName)
                
                return tres
            } |> awaitOp
        else
            Task.CompletedTask

    let private logCmd (ctx : CommandContext) (deleted : bool) =
        let color = 
            if deleted then ConsoleColor.Yellow else
                if ctx.isPrivate then ConsoleColor.Blue else ConsoleColor.Cyan

        let head = if ctx.isPrivate then "DMCommands" else "Commands"
        let action = if deleted then "deleted" else "used"
        let where = 
            if not ctx.isPrivate then
                let chan = ctx.message.Channel :?> IGuildChannel
                (sprintf "(%s - #%s) " chan.Guild.Name chan.Name)
            else 
                String.Empty 
        let cmdLog = sprintf "%s %s <%s>" ctx.message.Author.Username action ctx.commandName
        let args = if ctx.hasArguments then (sprintf " => [ %s ]" (String.Join(", ", ctx.arguments))) else " with no args"

        ctx.logger.Nice(head, color, where + cmdLog + args)

    let private runCmd (state : CommandHandlerState) (msg : SocketMessage) (cmd : Command) (input : string) =
        let args = getCmdArgs state input
        let ctx = buildCmdContext state cmd.name msg args
        if (args |> List.length) >= (cmd.parameters) then
            let task = handleTimeOut state msg cmd.name (cmd.callback.Invoke(ctx))
            task.ConfigureAwait(false) |> ignore
            await task
            logCmd ctx false 
        else
            let help = getCmdInfo cmd
            awaitResult (state.messageSender.Warning(msg, sprintf "bad usage [ %s ]" cmd.name, help))
                |> ignore
            logCmd ctx false
              

    let handleMessageReceived (msg : SocketMessage) =
        match handlerState with
        | Some state ->
            match getLastImgUrl msg with
            | Some url -> () //impl cache
            | None -> ()

            if not msg.Author.IsBot then
                let content = msg.Content
                if content.ToLower().StartsWith(state.prefix) || (startsWithBotMention state content) then
                    let cmdName = getCmdName state content
                    match state.commands |> Map.tryFind cmdName with
                    | Some cmd ->
                        if cmd.isEnabled then
                            if cmd.ownerOnly && not (msg.Author.Id.Equals(Config.OWNER_ID)) then
                                state.logger.Nice("Commands", ConsoleColor.Red,sprintf "%s tried to use a owner-only command <%s>" (msg.Author.ToString()) cmdName)
                                awaitResult (state.messageSender.Warning(msg, "Owner-only command", "This is a owner-only feature")) |> ignore
                            else
                                runCmd state msg cmd content
                        else
                            state.logger.Nice("Commands", ConsoleColor.Red,sprintf "%s tried to use a disabled command <%s>" (msg.Author.ToString()) cmdName)
                            awaitResult (state.messageSender.Warning(msg, "Disabled command", "This is a disabled feature for now")) |> ignore
                    | None -> ()
        | None -> 
            printfn "COMMAND HANDLER WAS NOT INITIALIZED ??!"