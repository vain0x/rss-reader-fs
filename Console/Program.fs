
open System
open System.IO
open RssReaderFs

type RssReader = RssReader.RssReader

type Config (path) =
  member this.LoadReader() =
    match RssReader.Serialize.load path with
    | Some r -> r
    | None ->
        eprintfn "Can't open file '%s'." path
        RssReader()

  member this.SaveReader(r) =
    RssReader.Serialize.save path r

type RssReaderConsole (cfg: Config) =
  let mutable reader =
    cfg.LoadReader()

  member this.Save() =
    cfg.SaveReader(reader)

  member this.UpdateAsync() =
   async {
      return! reader.Update()
   }

  member this.CheckUpdate() =
    async {
      let! newReader = this.UpdateAsync()
      let items = []//newReader.Timeline
      let len = items |> List.length
      if len > 0 then
        do!
          Console.Out.WriteLineAsync(sprintf "New %d feeds!" len)
          |> Async.AwaitTaskVoid
        return newReader |> Some
      else
        return None
    }

  member this.PrintItem(item, ?header) =
    let header =
      match header with
      | Some h -> h + " "
      | None -> ""
    let src =
      reader.TryFindSource(item.Uri)
    do
      printfn "%s%s" header (item.Title)
      printfn "* Date: %s" (item.Date.ToString("G"))
      printfn "* Link: %s" (item.Link |> Option.getOr "(no link)")
      src |> Option.iter (fun { Name = name } ->
          printfn "* From: %s" name
          )
      item.Desc |> Option.iter (printfn "* Desc:\r\n%s")

  member this.PrintTimeLine(newReader) =
    let body () =
      reader <- newReader
      let items = []//reader.Timeline
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
    in
      lock reader body

  member this.CheckNewFeedsAsync(?timeout, ?thresh) =
    let timeout = defaultArg timeout  (5 * 60 * 1000)  // 5 min
    let thresh  = defaultArg thresh   1

    let rec loop () =
      async {
        let! _ = this.CheckUpdate()
        do! Async.Sleep(timeout)
        return! loop ()
      }
    in
      loop ()

  member this.Interactive() =
    let rec loop () = async {
      let! line = Console.In.ReadLineAsync() |> Async.AwaitTask
      match line with
      | null | "" ->
          return! loop ()
      | "quit" | "halt" | "exit" ->
          ()
      | line ->
          let command =
            line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
            |> Array.toList
          match command with
          | "up" :: _ | "update" :: _ ->
              let! newReaderOpt = this.CheckUpdate()
              match newReaderOpt with
              | None ->
                  printfn "No new feeds available."
              | Some _ ->
                  ()

          | "show" :: _ ->
              let! newReaderOpt = this.CheckUpdate()
              match newReaderOpt with
              | None ->
                  printfn "No new feeds available."
              | Some newReader ->
                  // TODO:
                  ()

          | "src" :: _ ->
              reader.Feeds
              |> Array.map (fun feed -> feed.Source)
              |> Array.iteri (fun i src ->
                  printfn "#%d: %s <%s>"
                    i (src.Name) (src.Uri |> string)
                )

          | "add" :: name :: url :: _ ->
              let source = Rss.sourceFromUrl name url
              let r = RssReader([source])
              in
                lock reader (fun () ->
                  reader <- reader.Add(r)
                  )

          | "remove" :: name :: _ ->
              let pred (feed: RssFeed) =
                let source = feed.Source
                source.Name <> name
              let body () =
                let (r, removed) = reader.SourceFilter(pred)
                let count = removed |> Array.length
                if count > 0 then
                  reader <- r
                  printfn "%d sources have been removed:"
                    count
                  for feed in removed do
                    let src = feed.Source
                    printfn "%s <%s>" (src.Name) (src.Uri |> string)
              in
                lock reader body

          | _ -> ()
          return! loop ()
      }
    in
      loop ()

[<EntryPoint>]
let main argv =
  let cfg = Config(@"feeds.json")
  let rrc = RssReaderConsole(cfg)

  try
    rrc.CheckNewFeedsAsync()
    |> Async.Start

    rrc.Interactive()
    |> Async.RunSynchronously
  finally
    rrc.Save()

  // exit code
  0
