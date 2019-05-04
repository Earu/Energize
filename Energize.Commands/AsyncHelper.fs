namespace Energize.Commands

module AsyncHelper =
    open System.Threading.Tasks

    let toTaskResult<'t> (asyncOp : Async<'t>) : Task<'t> =
        asyncOp |> Async.StartAsTask

    let toTask (asyncOp : Async<unit>) : Task =
        asyncOp |> Async.StartAsTask :> Task

    let awaitResult<'t> (task : Task<'t>) : 't =
        task |> Async.AwaitTask |> Async.RunSynchronously

    let awaitIgnore<'t> (task : Task<'t>) : unit =
        awaitResult task |> ignore

    let await (task : Task) : unit =
        task |> Async.AwaitTask |> Async.RunSynchronously
    
    let awaitOp<'t> (asyncOp : Async<'t>) : 't =
        awaitResult (toTaskResult asyncOp)