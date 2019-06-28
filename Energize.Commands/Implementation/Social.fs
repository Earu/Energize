namespace Energize.Commands.Implementation

open Energize.Commands.Command
open System
open Energize.Commands.Context
open Energize.Essentials
open Energize.Commands.UserHelper
open Energize.Commands.AsyncHelper
open Discord
open Energize.Interfaces.Services.Database
open Energize.Interfaces.Services.Senders
open Discord.WebSocket
open Energize.Commands
open Discord.Rest

[<CommandModule("Social")>]
module Social =
    let private actions = StaticData.Instance.SocialActions |> Seq.map (|KeyValue|) |> Map.ofSeq

    let registerAction (ctx : CommandContext) (users : IUser list) (action : string) =
        let db = ctx.serviceManager.GetService<IDatabaseService>("Database")
        let dbctx = awaitResult (db.GetContext())
        for user in users do
            let dbuser = awaitResult (dbctx.Instance.GetOrCreateUserStats(user.Id))
            match action with
            | "hug" -> dbuser.HuggedCount <- dbuser.HuggedCount + 1UL
            | "kiss" -> dbuser.KissedCount <- dbuser.KissedCount + 1UL
            | "snuggle" -> dbuser.SnuggledCount <- dbuser.SnuggledCount + 1UL
            | "pet" -> dbuser.PetCount <- dbuser.PetCount + 1UL
            | "nom" -> dbuser.NomedCount <- dbuser.NomedCount + 1UL
            | "spank" -> dbuser.SpankedCount <- dbuser.SpankedCount + 1UL
            | "shoot" -> dbuser.ShotCount <- dbuser.ShotCount + 1UL
            | "slap" -> dbuser.SlappedCount <- dbuser.SlappedCount + 1UL
            | "yiff" -> dbuser.YiffedCount <- dbuser.YiffedCount + 1UL
            | "bite" -> dbuser.BittenCount <- dbuser.BittenCount + 1UL
            | "boop" -> dbuser.BoopedCount <- dbuser.BoopedCount + 1UL
            | "flex" -> dbuser.FlexCount <- dbuser.FlexCount + 1UL
            | _ -> ()
        dbctx.Dispose()

    [<CommandParameters(2)>]
    [<Command("act", "Social interaction with up to 3 users", "act <action> <user|userid> <user|userid|nothing> <user|userid|nothing>")>]
    let act (ctx : CommandContext) = async {
        return
            match actions |> Map.tryFind ctx.arguments.[0] with
            | Some sentences ->
                let users =
                    let allUsers =
                        ctx.arguments.[1..]
                        |> List.map (fun arg -> findUser ctx arg true)
                        |> List.filter (fun opt -> opt.IsSome)
                        |> List.map (fun user -> user.Value)
                        |> List.distinctBy (fun user -> user.Id)
                    if allUsers |> List.length > 3 then allUsers.[..3] else allUsers
                registerAction ctx users ctx.arguments.[0]
                let userMentions = users |> List.map (fun user -> if user.Id.Equals(ctx.message.Author.Id) then "themself" else user.Mention)
                let userDisplays = String.Join(" and ", userMentions)
                if userMentions |> List.isEmpty then
                    [ ctx.sendWarn None "Could not find any user(s) to interact with for your input" ]
                else
                    let sentence =
                        sentences.[ctx.random.Next(0, sentences.Length)]
                            .Replace("<origin>", ctx.authorMention)
                            .Replace("<user>", userDisplays)
                    [ ctx.sendOK None sentence ]
            | None ->
                let actionNames = actions |> Map.toList |> List.map (fun (name, _) -> name)
                let help = sprintf "Actions available are:\n`%s`" (String.Join(", ", actionNames))
                [ ctx.sendWarn None help ]
    }

    [<CommandParameters(1)>]
    [<CommandConditions(CommandCondition.GuildOnly)>]
    [<Command("someone", "Mentions a random user of the server and adds your text afterward", "someone <text>")>]
    let someone (ctx : CommandContext) = async {
        let user = findUser ctx "$random" true
        return 
            match user with
            | Some u ->
                [ ctx.sendOK None (sprintf "%s %s" u.Mention ctx.input) ]
            | None ->
                [ ctx.sendWarn None "There was a problem getting a random user" ]
    }

    [<CommandParameters(2)>]
    [<Command("love", "Gets how compatible two users are", "love <user|userid> <user|userid>")>]
    let love (ctx : CommandContext) = async {
        let user1 = findUser ctx ctx.arguments.[0] true
        let user2 = findUser ctx ctx.arguments.[1] true
        return
            match (user1, user2) with
            | (Some u1, Some u2) ->
                let percentage = ctx.random.Next(0, 101)
                let result = 
                    match percentage with
                    | p when p < 10 -> "Ouch! Not gonna work it seems!"
                    | p when p >= 10 && p < 30 -> "Good friends!"
                    | p when p >= 30 && p < 50 -> "Friends ... with benefits?"
                    | p when p >= 50 && p < 70 -> "A good couple!"
                    | p when p >= 70 && p < 100 -> "Deeply matching souls."
                    | p when p >= 100 -> "The ultimate couple."
                    | _ -> "?"
                let display = sprintf "%s & %s\n💓: \t%d%c\n%s" u1.Mention u2.Mention percentage '%' result
                [ ctx.sendOK None display ]
            | _ ->
                [ ctx.sendWarn None "Could not find any user(s) for your input" ]
    }

    [<CommandParameters(1)>]
    [<Command("setdesc", "Sets your description", "setdesc <description>")>]
    let setDesc (ctx : CommandContext) = async {
        let db = ctx.serviceManager.GetService<IDatabaseService>("Database")
        let dbctx = awaitResult (db.GetContext())
        let dbuser = awaitResult (dbctx.Instance.GetOrCreateUser(ctx.message.Author.Id))
        dbuser.Description <- ctx.input
        dbctx.Dispose()
        return [ ctx.sendOK None "Description successfully changed" ]
    }

    [<CommandParameters(1)>]
    [<Command("desc", "Gets a user description", "desc <user|userid>")>]
    let desc (ctx : CommandContext) = async {
        return
            match findUser ctx ctx.arguments.[0] true with
            | Some user ->
                let db = ctx.serviceManager.GetService<IDatabaseService>("Database")
                let dbctx = awaitResult (db.GetContext())
                let dbuser = awaitResult (dbctx.Instance.GetOrCreateUser(user.Id))
                dbctx.Dispose()
                [ ctx.sendOK None (sprintf "%s description is: `%s`" user.Mention dbuser.Description) ]
            | None ->
                [ ctx.sendWarn None "Could not find any user for your input" ]
    }

    [<CommandParameters(1)>]
    [<Command("stats", "Gets a user social interaction stats", "stats <user|userid>")>]
    let stats (ctx : CommandContext) = async {
        return
            match findUser ctx ctx.arguments.[0] true with
            | Some user ->
                let db = ctx.serviceManager.GetService<IDatabaseService>("Database")
                let dbctx = awaitResult (db.GetContext())
                let dbstats = awaitResult (dbctx.Instance.GetOrCreateUserStats(user.Id))
                let builder = EmbedBuilder()
                let fields = [
                   ctx.embedField "Hugs" dbstats.HuggedCount true
                   ctx.embedField "Kisses" dbstats.KissedCount true
                   ctx.embedField "Snuggles" dbstats.SnuggledCount true
                   ctx.embedField "Pets" dbstats.PetCount true
                   ctx.embedField "Noms" dbstats.NomedCount true
                   ctx.embedField "Spanks" dbstats.SpankedCount true
                   ctx.embedField "Shots" dbstats.ShotCount true
                   ctx.embedField "Slaps" dbstats.SlappedCount true
                   ctx.embedField "Yiffs" dbstats.YiffedCount true
                   ctx.embedField "Bites" dbstats.BittenCount true
                   ctx.embedField "Boops" dbstats.BoopedCount true
                   ctx.embedField "Flexes" dbstats.FlexCount true
                ]
                builder
                    .WithAuthorNickname(ctx.message)
                    .WithFields(fields)
                    .WithThumbnailUrl(user.GetAvatarUrl())
                    .WithColorType(EmbedColorType.Good)
                    .WithFooter(ctx.commandName)
                    |> ignore

                dbctx.Dispose()
                [ ctx.sendEmbed (builder.Build()) ]
            | None ->
                [ ctx.sendWarn None "Could not find any user for your input" ]
    }

    [<CommandParameters(3)>]
    [<Command("vote","Creates a 5 minutes vote with up to 9 choices","vote <description> <choice> <choice> <choice|nothing> ...")>]
    let vote (ctx : CommandContext) = async {
        let votes = ctx.serviceManager.GetService<IVoteSenderService>("Votes")
        let choices = if ctx.arguments.Length > 10 then ctx.arguments.[1..8] else ctx.arguments.[1..]
        return [ awaitResult (votes.SendVote(ctx.message, ctx.arguments.[0], choices)) ]
    }

    let private createHOFChannel (ctx : CommandContext) =
        let name = "⭐hall-of-fames"
        let desc = sprintf "Where %s will post unique messages" (ctx.client.CurrentUser.ToString())
        let guser = ctx.message.Author :?> SocketGuildUser
        let created = awaitResult (guser.Guild.CreateTextChannelAsync(name))
        let everyoneperms = OverwritePermissions(mentionEveryone = PermValue.Deny, sendMessages = PermValue.Deny, sendTTSMessages = PermValue.Deny)
        let botperms = OverwritePermissions(sendMessages = PermValue.Allow, addReactions = PermValue.Allow)
        await (created.AddPermissionOverwriteAsync(guser.Guild.EveryoneRole, everyoneperms))
        await (created.AddPermissionOverwriteAsync(ctx.client.CurrentUser, botperms))
        await (created.ModifyAsync(Action<TextChannelProperties>(fun prop -> prop.Topic <- Optional(desc))))
        created :> ITextChannel

    let private getOrCreateHOFChannel (ctx : CommandContext) =
        let db = ctx.serviceManager.GetService<IDatabaseService>("Database")
        let dbctx = awaitResult (db.GetContext())
        let guild = (ctx.message.Author :?> SocketGuildUser).Guild :> IGuild
        let dbguild = awaitResult (dbctx.Instance.GetOrCreateGuild(guild.Id))
        let chan =
            if dbguild.HasHallOfShames then
                Some ((awaitResult (guild.GetChannelAsync(dbguild.HallOfShameID))) :?> ITextChannel)
            else
                let c = createHOFChannel ctx
                dbguild.HallOfShameID <- c.Id
                dbguild.HasHallOfShames <- true
                Some c

        dbctx.Dispose()
        chan

    let private trySendFameMsg (ctx : CommandContext) (chan : ITextChannel option) (msg : IMessage) =
        let builder = EmbedBuilder()
        builder
            .WithAuthor(msg.Author)
            .WithLimitedDescription(msg.Content)
            .WithFooter("#" + msg.Channel.Name)
            .WithTimestamp(msg.CreatedAt)
            .WithColorType(EmbedColorType.Normal)
            |> ignore
        match ImageUrlProvider.getLastImgUrl msg with
        | Some url -> builder.WithImageUrl(url) |> ignore
        | None -> ()

        match chan.Value with
        | :? SocketChannel as chan ->
            Some (awaitResult (ctx.messageSender.Send(chan, builder.Build())))
        | :? RestChannel as chan ->
            Some (awaitResult (ctx.messageSender.Send(chan, builder.Build())))
        | _ -> None

    [<CommandParameters(1)>]
    [<CommandPermissions(ChannelPermission.ManageChannels)>]
    [<CommandConditions(CommandCondition.AdminOnly, CommandCondition.GuildOnly)>]
    [<Command("fame", "Adds a message to the hall of fames", "fame <messageid>")>]
    let fame (ctx : CommandContext) = async {
        let chan = getOrCreateHOFChannel ctx
        let msgId = ref 0UL
        return
            if UInt64.TryParse(ctx.arguments.[0], msgId) && chan.IsSome then
                let msg = awaitResult (ctx.message.Channel.GetMessageAsync(msgId.Value))
                match msg with
                | null ->
                    [ ctx.sendWarn None "Could not find any message for your input" ]
                | m ->
                    match trySendFameMsg ctx chan m with
                    | None ->
                        [ ctx.sendWarn None "There was an error when posting the message into hall of fames" ]
                    | Some posted ->
                        match posted with
                        | null -> ()
                        | posted -> await (posted.AddReactionAsync(Emoji("🌟")))

                        [ ctx.sendOK None "Message successfully added to hall of fames" ]
            else
                [ ctx.sendWarn None "This command is expecting a number (message id)" ]
    }