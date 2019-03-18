namespace Energize.Commands.Implementation

open Energize.Commands.Command

[<CommandModule("Game")>]
module Game =
    open Energize.Commands.Context
    open Energize.Commands.AsyncHelper
    open Energize.Toolkit
    open System
    open Discord
    open Energize.Interfaces.Services.Generation

    type VanityResponseObj = { steamid : string; success : int }
    type VanityObj = { response : VanityResponseObj }
    let private tryGetSteamId64 (ctx : CommandContext) : uint64 option =
        let id = ref 0UL
        match ctx.input with
        | input when UInt64.TryParse(input, id) ->
            Some (id.Value)
        | input when input.StartsWith("STEAM_") ->
            let parts = input.Split(':')
            let z = ref 0L
            let y = ref 0L
            if parts.Length.Equals(3) && Int64.TryParse(parts.[1], y) && Int64.TryParse(parts.[2], z) then
                let identifier = 0x0110000100000000L
                Some (uint64 (z.Value * 2L + identifier + y.Value))
            else
                None
        | input ->
            let endpoint = 
                sprintf "https://api.steampowered.com/ISteamUser/ResolveVanityURL/v1?key=%s&vanityurl=%s" Config.Instance.Keys.SteamAPIKey input
            let json = awaitResult (HttpClient.GetAsync(endpoint, ctx.logger))
            let vanityObj = JsonPayload.Deserialize<VanityObj>(json, ctx.logger)
            if vanityObj.response.success.Equals(1) then
                Some (uint64 vanityObj.response.steamid)
            else 
                None

    type SteamUserObj = 
        { steamid : int64; personaname : string; profileurl : string; avatarfull : string 
          personastate : int; gameid : int option; gameextrainfo : string; timecreated : int
          communityvisibilitystate : int }
    type SteamPlySummaryResponseObj = { players : SteamUserObj list }
    type SteamPlySummaryObj = { response : SteamPlySummaryResponseObj }

    let private steamStates = [
        "Offline"; "Online"; "Busy"; "Away"; "Snooze"; 
        "Looking to trade"; "Looking to play"; "Invalid"
    ]

    let private isInGame (ply : SteamUserObj) = ply.gameid.IsSome
    let private getState (ply : SteamUserObj) = 
        let state = ply.personastate
        if steamStates.Length - 1 > state && state >= 0 then
            if isInGame ply then ("In-Game" + ply.gameextrainfo) else steamStates.[state]
        else
            steamStates.[7]

    [<CommandParameters(1)>]
    [<Command("steam", "Searches steam for a profile", "steam <name|steamid|steamid64>")>]
    let steam (ctx : CommandContext) = async {
        let id64 = tryGetSteamId64 ctx
        return 
            match id64 with
            | Some id ->    
                let endpoint = 
                    sprintf "http://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key=%s&steamids=%d" Config.Instance.Keys.SteamAPIKey id
                let json = awaitResult (HttpClient.GetAsync(endpoint, ctx.logger))
                let steamPlyObj = JsonPayload.Deserialize<SteamPlySummaryObj>(json, ctx.logger)
                match steamPlyObj.response.players |> Seq.tryHead with
                | Some ply ->
                    let created = 
                        let dt = DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
                        dt.AddSeconds(float ply.timecreated).ToLocalTime()
                    let isPublic = ply.communityvisibilitystate.Equals(3)
                    let visibility = if isPublic then "Public" else "Private"
                    let fields = [
                        ctx.embedField "Name" ply.personaname true
                        ctx.embedField "Status" (getState ply) true
                        ctx.embedField "Creation Date" created true
                        ctx.embedField "Visibility" visibility true
                        ctx.embedField "SteamID64" ply.steamid true
                        ctx.embedField "URL" ply.profileurl true
                    ]
                    let builder = EmbedBuilder()
                    ctx.messageSender.BuilderWithAuthor(ctx.message, builder)
                    builder
                        .WithFields(fields)
                        .WithThumbnailUrl(ply.avatarfull)
                        .WithColor(ctx.messageSender.ColorGood)
                        .WithFooter(ctx.commandName)
                        |> ignore

                    [ ctx.sendEmbed (builder.Build()) ]
                | None ->
                    [ ctx.sendWarn None "Could not find any user for your input" ]
            | None ->
                [ ctx.sendWarn None "Could not find any user for your input" ]
    }

    [<CommandParameters(3)>]
    [<Command("minesweeper", "Generates a minesweeper game", "minesweeper <width>,<height>,<mineamount>")>]
    let minesweeper (ctx : CommandContext) = async {
        let width = int ctx.arguments.[0]
        let height = int ctx.arguments.[1]
        let mineAmount = int ctx.arguments.[2]
        return 
            match (width, height) with
            | (w, h) when w > 10 || h > 10 ->
                [ ctx.sendWarn None "Maximum width and height is 10" ]
            | (w, h) when mineAmount > h * w ->
                [ ctx.sendWarn None "Cannot have more mines than squares" ]
            | (w, h) ->
                let minesweeper = ctx.serviceManager.GetService<IMinesweeperService>("Minesweeper")
                let res = minesweeper.Generate(w, h, mineAmount)
                if res.Length > 2000 then
                    [ ctx.sendWarn None "Output is too long to be displayed" ]
                else
                    [ ctx.sendOK None res ]
    }