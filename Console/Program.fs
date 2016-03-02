
open System
open RssReaderFs

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
        if i > 0 then
          printfn "..."
          Console.ReadKey() |> ignore

        printfn "----------------"
        printfn "[%3d/%3d] %s"
          i len
          (item.Title)
        printfn "* Date: %s" (item.Date.ToString("G"))
        printfn "* Link: %s" (item.Link |> Option.getOr "(no link)")
        printfn "* From: %s" (item.Uri  |> string)
        item.Desc |> Option.iter (printfn "* Desc:\r\n%s")
        )

  member this.Passive() =
    async {
      while true do
        this.PrintTimeLine()
        do! Async.Sleep(1000)
        this.Update()
    }
    |> Async.RunSynchronously

[<EntryPoint>]
let main argv =
  let rrc = RssReaderConsole()

  rrc.Passive()

  // exit code
  0
