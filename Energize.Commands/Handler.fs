namespace Energize.Commands

module CommandHandler =
    open Discord.WebSocket
    open ImageUrlProvider
    open System.Text.RegularExpressions
    open Discord

    let private startsWithBotMention (str : string) (user : SocketSelfUser) =
        Regex.IsMatch(str,"^<@!?" + user.Id.ToString() + ">")

    let private getCmdName str (user : SocketSelfUser) (prefix : string) =
        match startsWithBotMention str user with
        | true ->
            user.Mention.Length
        | false ->
            prefix.Length

    let private isCmdLoaded cmdName =
        false
    
    let private handleCmd msg cmdName =
        ()

    let handleMessageReceived (msg : IMessage) (user : SocketSelfUser) (prefix : string) =
        match getLastImgUrl msg with
        | Some url -> () // cache
        | None -> ()

        match msg.Author.IsBot with
        | true ->
            let content = msg.Content
            match content.ToLower().StartsWith(prefix) || (startsWithBotMention content user) with
            | true ->
                let cmdName = getCmdName content
                match isCmdLoaded cmdName with
                | true ->
                    handleCmd msg cmdName
                | false -> ()
            | false -> ()
        | false -> ()
