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

  let ofTwitterUser name =
    TwitterUser (name, DateTime.Now)

  let rec atomSources (rr: RssReader) src =
    let collect srcName =
      rr.SourceMap |> Map.tryFind srcName
      |> Option.map (atomSources rr)
      |> Option.getOr Set.empty
    match src with
    | Feed _
    | TwitterUser _
      -> Set.singleton src
    | Unread srcName ->
        srcName |> collect
    | Union (_, srcNames) ->
        srcNames |> Set.collect (fun srcName -> collect srcName)

  let rec name =
    function
    | Feed feed -> feed.Name
    | Unread name
    | Union (name, _)
    | TwitterUser (name, _)
      -> name

  let rec rename oldName newName self =
    match self with
    | Feed feed ->
        feed |> RssFeed.rename oldName newName |> Feed
    | Unread srcName ->
        srcName |> replace oldName newName |> Unread
    | Union (srcName, srcs) ->
        let srcName'  = srcName |> replace oldName newName
        let srcs'     = srcs |> Set.map (replace oldName newName)
        in Union (srcName', srcs')
    | TwitterUser _
      -> self

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
    | TwitterUser (_, date) ->
        items |> Array.filter (fun item -> item.Date > date)

  let validate (rr: RssReader) =
    function
    | Feed feed -> feed |> RssFeed.validate
    | TwitterUser (name, _) ->
        rr.TwitterToken |> Twitter.tryFindUser name
        |> Trial.ignore
    | _ -> pass ()

  let rec toSExpr (rr: RssReader) =
    function
    | Feed feed ->
        sprintf """(feed %s "%s")"""
          feed.Name (feed.Url)
    | Unread srcName ->
        sprintf "(unread %s)" (rr.SourceMap |> Map.find srcName |> toSExpr rr)
    | Union (name, srcNames) ->
        sprintf "(union %s %s)"
          name
          (srcNames
            |> Set.map (fun name -> rr.SourceMap |> Map.find name |> toSExpr rr)
            |> String.concat " ")
    | TwitterUser (name, _) ->
        sprintf "(twitter-user %s)" name

  let toYaml src =
    src |> Yaml.dump<RssSource>

  let ofYaml yaml =
    yaml |> Yaml.load<RssSource>

module RssSourceUpdate =
  let empty =
    {
      DoneSet       = Map.empty
    }

  let merge l r =
    {
      DoneSet       = Map.appendWith (Set.union) l.DoneSet r.DoneSet
    }

  let mergeMany =
    List.fold merge empty
