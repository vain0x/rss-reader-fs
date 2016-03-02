open System

type RssConsolePrinter () =
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
    
  // 全アイテムの時系列順
  member this.Timeline =
    feeds
    |> Seq.collect (fun feed -> feed.Items)
    |> Seq.toList
    |> List.sortBy (fun item -> item.Date)
    |> List.rev

  member this.Print() =
    let items = this.Timeline
    let len = items |> List.length
    items
    |> List.iteri (fun i item ->
        printfn "----------------"
        printfn "[%2d/%2d] %s\r\nUri: %s\r\n%s\r\n\r\n%s"
          i len
          (item.Title)
          (item.Uri |> string)
          (item.Desc |> Option.getOr "(no description)")
          (item.Link |> Option.getOr "(no link)")

        Console.ReadKey() |> ignore
        )

[<EntryPoint>]
let main argv =
  let rcp = RssConsolePrinter()
  rcp.Print()

  // exit code
  0
