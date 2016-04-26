namespace RssReaderFs

open System
open FsYaml

module RssSource =
  let ofFeed (feed: RssFeed) =
    Feed feed

  let ofUnread source =
    Unread source

  let union name sources =
    Union (name, sources)

  let rec name =
    function
    | Feed (feed: RssFeed) -> feed.Name
    | Unread src -> src |> name
    | Union (name, _) -> name

  let rec toFeeds =
    function
    | Feed feed ->
        Set.singleton feed
    | Unread src ->
        src |> toFeeds
    | Union (_, srcs) ->
        srcs |> Set.collect toFeeds

  /// このソースに全体が含まれているソースの集合
  let rec subSources self =
    match self with
    | Feed _
    | Unread _ ->  // Unread は未読分を含まないので、元のソースを「含む」とはみなさない
        Set.singleton self
    | Union (_, srcs) ->
        srcs |> Set.collect subSources |> Set.add self

  /// items: このソースが受信対象とするフィードが発信したアイテムの列
  let rec filterItems items =
    function
    | Feed _ ->
        items

    | Unread src ->
        let doneSet =
          src
          |> toFeeds
          |> Set.collect RssFeed.doneSet
        in
          src
          |> filterItems items
          |> Array.filter (fun item -> doneSet |> Set.contains item |> not)

    | Union (_, srcSet) ->
        srcSet |> Set.fold filterItems items

  let fetchItemsAsync src =
    async {
      let feeds =
        src |> toFeeds

      let! feedItemsArray =
        feeds
        |> Seq.map (RssFeed.updateAsync)
        |> Async.Parallel

      let (feeds', itemsArray) =
        feedItemsArray |> Array.unzip

      let items =
        itemsArray
        |> Array.collect id
        |> flip filterItems src

      return (feeds', items)
    }

  let rec rename oldName newName self =
    match self with
    | Feed feed ->
        feed |> RssFeed.rename oldName newName |> Feed
    | Unread src ->
        src |> rename oldName newName |> Unread
    | Union (srcName, srcs) ->
        let srcName'  = srcName |> replace oldName newName
        let srcs'     = srcs |> Set.map (rename oldName newName)
        in Union (srcName', srcs')

  let rec toSExpr =
    function
    | Feed (feed: RssFeed) ->
        sprintf """(feed %s "%s")"""
          (feed.Name) (feed.Url |> Url.toString)
    | Unread src ->
        sprintf "(unread %s)" (src |> toSExpr)
    | Union (name, srcs) ->
        if srcs |> Set.isEmpty
        then "()"
        else
          sprintf "(union %s %s)"
            name
            (String.Join(" ", srcs |> Set.map toSExpr))

  let rec toSpec =
    function
    | Feed (feed: RssFeed) ->
        Feed (feed.Url)
    | Unread src ->
        Unread (src |> toSpec)
    | Union (name, srcs) ->
        Union (name, srcs |> Set.map toSpec)

  let rec ofSpec feedMap =
    function
    | Feed url ->
        match feedMap |> Map.tryFind url with
        | Some feed -> Feed feed
        | None -> failwithf "Unregistered URL: %s" (url |> Url.toString)
    | Unread src ->
        Unread (src |> ofSpec feedMap)
    | Union (name, srcs) ->
        Union (name, srcs |> Set.map (ofSpec feedMap))

  let toYaml (src: RssSource) =
    src
    |> toSpec
    |> Yaml.dump

  let ofYaml feedMap yaml =
    yaml
    |> Yaml.load<RssSourceSpec>
    |> ofSpec feedMap
