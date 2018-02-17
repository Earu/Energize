namespace Flamerize

open System
open System.Text
open System.Net
open System.IO

module Utils =
    [<AbstractClass; Sealed>]
    type public HTTP() =
        static member UserAgent = "Flamerize Discord(Earu's Bot)"
        static member Fetch (url: String): Option<string> =
            try
                let req: HttpWebRequest = WebRequest.Create url :?> HttpWebRequest
                req.Method <- "GET"
                req.Timeout <- 1000 * 60
                req.Headers.[HttpRequestHeader.UserAgent] <- HTTP.UserAgent

                let content = Async.RunSynchronously <| async{
                    let! answer = Async.AwaitTask (req.GetResponseAsync())
                    let stream = answer.GetResponseStream()
                    let reader = new StreamReader(stream,Encoding.UTF8)
                    let result = reader.ReadToEnd()

                    return result
                }

                Some(content)

            with
            | _ -> None