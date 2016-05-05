﻿namespace RssReaderFs.Cui

open System
open System.IO
open System.Collections.Generic
open Chessie.ErrorHandling
open RssReaderFs

type Ctrl (rc: RssClient, sendResult: CommandResult -> Async<unit>) =
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
        if newItems |> Array.isEmpty |> not then
          do! (newItems, Count) |> Async.inject |> Trial.inject |> ArticleSeq |> sendResult
        do! Async.Sleep(timeout)
        return! loop ()
      }
    in loop ()

  member this.UpdateAndShow(srcName, fmt) =
    this.TryFindSource(srcName)
    |> Trial.lift (fun src -> async {
        let! items = rc.UpdateAsync(src)
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
          do! sendResult result
          return! kont
    }
