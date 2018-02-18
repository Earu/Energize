namespace Flamerize

open System.Threading.Tasks
open DSharpPlus.CommandsNext
open DSharpPlus.CommandsNext.Attributes
open Utils
open DSharpPlus.Entities
open HtmlAgilityPack

type Commands() =

    [<Command("ping"); Description("Responds with socket latency.")>]
    member public self.PingAsync(ctx: CommandContext) = 
        async {
            do! ctx.TriggerTypingAsync() |> Async.AwaitTask
            do! ctx.RespondAsync(sprintf "Socket latency: %ims" ctx.Client.Ping) |> Async.AwaitTask |> Async.Ignore
        } |> Async.StartAsTask :> Task
    
    [<Command("glua"); Description("Gets info on a glua function")>]
    member public self.GluaAsync(ctx: CommandContext) ([<Description("The function to look for")>] ``text``: string) = 
        async {
            do! ctx.TriggerTypingAsync() |> Async.AwaitTask
            let parts = ``text``.Split '.'
            let query = match parts.Length > 1 with
                        | true -> ``text``.Replace('.','/')
                        | false -> "Global/" + ``text``

            let endpoint = "http://wiki.garrysmod.com/page/" + query
        
            match HTTP.Fetch endpoint with
            | Some(content) -> 
                let doc = new HtmlDocument()
                doc.LoadHtml content
                //let node = doc.DocumentNode.SelectSingleNode ""

                let embed = MessageSender.CreateEmbed ctx.User "GLua" endpoint
                do! ctx.RespondAsync("",false,embed) |> Async.AwaitTask |> Async.Ignore
            | None -> do! ctx.RespondAsync("Coulnd't find this function") |> Async.AwaitTask |> Async.Ignore
        } |> Async.StartAsTask :> Task