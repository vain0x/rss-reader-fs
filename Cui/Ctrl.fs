namespace RssReaderFs.Cui

open System
open System.IO
open System.Collections.Generic
open Chessie.ErrorHandling
open RssReaderFs.Core

type Ctrl (rc: RssReader, sendResult: CommandResult -> Async<unit>) =
  member this.TryFindSource(srcName) =
    rc
    |> RssReader.tryFindSource srcName
    |> Trial.failIfNone (srcName |> SourceDoesNotExist)

  member this.CheckNewItemsAsync(?timeout, ?thresh) =
    let timeout = defaultArg timeout  (5 * 60 * 1000)  // 5 min
    let thresh  = defaultArg thresh   1

    let rec loop () =
      async {
        let! newItems = rc |> RssReader.updateAsync Source.all
        if newItems |> Array.isEmpty |> not then
          do! (newItems, Count) |> Async.inject |> Trial.inject |> ArticleSeq |> sendResult
        do! Async.Sleep(timeout)
        return! loop ()
      }
    in loop ()

  member this.UpdateAndShow(srcName, fmt) =
    this.TryFindSource(srcName)
    |> Trial.lift (fun src -> async {
        let! items = rc |> RssReader.updateAsync src
        return (items, fmt)
        })
    |> ArticleSeq

  member private this.ProcCommandImpl(command) =
      match command with
      | "update" :: srcName :: _ ->
          this.UpdateAndShow(srcName, Count)
      | "update" :: _ ->
          this.UpdateAndShow(AllSourceName, Count)
      | "show" :: srcName :: _ ->
          this.UpdateAndShow(srcName, Details)
      | "show" :: _ ->
          this.UpdateAndShow(AllSourceName, Details)
      | "list" :: srcName :: _ ->
          this.UpdateAndShow(srcName, Titles)
      | "list" :: _ ->
          this.UpdateAndShow(AllSourceName, Titles)

      | "feeds" :: _ ->
          rc |> RssReader.allFeeds
          |> Seq.map (Source.ofFeed)
          |> SourceSeq

      | "feed" :: name :: url :: _ ->
          let feed      = RssFeed.create name url
          let result    = rc |> RssReader.tryAddSource (feed |> Source.ofFeed)
          in result |> Result

      | "twitter-user" :: name :: _ ->
          let twitterUser = Entity.TwitterUser(ScreenName = name)
          let src         = Source.ofTwitterUser twitterUser
          let result      = rc |> RssReader.tryAddSource src
          in result |> Result

      | "remove" :: name :: _ ->
          rc |> RssReader.tryRemoveSource name |> Result

      | "rename" :: oldName :: newName :: _ ->
          rc |> RssReader.renameSource oldName newName |> Result

      | "sources" :: _ ->
          rc |> RssReader.allAtomicSources |> SourceSeq

      | "tag" :: tagName :: srcName :: _ ->
          rc |> RssReader.addTag tagName srcName |> Result

      | "detag" :: tagName :: srcName :: _ ->
          rc |> RssReader.removeTag tagName srcName |> Result

      | "tags" :: srcName :: _ ->
          rc
          |> RssReader.tagSetOf srcName
          |> Seq.map (Source.ofTag)
          |> SourceSeq

      | "tags" :: _ ->
          rc |> RssReader.allTags
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
          do! sendResult result
          return! kont
    }
