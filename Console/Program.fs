
open System
open System.IO
open System.Collections.Generic
open RssReaderFs

type Config (path) =
  member this.LoadReader() =
    let sources =
      match Rss.Serialize.load path with
      | Some r -> r
      | None ->
          eprintfn "Can't open file '%s'." path
          [||]
    in
      RssReader.create(sources)

  member this.SaveReader(r) =
    Rss.Serialize.save path r

type RssReaderConsole (cfg: Config) =
  let (* mutable *) unreadItems =
    new HashSet<RssItem>()

  let observer =
    { new RssSubscriber with
        member this.OnNewItems(items: RssItem []) =
          for item in items do
            unreadItems.Add(item) |> ignore
          }

  let mutable reader =
    cfg.LoadReader()
    |> RssReader.subscribe(observer)

  member this.Save() =
    cfg.SaveReader(reader)

  member this.CheckUpdate() =
    async {
      let! newReader = reader |> RssReader.updateAllAsync
      let len = unreadItems.Count
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
      reader |> RssReader.tryFindSource(item.Uri)
    do
      printfn "%s%s" header (item.Title)
      printfn "* Date: %s" (item.Date.ToString("G"))
      printfn "* Link: %s" (item.Link |> Option.getOr "(no link)")
      src |> Option.iter (fun { Name = name } ->
          printfn "* From: %s" name
          )
      item.Desc |> Option.iter (printfn "* Desc:\r\n%s")

      reader <- reader |> RssReader.readItem item (DateTime.Now)
      unreadItems.Remove(item) |> ignore

  member this.PrintTimeLine(newReader) =
    let body () =
      reader <- newReader
      let len = unreadItems.Count
      unreadItems
      |> Seq.toList  // unreadItems は可変なので Seq.iter だとダメ
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
                  this.PrintTimeLine(newReader)
                  ()

          | "src" :: _ ->
              reader
              |> RssReader.sources
              |> Array.iteri (fun i src ->
                  printfn "#%d: %s <%s>"
                    i (src.Name) (src.Uri |> string)
                )

          | "add" :: name :: url :: _ ->
              let source = Rss.sourceFromUrl name url
              in
                lock reader (fun () ->
                  reader <- reader |> RssReader.add(source)
                  )

          | "remove" :: url :: _ ->
              let uri = Uri(url)
              let body () =
                reader
                |> RssReader.tryFindSource(uri)
                |> Option.iter (fun src ->
                    reader <- reader |> RssReader.remove(uri)
                    printfn "'%s <%s>' has been removed."
                      (src.Name)
                      (src.Uri |> string)
                    )
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
