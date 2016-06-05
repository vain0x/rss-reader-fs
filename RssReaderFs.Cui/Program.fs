namespace RssReaderFs.Cui

open System
open RssReaderFs.Core

module Program =
  [<EntryPoint>]
  let main argv =
    let rr = RssReader.create ()
    let view = View(rr)
    let rrc = Ctrl(rr, view.PrintCommandResult)

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
        rr |> RssReader.save

      // exit code
      0
    with
    | e ->
        eprintfn "%s" e.Message
        1
