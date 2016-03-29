namespace RssReaderFs.Cui

open System
open RssReaderFs

module Program =
  [<EntryPoint>]
  let main argv =
    let path = @"feeds.json"
    let rr = RssReader.Serialize.loadOrEmpty path
    let rrc = Ctrl(rr)

    try
      rrc.CheckNewItemsAsync()
      |> Async.Start

      rrc.Interactive()
      |> Async.RunSynchronously
    finally
      rrc.Reader |> RssReader.Serialize.save path

    // exit code
    0
