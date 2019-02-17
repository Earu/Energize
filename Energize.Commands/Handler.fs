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
    open Energize.Toolkit
    open System
    open AsyncHelper
    open Command
    open Energize.Commands.Implementation

    type CommandState =
        {
            client : DiscordShardedClient
            restClient : DiscordRestClient
            caches : Map<uint64, CommandCache>
            globalCache : CommandCache
            commands : Map<string, Command>
            logger : Logger
            messageSender : MessageSender
            prefix : string
        }

    // I had to.
    let mutable private handlerState : CommandState option = None

    let registerCmd (state : CommandState) (cmd : Command) =
        state.logger.Nice("Commands", ConsoleColor.Green, sprintf "Registered <%s> in module [%s]" cmd.name cmd.moduleName)
        Some { state with commands = state.commands.Add (cmd.name, cmd) }

    let registerHelpCmd (state : CommandState) =
        let helpCmd : Command = 
            {
                name = "help"
                callback = 
                    (fun ctx -> async {
                        let cmdName = ctx.arguments.[0].Trim()
                        match handlerState.Value.commands |> Map.tryFind cmdName with
                        | Some cmd ->
                            let help = sprintf "**USAGE:**\n``%s``\n**HELP:**\n``%s``" cmd.usage cmd.help
                            awaitResult (ctx.messageSender.Good(ctx.message, ctx.commandName, help))
                                |> ignore
                        | None ->
                            awaitResult (ctx.messageSender.Warning(ctx.message, ctx.commandName, sprintf "Could not find any command named \'%s\'" cmdName))
                                |> ignore
                    })
                isLoaded = true
                usage = "help <cmd>"
                help = "This command."
                moduleName = "Core"
                parameters = 1
            }
        registerCmd state helpCmd

    let initialize (client : DiscordShardedClient) (restClient : DiscordRestClient) (logger : Logger) 
        (messageSender : MessageSender) (prefix : string) =
        let newState : CommandState =
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
            }
        
        newState.logger.Nice("Commands", ConsoleColor.Green, sprintf "Initializing commands with prefix \'%s\'" prefix)
        for cmd in Util.commands do
            handlerState <- (registerCmd newState cmd)

        handlerState <- registerHelpCmd (match handlerState with Some s -> s | None -> newState)
            

    let private getChannelCache (state : CommandState) (id : uint64) : CommandCache =
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

    let private startsWithBotMention (state : CommandState) (input : string) : bool =
        Regex.IsMatch(input,"^<@!?" + state.client.CurrentUser.Id.ToString() + ">")

    let private getPrefixLength (state : CommandState) (input : string) : int =
        if startsWithBotMention state input then
            state.client.CurrentUser.Mention.Length
        else
            state.prefix.Length
    
    // yeah yeah ugly mutability I know, but CPU intensive otherwise
    let unloadCmd (state : CommandState) (cmdName : string)  =
        match state.commands |> Map.tryFind cmdName with
        | Some cmd ->
            cmd.isLoaded <- false
        | None -> ()

    let loadCmd (state : CommandState) (cmdName : string) = 
        match state.commands |> Map.tryFind cmdName with
        | Some cmd ->
            cmd.isLoaded <- true
        | None -> ()

    let private getCmdName (state : CommandState) (input : string) : string =
        input.Substring(getPrefixLength state input).Split(' ').[0]

    let private getCmdArgs (state : CommandState) (input : string) : string list =
        let offset = (getPrefixLength state input) + ((getCmdName state input) |> String.length)
        let args = input.[offset..].TrimStart().Split(',') |> Array.toList
        if args.[0] |> String.IsNullOrWhiteSpace && (args |> List.length).Equals(1) then
            []
        else
            args

    let private isCmdLoaded (cmd : Command) : bool =
        cmd.isLoaded
    
    let private buildCmdContext (state : CommandState) (cmdName : string) (msg : SocketMessage) (args : string list) : CommandContext =
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
        }

    let private handleTimeOut (state : CommandState) (msg : SocketMessage) (cmdName : string) (asyncOp : Async<unit>) : Task =
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

    type private CommandLogInfo =
        {
            log : string
            color : ConsoleColor
            head : string
            action : string
        }

    // that function is disgusting
    let private logCmd (ctx : CommandContext) (deleted : bool) =
        let mutable info = { log = String.Empty; color = ConsoleColor.Blue; head = "DMCommands"; action = "used" }
        info <-
            if ctx.isPrivate then
                info
            else
                let chan = ctx.message.Channel :?> IGuildChannel
                let log = info.log + (sprintf "(%s - #%s) " chan.Guild.Name chan.Name)
                { info with log = log; color = ConsoleColor.Cyan; head = "Commands" }

        info <-
            if deleted then
                { info with action = "deleted"; color = ConsoleColor.Yellow }
            else 
                info
        info <-
            let log = sprintf "%s %s <%s>" ctx.message.Author.Username info.action ctx.commandName
            if String.IsNullOrWhiteSpace ctx.arguments.[0] then
                { info with log = log + " with no args" }
            else
                { info with log = log + (sprintf " => [ %s ]" (String.Join(',', ctx.arguments))) }

        ctx.logger.Nice(info.head, info.color, info.log)

    let private runCmd (state : CommandState) (msg : SocketMessage) (cmd : Command) (input : string) =
        let args = getCmdArgs state input
        let ctx = buildCmdContext state cmd.name msg args
        if (args |> List.length).Equals(cmd.parameters) then
            let task = handleTimeOut state msg cmd.name (cmd.callback ctx)
            task.ConfigureAwait(false) |> ignore
            await task
            logCmd ctx false 
        else
            let help = sprintf "**USAGE:**\n``%s``\n**HELP:**\n``%s``" cmd.usage cmd.help
            awaitResult (state.messageSender.Warning(msg, sprintf "Bad usage [%s]" cmd.name, help))
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
                        if isCmdLoaded cmd then
                            runCmd state msg cmd content
                        else
                            state.logger.Nice("Commands", ConsoleColor.Red,sprintf "%s tried to use a disabled command <%s>" (msg.Author.ToString()) cmdName)
                            awaitResult (state.messageSender.Warning(msg, "Disabled command", "This is a disabled feature for now")) |> ignore
                    | None -> ()
        | None -> 
            printfn "COMMAND HANDLER WAS NOT INITIALIZED ??!"
            
