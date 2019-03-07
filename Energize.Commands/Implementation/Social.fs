namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Social")>]
module Social =
    open System
    open Energize.Commands.Context
    open Energize.Toolkit
    open Energize.Commands.UserHelper
    open Energize.Commands.AsyncHelper
    open System.Net
    open Energize.Interfaces.Services
    open System.Text
    open Discord.WebSocket
    open Discord

    let private actions = StaticData.SOCIAL_ACTIONS |> Seq.map (|KeyValue|) |> Map.ofSeq

    let registerAction (ctx : CommandContext) (users : SocketUser list) (action : string) =
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
    [<Command("act", "Social interaction with up to 3 users", "act <action>,<user|userid>,<user|userid|nothing>,<user|userid|nothing>")>]
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
                let help = sprintf "Actions available are:\n`%s`" (String.Join(',', actionNames))
                [ ctx.sendWarn None help ]
    }

    type LoveObj = { percentage : int; result : string }
    [<CommandParameters(2)>]
    [<Command("love", "Gets how compatible two users are", "love <user|userid>,<user|userid>")>]
    let love (ctx : CommandContext) = async {
        let user1 = findUser ctx ctx.arguments.[0] true
        let user2 = findUser ctx ctx.arguments.[1] true
        return 
            match (user1, user2) with
            | (Some u1, Some u2) ->
                let endpoint = 
                    let u1arg = sprintf "fname=%s&" u1.Username
                    let u2arg = sprintf "sname=%s" u2.Username
                    sprintf "https://love-calculator.p.mashape.com/getPercentage?%s%s" u1arg u2arg
                let json = 
                    let cb (req : HttpWebRequest) =
                        req.Headers.[System.Net.HttpRequestHeader.Accept] <- "text/plain"
                        req.Headers.["X-Mashape-Key"] <- Config.MASHAPE_KEY
                    awaitResult (HttpClient.GetAsync(endpoint, ctx.logger, null, Action<HttpWebRequest>(cb)))
                let love = JsonPayload.Deserialize<LoveObj>(json, ctx.logger)
                let display = sprintf "%s & %s\n💓: \t%dpts\n%s" u1.Mention u2.Mention love.percentage love.result
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
                ctx.messageSender.BuilderWithAuthor(ctx.message, builder)
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
                    .WithFields(fields)
                    .WithThumbnailUrl(user.GetAvatarUrl())
                    .WithColor(ctx.messageSender.ColorGood)
                    .WithFooter(ctx.commandName)
                    |> ignore

                dbctx.Dispose()
                [ ctx.sendEmbed (builder.Build()) ]
            | None ->
                [ ctx.sendWarn None "Could not find any user for your input" ]
    }