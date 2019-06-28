namespace Energize.Commands

open Discord.WebSocket
open Discord
open Discord.Rest
open Energize.Essentials
open Cache
open System
open Energize.Interfaces.Services
open AsyncHelper

module Context =
    let private properOutput (input : string) =
        if input.Length < 2048 then
            input
        else
            input.Substring(0,2045) + "..."

    type CommandContext =
        {
            client : DiscordShardedClient
            restClient : DiscordRestClient
            message : IMessage
            arguments : string list
            prefix : string
            messageSender : MessageSender
            logger : Logger
            isPrivate : bool
            cache : CommandCache
            commandName : string
            serviceManager : IServiceManager
            random : Random
            guildUsers : IGuildUser list
            commandCount : int
        }

        member this.hasArguments =
            (this.arguments |> List.length > 0) && (not (String.IsNullOrWhiteSpace this.arguments.[0]))

        member this.input =
            String.Join(Config.Instance.Discord.Separator, this.arguments).Trim()

        member this.authorMention =
            this.message.Author.Mention

        member this.sendOK (head : string option) (input : string) =
            let header = match head with Some h -> h | None -> this.commandName
            awaitResult (this.messageSender.Good(this.message, header, properOutput input))

        member this.sendWarn (head : string option) (input : string) =
            let header = match head with Some h -> h | None -> this.commandName
            awaitResult (this.messageSender.Warning(this.message, header, properOutput input))

        member this.sendBad (head : string option) (input : string) = 
            let header = match head with Some h -> h | None -> this.commandName
            awaitResult (this.messageSender.Danger(this.message, header, properOutput input))

        member this.sendRaw (input : string) =
            awaitResult (this.messageSender.SendRaw(this.message, properOutput input))

        member this.sendEmbed (embed : Embed) =
            awaitResult (this.messageSender.Send(this.message, embed))

        member _this.embedField (name: string) (value : obj) (isinline : bool) =
            let display = 
                let str = match value with null -> String.Empty | _ -> value.ToString()
                if str.Length > 1024 then str.Substring(0,1021) + "..." else str
            let field = EmbedFieldBuilder()
            field
                .WithIsInline(isinline)
                .WithName(name)
                .WithValue(if String.IsNullOrWhiteSpace display then " - " else display)

    let isPrivate (msg : SocketMessage) =
        match msg.Channel with 
        | :? IDMChannel -> true 
        | _ -> false

    let isNSFW (msg : SocketMessage) = 
        if isPrivate msg then 
            true
        else
            let chan = msg.Channel :?> ITextChannel
            chan.IsNsfw || chan.Name.ToLower().Contains("nsfw")

    let isAdmin (user : SocketUser) =
        match user with
        | :? SocketGuildUser as guser ->
            let roles = guser.Roles |> Seq.filter (fun role -> role.Name.Equals("EnergizeAdmin"))
            (roles |> Seq.length > 0) || guser.GuildPermissions.Administrator
        | _ -> true

    let isAuthorAdmin (msg : SocketMessage) =
        if isPrivate msg then true else isAdmin msg.Author