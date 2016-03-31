namespace RssReaderFs.Cui

open System
open RssReaderFs

module Program =
  [<EntryPoint>]
  let main argv =
    let rc = RssClient.Create(@"feeds.json")
    let view = View(rc)
    let rrc = Ctrl(rc, view)

    try
      match argv with
      | [||]
      | [| "-i" |]
      | [| "--interactive " |] ->
          rrc.CheckNewItemsAsync()
          |> Async.Start

          rrc.Interactive()
          |> Async.RunSynchronously
      | _ ->
          rrc.ProcCommand(argv |> Array.toList)
          |> Async.RunSynchronously
    finally
      rc.Save()

    // exit code
    0
