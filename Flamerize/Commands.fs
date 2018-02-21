namespace Flamerize

open System.Text.RegularExpressions
open System.Threading.Tasks
open DSharpPlus.CommandsNext
open DSharpPlus.CommandsNext.Attributes
open Utils
open HtmlAgilityPack
open System.Drawing
open System.Collections
open System
open DSharpPlus.Entities
open DSharpPlus



type Commands() =
    member private self.Colors = [
        "Teal",Color.Teal; "Green",Color.Green; "LightGreen",Color.LightGreen; "DarkGreen",Color.DarkGreen; "Blue",Color.Blue;
        "DarkBlue",Color.DarkBlue; "Cyan",Color.Cyan; "DarkCyan",Color.DarkCyan; "Purple",Color.Purple; "Magenta",Color.Magenta
        "DarkMagenta",Color.DarkMagenta; "Gold",Color.Gold; "DarkGold",Color.DarkGoldenrod; "Orange",Color.Orange; "DarkOrange",Color.DarkOrange
        "Red",Color.Red; "DarkRed",Color.DarkRed; "Pink",Color.Pink; "Fuchsia",Color.Fuchsia; "Yellow",Color.Yellow; "Black",Color.Black; "White",Color.White
    ]

    [<Command("ping"); Description("Responds with socket latency.")>]
    member public self.PingAsync(ctx: CommandContext) = 
        async {
            do! ctx.TriggerTypingAsync() |> Async.AwaitTask
            let embed = MessageSender.CreateEmbed ctx.User "Ping" (sprintf "Socket latency: %ims" ctx.Client.Ping) EmbedColor.Good
            do! ctx.RespondAsync("",false,embed) |> Async.AwaitTask |> Async.Ignore
        } |> Async.StartAsTask :> Task
    
    [<Command("glua"); Description("Gets info on a glua function")>]
    member public self.GluaAsync(ctx: CommandContext) ([<Description("The function to look for")>] ``text``: string) = 
        async {
            do! ctx.TriggerTypingAsync() |> Async.AwaitTask
            let parts = ``text``.Split '.'
            let query = match parts.Length > 1 with
                        | true -> Regex.Replace(``text``,"(\.|:)","/")
                        | false -> "Global/" + ``text``

            let endpoint = "http://wiki.garrysmod.com/page/" + query
            let embed = 
                match HTTP.Fetch endpoint with
                | Some(content) -> 
                    let doc = new HtmlDocument()
                    doc.LoadHtml content
                    let root = doc.DocumentNode
                    let realm = (root.SelectSingleNode "//*[@id='bodyContent']/div[4]/div[1]/span").GetAttributeValue("title","NOT_FOUND")
                
                    let args = root.SelectNodes "//div[@class='argument']"
                    let argchunks = root.SelectNodes "//span[@class='arg_chunk']"
                    let argnames = match argchunks with 
                                    | null -> Seq.empty 
                                    | _ -> seq { for arg in argchunks -> arg.InnerText }
                
                    let argdescs = seq { for arg in args -> arg.InnerText }
                    let call = match Seq.length argnames > 0 with
                                | true -> argnames |> String.concat ", " 
                                | false -> ""
                    
                    //Returns the current seq passed or an empty one
                    let seq_or_default list = 
                        match list with
                        | null -> Seq.empty
                        | _ -> list

                    //Func to have max amount of values in a list
                    let max list = Seq.length (seq_or_default list) - 1
                    
                    //For the string display in embed
                    let display list = 
                        let mutable disp = match Seq.length (seq_or_default list) > 0 with
                                            | true -> list |> String.concat ", " 
                                            | false -> "None"
                        disp <- Regex.Replace(disp,"\.","")
                        disp <- Regex.Replace(disp,"\s{3,}","\n")
                        Regex.Replace(disp,"\s+"," ").TrimStart()
                        
                    let argseq = seq { for x in 0..max args -> sprintf "%s" (Seq.item  x argdescs) }
                    let argdisplay = display argseq

                    let rets = root.SelectNodes "//div[@class='return']"
                    let retdescs = seq { for ret in rets -> ret.InnerText }
                    let retseq = seq { for x in 0..max rets -> sprintf "%s" (Seq.item x retdescs)}
                    let retdisplay = display retseq
                
                    let mutable desc = sprintf "**CALL:** %s(%s)\n**REALM:** %s\n**ARGS:**\n%s\n" ``text`` call realm argdisplay
                    desc <- desc + sprintf "**RETURNS:**\n%s\n**WIKI:** %s" retdisplay endpoint
                    MessageSender.CreateEmbed ctx.User "GLua" desc EmbedColor.Good                
                | None -> MessageSender.CreateEmbed ctx.User "GLua" "Couln't find this function" EmbedColor.Warning
            
            do! ctx.RespondAsync("",false,embed) |> Async.AwaitTask |> Async.Ignore
        } |> Async.StartAsTask :> Task
    
    [<Command("colors"); Description("See the colors available")>]
    member public self.ColorsAsync(ctx: CommandContext) =
        async{
            let colors = seq { for (k,_) in self.Colors -> k } |> String.concat ", "
            let desc = sprintf "```\n%s```" colors
            let embed = MessageSender.CreateEmbed ctx.User "Colors" desc EmbedColor.Good
            do! ctx.RespondAsync("",false,embed) |> Async.AwaitTask |> Async.Ignore
        } |> Async.StartAsTask :> Task
    
    member private self.CToFList<'T> clist = seq { for (x: 'T) in clist -> x } |> Seq.toList

    member private self.GetRole(_member: DiscordMember) (name: string): Option<DiscordRole> =
        let predicate (x: DiscordRole) = x.Name.Equals(name)
        let roles = self.CToFList _member.Roles
        match List.exists predicate roles with
        | true -> Some(List.find predicate roles)
        | false -> None
    
    member private self.GetColorRoles(_member: DiscordMember): List<DiscordRole> =
        let mutable roles = List.empty
        for (name: string,_: Color) in self.Colors do
            let opt = self.GetRole _member name
            if opt.IsSome then
                roles <- opt.Value :: roles
        roles

    [<Command("color"); Description("Sets your color")>]
    member public self.ColorAsync(ctx: CommandContext) ([<Description("The color to apply")>]``color``: string) =
        let prediccol (x: string,_: Color) = x.Equals(``color``) 
        let colorfound = List.exists prediccol self.Colors
        async{
            match colorfound with
            | true -> 
                let predicrole (role: DiscordRole) = role.Name.Equals(``color``)
                let roles = self.CToFList ctx.Guild.Roles
                let mutable role = if List.exists predicrole roles then List.find predicrole roles else null
                role <- match role with
                        | null -> 
                            let (_,c) = List.find prediccol self.Colors
                            let col = new Nullable<DiscordColor>(new DiscordColor(c.R,c.G,c.B))
                            let newr = ctx.Guild.CreateRoleAsync(``color``,Nullable(),col) |> Async.AwaitTask |> Async.RunSynchronously
                            let botmember = ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id) |> Async.AwaitTask |> Async.RunSynchronously
                            let mutable highest = 0
                            for r in botmember.Roles do
                                highest <- if r.Position > highest then r.Position else highest
                            ctx.Guild.UpdateRolePositionAsync(newr,highest - 1) |> Async.AwaitTask |> Async.RunSynchronously
                            newr
                        | _ -> role
                
                let! _member = ctx.Guild.GetMemberAsync(ctx.User.Id) |> Async.AwaitTask
                for r in self.GetColorRoles _member do
                    do! ctx.Guild.RevokeRoleAsync(_member,r,"Color change") |> Async.AwaitTask |> Async.Ignore
                
                do! ctx.Guild.GrantRoleAsync(_member,role) |> Async.AwaitTask |> Async.Ignore
                let embed = MessageSender.CreateEmbed ctx.User "Color" (sprintf "Color successfully set to %s" ``color``) EmbedColor.Good
                do! ctx.RespondAsync("",false,embed) |> Async.AwaitTask |> Async.Ignore
            | false ->
                let colors = seq { for (k,_) in self.Colors -> k } |> String.concat ", "
                let desc = sprintf "Couln't find a color corresponding to your input!\nAvailable colors:\n```\n%s```" colors
                let embed = MessageSender.CreateEmbed ctx.User "Color" desc EmbedColor.Warning
                do! ctx.RespondAsync("",false,embed) |> Async.AwaitTask |> Async.Ignore
        } |> Async.StartAsTask :> Task