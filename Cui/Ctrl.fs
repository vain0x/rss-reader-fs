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

  member this.UpdateAndShowTitles(srcOpt) =
    async {
      let! items = this.Update(srcOpt)
      do view.PrintItemTitles(items)
    }

  member this.TryFindSource(srcName) =
    rc.Reader
    |> RssReader.tryFindSource srcName
    |> tap (fun opt ->
        if opt |> Option.isNone then
          eprintfn "Unknown source name: %s" srcName
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
          
      | "list" :: srcName :: _ ->
          match this.TryFindSource(srcName) with
          | None -> ()
          | Some src ->
              do! this.UpdateAndShowTitles(Some src)

      | "list" :: _ ->
          do! this.UpdateAndShowTitles(None)

      | "feeds" :: _ ->
          rc.Reader
          |> RssReader.allFeeds
          |> Array.iter (fun src ->
              printfn "%s <%s>"
                (src.Name) (src.Url |> Url.toString)
            )

      | "feed" :: name :: url :: _ ->
          let feed = RssFeed.create name url
          let old = rc.AddSource(feed |> RssSource.ofFeed)
          let () =
            match old with
            | None ->
                printfn "Feed '%s' has been added."
                  name
            | Some src ->
                eprintfn "Source '%s' does already exist: %s"
                  (src |> RssSource.name) (src |> RssSource.toSExpr)
          in ()

      | "remove" :: name :: _ ->
          match rc.RemoveSource(name) with
          | Some src ->
              printfn "Source '%s' has been removed: %s"
                name (src |> RssSource.toSExpr)
          | None ->
              eprintfn "Unknown source name: %s"
                name

      | "rename" :: oldName :: newName :: _ ->
          if rc.RenameSource(oldName, newName)
          then printfn "Some sources are renamed."
          else printfn "No sources are renamed."

      | "sources" :: _ ->
          rc.Reader
          |> RssReader.sourceMap
          |> Map.toList
          |> List.iter (fun (_, src) ->
              printfn "%s" (src |> RssSource.toSExpr)
              )

      | "tag" :: tagName :: srcName :: _ ->
          match this.TryFindSource(srcName) with
          | None -> ()
          | Some src ->
              match rc.AddTag(tagName, src) with
              | Some _ ->
                  eprintfn "Source '%s' does already exist."
                    tagName
              | None ->
                  printfn "Tag '%s' is added to '%s'."
                    tagName srcName

      | "detag" :: tagName :: srcName :: _ ->
          match this.TryFindSource(srcName) with
          | None -> ()
          | Some src ->
              match rc.RemoveTag(tagName, src) with
              | None ->
                  eprintfn "Source '%s' doesn't have tag '%s'."
                    srcName tagName
              | Some _ ->
                  printfn "Tag '%s' is removed from '%s'."
                    tagName srcName

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

      | _ ->
          eprintfn "Unknown command: %s"
            (String.Join(" ", command))
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
