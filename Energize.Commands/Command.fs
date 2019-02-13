namespace Energize.Commands

module Command =
    open System.Threading.Tasks
    open Discord.WebSocket
    open Discord.Rest
    open Energize.Toolkit
    open Discord

    //type ArgumentCallback = (CommandContext -> string list -> SocketUser)

    type CommandContext =
        {
            discordClient : DiscordShardedClient
            discordRest : DiscordRestClient
            message : SocketMessage
            arguments : string list
            prefix : string
            messageSender : MessageSender
            logger : Logger
            isPrivate : bool
            guildCachedUsers : SocketGuildUser list
        }

        member this.isNSFW = 
            let chan = this.message.Channel :?> ITextChannel
            chan.IsNsfw || chan.Name.ToLower().Contains("nsfw")

        member this.isAuthorAdmin =
            match this.isPrivate with
            | true -> 
                true
            | false ->
                let author = this.message.Author :?> SocketGuildUser
                let roles = author.Roles |> Seq.filter (fun role -> role.Name.Equals("EnergizeAdmin") || role.Name.Equals("EBotAdmin"))
                (roles |> Seq.length > 0) || author.GuildPermissions.Administrator

        member private this.findUser (input : string) (withId : bool) =
            match input.StartsWith("$") && input |> String.length > 0 with
            | true ->
                let args = input.[1..].Split(' ')
                let identifier = args.[0]
                //args.[1..]
                // find a way to implement arguments
                None

            | false -> None
    
    type CommandCallback = (CommandContext -> Task)

    type Command = 
        {
            name : string
            callback : CommandCallback
            help : string
            usage : string
            loaded : bool
            moduleName : string
        }
        member this.run (ctx : CommandContext) (logger: Logger) : Task =
            try
                this.callback ctx
            with ex ->
                logger.Error(ex.Message)
                Task.CompletedTask


