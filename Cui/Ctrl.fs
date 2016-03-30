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
      return newItems |> Array.isEmpty |> not
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
              let! success = this.TryUpdate(None)
              if success |> not then
                printfn "No new items available."

          | "show" :: _ ->
              let! success = this.TryUpdate(None)
              if success then
                view.PrintTimeLine()
              else
                printfn "No new items available."

          | "feeds" :: _ ->
              let body () =
                rc.Reader
                |> RssReader.allFeeds
                |> Array.iteri (fun i src ->
                    printfn "#%d: %s <%s>"
                      i (src.Name) (src.Url |> string)
                  )
              in lockConsole body

          | "add" :: name :: url :: _ ->
              let feed = RssFeed.create name url
              let body () =
                rc.AddFeed(feed)
              in lockConsole body

          | "remove" :: url :: _ ->
              let url = Url.ofString url
              let body () =
                rc.Reader
                |> RssReader.tryFindFeed url
                |> Option.iter (fun src ->
                    rc.RemoveFeed(url)
                    printfn "'%s <%s>' has been removed."
                      (src.Name)
                      (src.Url |> Url.toString)
                    )
              in lockConsole body

          | _ -> ()
          return! loop ()
      }
    in loop ()
