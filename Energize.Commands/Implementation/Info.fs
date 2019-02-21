namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Information")>]
module Info =
    open Energize.Commands.Context
    open Discord
    open Discord.WebSocket
    open Energize.Commands
    open AsyncHelper
    open System.Text

    [<GuildOnlyCommand>]
    [<Command("server", "Gets information about the server", "server <nothing>")>]
    let server (ctx : CommandContext) = async {
        let guild = (ctx.message.Channel :?> IGuildChannel).Guild :?> SocketGuild
        let owner = awaitResult (ctx.restClient.GetUserAsync(guild.Owner.Id))
        let createdAt =
            let time = guild.CreatedAt.ToString()
            time.Remove(time.Length - 7)
        let region = guild.VoiceRegionId.ToString().ToUpper()
        let builder = StringBuilder()
        builder
            .Append(sprintf "**ID:** %d\n" guild.Id)
            .Append(sprintf "**OWNER:** %s\n" (match owner with null -> "Unknown" | _ -> owner.Mention))
            .Append(sprintf "**MEMBERS:** %d\n" guild.MemberCount)
            .Append(sprintf "**REGION:** %s\n" region)
            .Append(sprintf "**CREATED ON:** %s\n" createdAt)
            .Append(sprintf "**MAIN CHANNEL:** %s\n" guild.DefaultChannel.Name)
            |> ignore

        let len = guild.Emotes |> Seq.length
        if len > 0 then
            builder.Append("\n--- Emotes ---\n") |> ignore
            for i in 0..(len - 1) do
                let emoji = guild.Emotes |> Seq.tryItem i
                match emoji with
                | Some e ->
                    builder.Append(" " + e.ToString() + " ") |> ignore
                    if (i % 10).Equals(0) then
                        builder.Append('\n') |> ignore
                | None -> ()
        
        let result = builder.ToString()
        awaitIgnore (ctx.messageSender.Send(ctx.message, guild.Name, result, ctx.messageSender.ColorGood, guild.IconUrl))
    }