namespace RssReaderFs.Cui

open System
open System.IO
open System.Collections.Generic
open Chessie.ErrorHandling
open RssReaderFs

type Ctrl (rc: RssClient, view: View) =
  member this.TryFindSource(srcName) =
    rc.Reader
    |> RssReader.tryFindSource srcName
    |> Trial.failIfNone ("Unknown source name:" + srcName)

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
    this.TryFindSource(srcName)
    |> Trial.lift (fun src -> async {
        let! items = rc.UpdateAsync(src)
        do view.PrintCount(items)
        })

  member this.UpdateAndShowDetails(srcName) =
    this.TryFindSource(srcName)
    |> Trial.lift (fun src -> async {
        let! items = rc.UpdateAsync(src)
        do view.PrintItems(items)
        })

  member this.UpdateAndShowTitles(srcName) =
    this.TryFindSource(srcName)
    |> Trial.lift (fun src -> async {
        let! items = rc.UpdateAsync(src)
        do view.PrintItemTitles(items)
        })

  member private this.ProcCommandImpl(command) =
      match command with
      | "update" :: srcName :: _ ->
          this.UpdateAndShowCount(srcName) |> ResultAsync

      | "update" :: _ ->
          this.UpdateAndShowCount(AllSourceName) |> ResultAsync

      | "show" :: srcName :: _ ->
          this.UpdateAndShowDetails(srcName) |> ResultAsync

      | "show" :: _ ->
          this.UpdateAndShowDetails(AllSourceName) |> ResultAsync
          
      | "list" :: srcName :: _ ->
          this.UpdateAndShowTitles(srcName) |> ResultAsync

      | "list" :: _ ->
          this.UpdateAndShowTitles(AllSourceName) |> ResultAsync

      | "feeds" :: _ ->
          rc.Reader |> RssReader.allFeeds
          |> Seq.map (Source.ofFeed)
          |> SourceSeq

      | "feed" :: name :: url :: _ ->
          let feed      = RssFeed.create name url
          let result    = rc.TryAddSource(feed |> Source.ofFeed)
          in result |> Result

      | "twitter-user" :: name :: _ ->
          let twitterUser = Entity.TwitterUser(ScreenName = name, ReadDate = DateTime.Now)
          let src         = Source.ofTwitterUser twitterUser
          let result      = rc.TryAddSource(src)
          in result |> Result

      | "remove" :: name :: _ ->
          rc.TryRemoveSource(name) |> Result

      | "rename" :: oldName :: newName :: _ ->
          rc.RenameSource(oldName, newName) |> Result

      | "sources" :: _ ->
          rc.Reader |> RssReader.allAtomicSources |> SourceSeq

      | "tag" :: tagName :: srcName :: _ ->
          rc.AddTag(tagName, srcName) |> Result

      | "detag" :: tagName :: srcName :: _ ->
          rc.RemoveTag(tagName, srcName) |> Result

      | "tags" :: srcName :: _ ->
          rc.Reader
          |> RssReader.tagSetOf srcName
          |> Seq.map (Source.ofTag)
          |> SourceSeq

      | "tags" :: _ ->
          rc.Reader |> RssReader.allTags
          |> Seq.map (Source.ofTag)
          |> SourceSeq

      | _ ->
          UnknownCommand command

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
          let result = this.ProcCommand(command)
          do! view.PrintCommandResult(result)
          return! kont
    }

  member this.Interactive() =
    let rec loop () =
      async {
        let! line = Console.In.ReadLineAsync() |> Async.AwaitTask
        return! this.ProcCommandLine(loop (), line)
      }
    in loop ()
