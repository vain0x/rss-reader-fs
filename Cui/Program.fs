namespace RssReaderFs.Cui

open System
open RssReaderFs.Core

module Program =
  [<EntryPoint>]
  let main argv =
    let rc = RssReader.create ()
    let view = View(rc)
    let rrc = Ctrl(rc, view.PrintCommandResult)

    try
      try
        match argv with
        | [||]
        | [| "-i" |]
        | [| "--interactive " |] ->
            rrc.CheckNewItemsAsync()
            |> Async.Start

            view.Interactive(rrc)
            |> Async.RunSynchronously
        | _ ->
            view.PrintCommandResult(rrc.ProcCommand(argv |> Array.toList))
            |> Async.RunSynchronously
      finally
        rc |> RssReader.save

      // exit code
      0
    with
    | e ->
        eprintfn "%s" e.Message
        1
