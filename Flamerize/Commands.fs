namespace Flamerize

open System.Text.RegularExpressions
open System.Threading.Tasks
open DSharpPlus.CommandsNext
open DSharpPlus.CommandsNext.Attributes
open Utils
open HtmlAgilityPack

type Commands() =

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
                
                    let aseqdisplay = seq { for x in 0..((match args with | null -> 0 | _ -> Seq.length args) - 1) -> sprintf "%s" (Seq.item  x argdescs) }
                    let mutable argdisplay = match Seq.length aseqdisplay > 0 with
                                                | true -> aseqdisplay |> String.concat ", " 
                                                | false -> "None"
                    argdisplay <- Regex.Replace(argdisplay,"\.","")
                    argdisplay <- Regex.Replace(argdisplay,"\s{3,}","\n")
                    argdisplay <- Regex.Replace(argdisplay,"\s+"," ").TrimStart()

                    let rets = root.SelectNodes "//div[@class='return']"
                    let retdescs = seq { for ret in rets -> ret.InnerText }
                    let rseqdisplay = seq { for x in 0..((match rets with | null -> 0 | _ -> Seq.length rets) - 1) -> sprintf "%s" (Seq.item x retdescs)}
                    let mutable retdisplay = match Seq.length rseqdisplay > 0 with
                                                | true -> rseqdisplay |> String.concat ", "
                                                | false -> "None"
                    retdisplay <- Regex.Replace(retdisplay,"\.","")
                    retdisplay <- Regex.Replace(retdisplay,"\s{3,}","\n")
                    retdisplay <- Regex.Replace(retdisplay,"\s+"," ").TrimStart()
                
                    let desc = sprintf "**CALL:** %s(%s)\n**REALM:** %s\n**ARGS:**\n%s\n**RETURNS:**\n%s\n**WIKI:** %s" ``text`` call realm argdisplay retdisplay endpoint
                    MessageSender.CreateEmbed ctx.User "GLua" desc EmbedColor.Good                
                | None -> MessageSender.CreateEmbed ctx.User "GLua" "Couln't find this function" EmbedColor.Warning
            
            do! ctx.RespondAsync("",false,embed) |> Async.AwaitTask |> Async.Ignore
        } |> Async.StartAsTask :> Task