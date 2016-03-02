
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

  let reader =
    RssReader.RssReader(sources)

  member this.PrintItem(item, ?header) =
    let header =
      match header with
      | Some h -> h + " "
      | None -> ""

    printfn "%s%s" header (item.Title)
    printfn "* Date: %s" (item.Date.ToString("G"))
    printfn "* Link: %s" (item.Link |> Option.getOr "(no link)")
    printfn "* From: %s" (item.Uri  |> string)
    item.Desc |> Option.iter (printfn "* Desc:\r\n%s")

  member this.PrintTimeLine() =
    let items = reader.Timeline
    let len = items |> List.length
    items
    |> List.iteri (fun i item ->
        if i > 0 then
          printfn "..."
          Console.ReadKey() |> ignore

        printfn "----------------"
        this.PrintItem
          ( item
          , (sprintf "[%3d/%3d]" i len)
          )
        )

  member this.Passive() =
    async {
      while true do
        this.PrintTimeLine()
        do! Async.Sleep(1000)
        reader.Update()
    }
    |> Async.RunSynchronously

[<EntryPoint>]
let main argv =
  let rrc = RssReaderConsole()

  rrc.Passive()

  // exit code
  0
