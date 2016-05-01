namespace RssReaderFs

open System
open FsYaml
open Chessie.ErrorHandling

module RssSource =
  let ofFeed (feed: RssFeed) =
    Feed feed

  let ofUnread srcName =
    Unread srcName

  let union name sources =
    Union (name, sources)

  let rec name =
    function
    | Feed feed -> feed.Name
    | Unread name
    | Union (name, _)
      -> name

  let rec rename oldName newName =
    function
    | Feed feed ->
        feed |> RssFeed.rename oldName newName |> Feed
    | Unread srcName ->
        srcName |> replace oldName newName |> Unread
    | Union (srcName, srcs) ->
        let srcName'  = srcName |> replace oldName newName
        let srcs'     = srcs |> Set.map (replace oldName newName)
        in Union (srcName', srcs')

  let rec unreadItems (rr: RssReader) (items: RssItem []) =
    function
    | Feed feed ->
        items |> Array.filter (fun item -> feed.DoneSet |> Set.contains item |> not)
    | Unread srcName ->
        let src = rr.SourceMap |> Map.find srcName
        in unreadItems rr items src
    | Union (srcName, srcNames) ->
        srcNames |> Set.fold (fun items srcName ->
            rr.SourceMap |> Map.find srcName
            |> unreadItems rr items
            ) items

  let validate =
    function
    | Feed feed -> feed |> RssFeed.validate
    | _ -> pass ()

  let rec toSExpr (rr: RssReader) =
    function
    | Feed feed ->
        sprintf """(feed %s "%s")"""
          feed.Name (feed.Url |> Url.toString)
    | Unread srcName ->
        sprintf "(unread %s)" (rr.SourceMap |> Map.find srcName |> toSExpr rr)
    | Union (name, srcNames) ->
        sprintf "(union %s %s)"
          name
          (srcNames
            |> Set.map (fun name -> rr.SourceMap |> Map.find name |> toSExpr rr)
            |> String.concat " ")

  let toYaml src =
    src |> Yaml.dump<RssSource>

  let ofYaml yaml =
    yaml |> Yaml.load<RssSource>
