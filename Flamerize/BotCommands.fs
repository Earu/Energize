namespace Flamerize

open System.Threading.Tasks
open DSharpPlus.CommandsNext
open DSharpPlus.CommandsNext.Attributes
open Utils
open DSharpPlus.Entities
open HtmlAgilityPack

type BotCommands() =

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
            let endpoint = "https://samuelmaddock.github.io/glua-docs/#?q=" + ``text``
        
            match HTTP.Fetch endpoint with
            | Some(content) -> 
                let doc = new HtmlDocument()
                doc.LoadHtml(content)
                let node = doc.DocumentNode.SelectSingleNode("//article")
                let gwiki = node.SelectSingleNode("//a[@class]").InnerText
                //let github = node.SelectSingleNode("//a[@class='extra']/@href").InnerText
                //let func = node.SelectSingleNode("//div[@class='function_line']").InnerText

                do! ctx.RespondAsync(sprintf "%s" gwiki) |> Async.AwaitTask |> Async.Ignore
            | None -> do! ctx.RespondAsync("not found") |> Async.AwaitTask |> Async.Ignore
        } |> Async.StartAsTask :> Task