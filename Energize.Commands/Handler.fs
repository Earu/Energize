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

    type Command =
        {
            name : string
            callback : (CommandContext -> Task)
            mutable isLoaded : bool
            usage : string
            help : string
            moduleName : string
        }

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
        handlerState <- Some newState

    let private getChannelCache (state : CommandState) (id : uint64) : CommandCache =
        if state.caches |> Map.containsKey id then
            state.caches.[id]
        else
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
    
    let registerCmd (state : CommandState) (name : string) (callback : CommandContext -> Task) (usage : string) 
        (help : string) (moduleName : string) =
        let cmd : Command = 
            { 
                name = name
                callback = callback 
                isLoaded = true
                usage = usage
                help = help
                moduleName = moduleName
            }
        handlerState <- Some { state with commands = state.commands.Add (name, cmd) }
    
    // yeah yeah ugly mutability I know, but CPU intensive otherwise
    let unloadCmd (state : CommandState) (input : string)  =
        if state.commands |> Map.containsKey input then
            state.commands.[input].isLoaded <- false

    let loadCmd (state : CommandState) (input : string) = 
        if state.commands |> Map.containsKey input then
            state.commands.[input].isLoaded <- true

    let private getCmdName (state : CommandState) (input : string) : string =
        input.Substring(getPrefixLength state input).Split(' ').[0]

    let private getCmdArgs (state : CommandState) (input : string) : string list =
        let offset = (getPrefixLength state input) + ((getCmdName state input) |> String.length)
        input.[offset..].TrimStart().Split(',') |> Array.toList

    let private isCmdLoaded (state : CommandState) (cmdName : string) : bool =
        if state.commands |> Map.containsKey cmdName then
            state.commands.[cmdName].isLoaded
        else
            false
    
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

    let private handleTimeOut (state : CommandState) (msg : SocketMessage) (cmdName : string) (tcallback : Task) : Task =
        if not tcallback.IsCompleted then
            async {
                let! tres = Task.WhenAny(tcallback, Task.Delay(2000)) |> Async.AwaitTask
                if not (tres.Equals(tcallback)) then
                    state.messageSender.Warning(msg, "Time out", sprintf "Your command %s is timing out!" cmdName) 
                        |> Async.AwaitTask |> ignore
                    state.logger.Nice("Commands", ConsoleColor.Yellow, sprintf "Time out of command <%s>" cmdName)
                
                return tres
            } |> Async.RunSynchronously
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
        let mutable info = { log = String.Empty; color = ConsoleColor.Cyan; head = "DMCommands"; action = "used" }
        info <-
            if ctx.isPrivate then
                info
            else
                let chan = ctx.message.Channel :?> IGuildChannel
                let log = info.log + (sprintf "(%s - #%s) " chan.Guild.Name chan.Name)
                { info with log = log; color = ConsoleColor.Blue; head = "Commands" }

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

    let private runCmd (state : CommandState) (msg : SocketMessage) (cmdName : string) (input : string) =
        match state.commands |> Map.tryFind cmdName with
        | Some cmd ->
            let ctx = buildCmdContext state cmdName msg (getCmdArgs state input)
            let task = handleTimeOut state msg cmdName (cmd.callback ctx)
            task.ConfigureAwait(false) |> ignore
            task |> Async.AwaitTask |> Async.RunSynchronously
            logCmd ctx false
        | None ->
            state.logger.Warning(sprintf "Could not find command callback for <%s>?!" cmdName)

    let handleMessageReceived (msg : SocketMessage) =
        match handlerState with
        | Some s ->
            match getLastImgUrl msg with
            | Some url -> () // cache
            | None -> ()

            if msg.Author.IsBot then
                let content = msg.Content
                if content.ToLower().StartsWith(s.prefix) || (startsWithBotMention s content) then
                    let cmdName = getCmdName s content
                    if isCmdLoaded s cmdName then
                        runCmd s msg cmdName content
                    else
                        s.logger.Nice("Commands", ConsoleColor.Red,sprintf "%s tried to use a disabled command <%s>" (msg.Author.ToString()) cmdName)
                        s.messageSender.Warning(msg, "Disabled command", "This is a disabled feature for now") 
                            |> Async.AwaitTask |> Async.RunSynchronously |> ignore
        | None -> 
            printfn "COMMAND HANDLER WAS NOT INITIALIZED ??!"
            
