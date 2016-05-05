namespace RssReaderFs.Cui

open System
open System.IO
open System.Collections.Generic
open RssReaderFs

type Ctrl (rc: RssClient, view: View) =
  member this.TryFindSource(srcName) =
    rc.Reader
    |> RssReader.tryFindSource srcName
    |> tap (fun opt ->
        if opt |> Option.isNone then
          view.PrintUnknownSourceNameError(srcName)
        )

  member this.CheckNewItemsAsync(?timeout, ?thresh) =
    let timeout = defaultArg timeout  (5 * 60 * 1000)  // 5 min
    let thresh  = defaultArg thresh   1

    let rec loop () =
      async {
        let! newItems = rc.UpdateAllAsync
        do
          if newItems |> Array.isEmpty |> not then
            view.PrintCount(newItems)
        do! Async.Sleep(timeout)
        return! loop ()
      }
    in loop ()

  member this.UpdateAndShowCount(srcName) =
    async {
      match this.TryFindSource(srcName) with
      | None -> ()
      | Some src ->
          let! items = rc.UpdateAsync(src)
          do view.PrintCount(items)
    }

  member this.UpdateAndShowDetails(srcName) =
    async {
      match this.TryFindSource(srcName) with
      | None -> ()
      | Some src ->
          let! items = rc.UpdateAsync(src)
          do view.PrintItems(items)
    }

  member this.UpdateAndShowTitles(srcName) =
    async {
      match this.TryFindSource(srcName) with
      | None -> ()
      | Some src ->
          let! items = rc.UpdateAsync(src)
          do view.PrintItemTitles(items)
    }

  member private this.ProcCommandImpl(command) =
    async {
      match command with
      | "update" :: srcName :: _ ->
          do! this.UpdateAndShowCount(srcName)

      | "update" :: _ ->
          do! this.UpdateAndShowCount(AllSourceName)

      | "show" :: srcName :: _ ->
          do! this.UpdateAndShowDetails(srcName)

      | "show" :: _ ->
          do! this.UpdateAndShowDetails(AllSourceName)
          
      | "list" :: srcName :: _ ->
          do! this.UpdateAndShowTitles(srcName)

      | "list" :: _ ->
          do! this.UpdateAndShowTitles(AllSourceName)

      | "feeds" :: _ ->
          do view.PrintFeeds(rc.Reader |> RssReader.allFeeds)

      | "feed" :: name :: url :: _ ->
          let feed      = RssFeed.create name url
          let result    = rc.TryAddSource(feed |> RssSource.ofFeed)
          do view.PrintResult(result)

      | "twitter-user" :: name :: _ ->
          let twitterUser = Entity.TwitterUser(ScreenName = name, ReadDate = DateTime.Now)
          do view.PrintResult(rc.TryAddSource(RssSource.ofTwitterUser twitterUser))

      | "remove" :: name :: _ ->
          let result    = rc.TryRemoveSource(name)
          do view.PrintResult(result)

      | "rename" :: oldName :: newName :: _ ->
          let result    = rc.RenameSource(oldName, newName)
          do view.PrintResult(result)

      | "sources" :: _ ->
          do view.PrintSources(rc.Reader |> RssReader.allAtomicSources)

      | "tag" :: tagName :: srcName :: _ ->
          let result    = rc.AddTag(tagName, srcName)
          do view.PrintResult(result)

      | "detag" :: tagName :: srcName :: _ ->
          let result    = rc.RemoveTag(tagName, srcName)
          do view.PrintResult(result)

      | "tags" :: srcName :: _ ->
          rc.Reader
          |> RssReader.tagSetOf srcName
          |> Set.iter (fun tagName ->
              view.PrintTag(tagName)
              )

      | "tags" :: _ ->
          rc.Reader |> RssReader.allTags
          |> Set.iter (fun tagName -> view.PrintTag(tagName))

      | _ ->
          view.PrintUnknownCommand(command)
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
    let rec loop () =
      async {
        let! line = Console.In.ReadLineAsync() |> Async.AwaitTask
        return! this.ProcCommandLine(loop (), line)
      }
    in loop ()
