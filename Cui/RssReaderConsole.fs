namespace RssReaderFs.Cui

open System
open System.IO
open System.Collections.Generic
open RssReaderFs

type RssReaderConsole (rc: RssClient) =
  let feeds () =
    rc.Feeds

  let reader () =
    rc.Reader

  do rc.Subscribe (fun items ->
      let body () =
        printfn "New %d feeds!" (items |> Array.length)
      in lockConsole body
      ) |> ignore

  member this.TryUpdate() =
    async {
      let! newItems = rc.UpdateAllAsync
      return newItems |> Array.isEmpty |> not
    }

  member this.PrintItem(item, ?header) =
    let header =
      match header with
      | Some h -> h + " "
      | None -> ""
    let src =
      reader () |> RssReader.tryFindSource(item.Url)
    let body () =
      printfn "%s%s" header (item.Title)
      printfn "* Date: %s" (item.Date.ToString("G"))
      printfn "* Link: %s" (item.Link |> Option.getOr "(no link)")
      src |> Option.iter (fun { Name = name } ->
          printfn "* From: %s" name
          )
      item.Desc |> Option.iter (printfn "* Desc:\r\n%s")

      rc.ReadItem(item)
    in lockConsole body

  member this.PrintTimeLine() =
    let body () =
      let feeds = feeds ()
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
    in lockConsole body

  member this.CheckNewFeedsAsync(?timeout, ?thresh) =
    let timeout = defaultArg timeout  (5 * 60 * 1000)  // 5 min
    let thresh  = defaultArg thresh   1

    let rec loop () =
      async {
        let! _ = rc.UpdateAllAsync
        do! Async.Sleep(timeout)
        return! loop ()
      }
    in loop ()

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
              let! success = this.TryUpdate()
              if success |> not then
                printfn "No new feeds available."

          | "show" :: _ ->
              let! success = this.TryUpdate()
              if success then
                this.PrintTimeLine()
              else
                printfn "No new feeds available."

          | "src" :: _ ->
              let body () =
                reader ()
                |> RssReader.sources
                |> Array.iteri (fun i src ->
                    printfn "#%d: %s <%s>"
                      i (src.Name) (src.Url |> string)
                  )
              in lockConsole body

          | "add" :: name :: url :: _ ->
              let source = Rss.sourceFromUrl name url
              let body () =
                rc.AddSource(source)
              in lockConsole body

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
              in lockConsole body

          | _ -> ()
          return! loop ()
      }
    in loop ()
