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

  let mutable items =
    []

  member this.Update() =
    sources
    |> List.map (Rss.downloadRssAsync)
    |> Async.Parallel
    |> Async.RunSynchronously
    |> tap (fun feeds ->
        // 時系列順
        items <-
          feeds
          |> Seq.collect (fun feed -> feed.Items)
          |> Seq.toList
          |> List.sortBy (fun item -> item.Date)
          |> List.rev
        )
    |> ignore

  member this.Print() =
    let len = items |> List.length
    items
    |> List.iteri (fun i item ->
        printfn "----------------"
        printfn "[%2d/%2d] %s\r\nSource: %s <%s>\r\n%s\r\n\r\n%s"
          i len
          (item.Title)
          (item.Source.Name)
          (item.Source.Uri |> string)
          (item.Desc |> Option.getOr "(no description)")
          (item.Link |> Option.getOr "(no link)")

        Console.ReadKey() |> ignore
        )

[<EntryPoint>]
let main argv =
  let rcp = RssConsolePrinter()
  rcp.Update()
  rcp.Print()

  // exit code
  0
