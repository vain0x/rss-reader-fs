namespace RssReaderFs.Cui

open System
open System.IO
open System.Collections.Generic
open RssReaderFs

type Ctrl (rc: RssClient) =
  let view =
    new View(rc)

  member this.Update(srcOpt) =
    async {
      let src =
        defaultArg srcOpt (rc.Reader |> RssReader.allFeedSource)
      return! rc.UpdateAsync src
    }

  member this.CheckNewItemsAsync(?timeout, ?thresh) =
    let timeout = defaultArg timeout  (5 * 60 * 1000)  // 5 min
    let thresh  = defaultArg thresh   1

    let rec loop () =
      async {
        let! newItems = this.Update(None)
        do
          if newItems |> Array.isEmpty |> not then
            view.PrintCount(newItems)
        do! Async.Sleep(timeout)
        return! loop ()
      }
    in loop ()

  member this.UpdateAndShowCount(srcOpt) =
    async {
      let! items = this.Update(srcOpt)
      do view.PrintCount(items)
    }

  member this.UpdateAndShowDetails(srcOpt) =
    async {
      let! items = this.Update(srcOpt)
      do view.PrintItems(items)
    }

  member this.TryFindSource(srcName) =
    rc.Reader
    |> RssReader.tryFindSource srcName
    |> tap (fun opt ->
        if opt |> Option.isNone then
          eprintfn "Unknown source: %s" srcName
        )

  member private this.ProcCommandImpl(command) =
    async {
      match command with
      | "update" :: srcName :: _ ->
          match this.TryFindSource(srcName) with
          | None -> ()
          | Some src ->
              do! this.UpdateAndShowCount(Some src)

      | "update" :: _ ->
          do! this.UpdateAndShowCount(None)

      | "show" :: srcName :: _ ->
          match this.TryFindSource(srcName) with
          | None -> ()
          | Some src ->
              do! this.UpdateAndShowDetails(Some src)

      | "show" :: _ ->
          do! this.UpdateAndShowDetails(None)

      | "feeds" :: _ ->
          rc.Reader
          |> RssReader.allFeeds
          |> Array.iter (fun src ->
              printfn "%s <%s>"
                (src.Name) (src.Url |> Url.toString)
            )

      | "feed" :: name :: url :: _ ->
          let feed = RssFeed.create name url
          in
            rc.AddSource(feed |> RssSource.ofFeed)

      | "remove" :: name :: _ ->
          rc.Reader
          |> RssReader.tryFindSource name
          |> Option.iter (fun src ->
              rc.RemoveSource(name)
              printfn "'%s' has been removed."
                (src |> RssSource.name)
              )

      | "sources" :: _ ->
          rc.Reader
          |> RssReader.sourceMap
          |> Map.toList
          |> List.iter (fun (_, src) ->
              printfn "%s" (src |> RssSource.toSExpr)
              )

      | "tag" :: tagName :: srcName :: _ ->
          match rc.Reader |> RssReader.tryFindSource srcName with
          | Some src -> rc.AddTag(tagName, src)
          | None -> printfn "Unknown source name: %s" srcName

      | "detag" :: tagName :: srcName :: _ ->
          match rc.Reader |> RssReader.tryFindSource srcName with
          | Some src -> rc.RemoveTag(tagName, src)
          | None -> printfn "Unknown source name: %s" srcName

      | "tags" :: srcName :: _ ->
          match this.TryFindSource(srcName) with
          | None -> ()
          | Some src ->
              rc.Reader
              |> RssReader.tagSetOf src
              |> Set.iter (fun tagName ->
                  view.PrintTag(tagName)
                  )

      | "tags" :: _ ->
          rc.Reader
          |> RssReader.tagMap 
          |> Map.iter (fun tagName _ ->
              view.PrintTag(tagName)
              )

      | _ -> ()
    }

  member this.ProcCommand(command) =
    lockConsole (fun () -> this.ProcCommandImpl(command))

  member this.ProcCommandLine(kont, lineOrNull) =
    async {
      match lineOrNull with
      | null | "" ->
          return! kont
      | "quit" | "halt" | "exit" ->
          ()
      | line ->
          let command =
            line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
            |> Array.toList
          do! this.ProcCommand(command)
          return! kont
    }

  member this.Interactive() =
    let rec loop () = async {
        let! line = Console.In.ReadLineAsync() |> Async.AwaitTask
        return! this.ProcCommandLine(loop (), line)
      }
    in loop ()
