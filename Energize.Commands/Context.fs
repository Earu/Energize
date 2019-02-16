namespace Energize.Commands

module Context =
    open Discord.WebSocket
    open Discord
    open System
    open Discord.Rest
    open Energize.Toolkit
    open Cache
    
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
            cache : CommandCache
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

    let private tryFindGuildUser (ctx : CommandContext) (predicate : IGuildUser -> bool) =
        match ctx.isPrivate with
        | true ->
            None
        | false ->
            let res = ctx.cache.guildUsers |> Seq.tryFind predicate
            match res with
            | Some user ->
                Some (user :> SocketUser)
            | None ->
                None

    let private handleLast (ctx : CommandContext) : SocketUser option =
        Some (ctx.message.Author)

    let private handleAdmin (ctx : CommandContext) : SocketUser option = 
        tryFindGuildUser ctx (fun user -> user.GuildPermissions.Administrator)

    let private handleId (ctx : CommandContext) (input : string) : SocketUser option =
        try
            let id = uint64 input
            tryFindGuildUser ctx (fun user -> user.Id.Equals(id))
        with _ ->
            None
    
    let private handleRandom (ctx : CommandContext) : SocketUser option = 
        match ctx.isPrivate with
        | true ->
            None
        | false ->
            let len = (Seq.length ctx.cache.guildUsers) 
            let rand = Random()
            let i = rand.Next(0,len)
            Some (ctx.cache.guildUsers.[i] :> SocketUser)

    let private handleTag (ctx : CommandContext) (tag : string) (args : string list) : SocketUser option =
        match tag with
        | "last" -> 
            handleLast ctx
        | "admin" ->
            handleAdmin ctx
        | "id" ->
            handleId ctx args.Head
        | "random" ->
            handleRandom ctx 
        | _ -> 
            None

    let findUser (ctx : CommandContext) (input : string) (withId : bool) =
        match input.StartsWith("$") && input |> String.length > 0 with
        | true ->
            let args = input.Split(' ') |> Seq.toList
            handleTag ctx args.Head (args.[1..])

        | false -> None
        


