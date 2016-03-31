namespace RssReaderFs.Cui

open System
open System.IO
open System.Collections.Generic
open RssReaderFs

type Ctrl (rc: RssClient) =
  let view =
    new View(rc)

  member this.TryUpdate(srcOpt) =
    async {
      let src =
        defaultArg srcOpt (rc.Reader |> RssReader.allFeedSource)

      let! newItems = rc.UpdateAsync src
      do
        if newItems |> Array.isEmpty |> not then
          view.OnNewFeeds(newItems)
      return newItems
    }

  member this.CheckNewItemsAsync(?timeout, ?thresh) =
    let timeout = defaultArg timeout  (5 * 60 * 1000)  // 5 min
    let thresh  = defaultArg thresh   1

    let rec loop () =
      async {
        let! _ = this.TryUpdate(None)
        do! Async.Sleep(timeout)
        return! loop ()
      }
    in loop ()

  member this.TryUpdateAndShow(srcOpt) =
    async {
      let! items = this.TryUpdate(srcOpt)
      match items with
      | [||] ->
          printfn "No new items available."
      | items ->
          view.PrintItems(items)
    }

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
              let! items = this.TryUpdate(None)
              if items |> Seq.isEmpty then
                printfn "No new items available."

          | "show" :: srcName :: _ ->
            match rc.Reader |> RssReader.tryFindSource srcName with
            | Some src ->
                do! this.TryUpdateAndShow(Some src)
            | None ->
                printfn "Unknown source: %s" srcName

          | "show" :: _ ->
            do! this.TryUpdateAndShow(None)

          | "feeds" :: _ ->
              let body () =
                rc.Reader
                |> RssReader.allFeeds
                |> Array.iter (fun src ->
                    printfn "%s <%s>"
                      (src.Name) (src.Url |> Url.toString)
                  )
              in lockConsole body

          | "feed" :: name :: url :: _ ->
              let feed = RssFeed.create name url
              let body () =
                rc.AddSource(feed |> RssSource.ofFeed)
              in lockConsole body

          | "remove" :: name :: _ ->
              let body () =
                rc.Reader
                |> RssReader.tryFindSource name
                |> Option.iter (fun src ->
                    rc.RemoveSource(name)
                    printfn "'%s' has been removed."
                      (src |> RssSource.name)
                    )
              in lockConsole body

          | "sources" :: _ ->
              let body () =
                rc.Reader
                |> RssReader.sourceMap
                |> Map.toList
                |> List.iter (fun (_, src) ->
                    printfn "%s" (src |> RssSource.toSExpr)
                    )
              in lockConsole body

          | "tag" :: tagName :: srcName :: _ ->
              let body () =
                match rc.Reader |> RssReader.tryFindSource srcName with
                | Some src -> rc.AddTag(tagName, src)
                | None -> printfn "Unknown source name: %s" srcName
              in lockConsole body

          | "detag" :: tagName :: srcName :: _ ->
              let body () =
                match rc.Reader |> RssReader.tryFindSource srcName with
                | Some src -> rc.RemoveTag(tagName, src)
                | None -> printfn "Unknown source name: %s" srcName
              in lockConsole body

          | _ -> ()
          return! loop ()
      }
    in loop ()
