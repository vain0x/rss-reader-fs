
open System
open System.IO
open System.Collections.Generic
open RssReaderFs

type RssReaderConsole (rc: RssClient) =
  let unreadItems () =
    rc.Feeds

  let reader () =
    rc.Reader

  member this.CheckUpdate() =
    async {
      do! rc.UpdateAllAsync
      let feeds = unreadItems ()
      let len = feeds |> Seq.length
      if len > 0 then
        do!
          Console.Out.WriteLineAsync(sprintf "New %d feeds!" len)
          |> Async.AwaitTaskVoid
        return reader |> Some
      else
        return None
    }

  member this.PrintItem(item, ?header) =
    let header =
      match header with
      | Some h -> h + " "
      | None -> ""
    let src =
      reader () |> RssReader.tryFindSource(item.Url)
    do
      printfn "%s%s" header (item.Title)
      printfn "* Date: %s" (item.Date.ToString("G"))
      printfn "* Link: %s" (item.Link |> Option.getOr "(no link)")
      src |> Option.iter (fun { Name = name } ->
          printfn "* From: %s" name
          )
      item.Desc |> Option.iter (printfn "* Desc:\r\n%s")

      rc.ReadItem(item)

  member this.PrintTimeLine(newReader) =
    let body () =
      let feeds = unreadItems ()
      let len = feeds |> Seq.length
      feeds
      |> Seq.iteri (fun i item ->
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
              reader ()
              |> RssReader.sources
              |> Array.iteri (fun i src ->
                  printfn "#%d: %s <%s>"
                    i (src.Name) (src.Url |> string)
                )

          | "add" :: name :: url :: _ ->
              let source = Rss.sourceFromUrl name url
              in
                lock reader (fun () ->
                  rc.AddSource(source)
                  )

          | "remove" :: url :: _ ->
              let url = Url.ofString url
              let body () =
                reader ()
                |> RssReader.tryFindSource(url)
                |> Option.iter (fun src ->
                    rc.RemoveSource(url)
                    printfn "'%s <%s>' has been removed."
                      (src.Name)
                      (src.Url |> Url.toString)
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
