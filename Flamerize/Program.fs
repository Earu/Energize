open Flamerize.Initializer
open System.Threading.Tasks

[<EntryPoint>]
let Main _ =
    Discord.ConnectAsync() |> Async.AwaitTask |> Async.RunSynchronously
    Task.Delay -1 |> Async.AwaitTask |> Async.RunSynchronously
    0