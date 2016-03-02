open System

type RssReaderConsole () =
  let sources =
    [
      ("Yahoo!ニュース", @"http://dailynews.yahoo.co.jp/fc/rss.xml")
      ("NHKニュース", @"http://www3.nhk.or.jp/rss/news/cat0.xml")
    ]
    |> List.map (fun (name, url) ->
        { Name = name; Uri = Uri(url) }
        )

  let mutable feeds =
    sources
    |> List.map (Rss.downloadFeedAsync)
    |> Async.Parallel
    |> Async.RunSynchronously

  member this.Update() =
    let newFeeds =
      feeds
      |> Seq.map (Rss.updateFeedAsync)
      |> Async.Parallel
      |> Async.RunSynchronously
    feeds <- newFeeds

  // 全アイテムの時系列順
  member this.Timeline =
    feeds
    |> Seq.collect (fun feed -> feed.Items)
    |> Seq.toList
    |> List.sortBy (fun item -> item.Date)
    |> List.rev

  member this.PrintTimeLine() =
    let items = this.Timeline
    let len = items |> List.length
    items
    |> List.iteri (fun i item ->
        printfn "----------------"
        printfn "[%2d/%2d] %s\r\nDate: %s\r\nUri: %s\r\n%s\r\n\r\n%s"
          i len
          (item.Title)
          (item.Uri |> string)
          (item.Date |> string)
          (item.Desc |> Option.getOr "(no description)")
          (item.Link |> Option.getOr "(no link)")

        Console.ReadKey() |> ignore
        )

[<EntryPoint>]
let main argv =
  let rrc = RssReaderConsole()

  async {
    while true do
      rrc.PrintTimeLine()
      do! Async.Sleep(1000)
      rrc.Update()
  }
  |> Async.RunSynchronously

  // exit code
  0
