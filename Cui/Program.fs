﻿namespace RssReaderFs.Cui

open System
open RssReaderFs

module Program =
  [<EntryPoint>]
  let main argv =
    let rc = RssClient.Create()
    let view = View(rc)
    let rrc = Ctrl(rc, view.PrintCommandResult)

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
      rc.Save()

    // exit code
    0
