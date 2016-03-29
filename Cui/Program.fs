namespace RssReaderFs.Cui

open System
open RssReaderFs

module Program =
  [<EntryPoint>]
  let main argv =
    let rc = RssClient.Create(@"feeds.json")
    let rrc = RssReaderConsole(rc)

    try
      rrc.CheckNewFeedsAsync()
      |> Async.Start

      rrc.Interactive()
      |> Async.RunSynchronously
    finally
      rc.Save()

    // exit code
    0
