namespace RssReaderFs

open System

module RssChecker =
  type RssChecker (reader: RssReader.RssReader) =
    inherit Observable.ObservableSource<RssItem>()

    member this.RunAsync(?timeout) =
      let timeout = defaultArg timeout (60 * 1000)
      async {
        while true do
          let! newReader = reader.Update()
          for item in newReader.Timeline do
            this.Next(item)
          do! Async.Sleep(timeout)
      }
