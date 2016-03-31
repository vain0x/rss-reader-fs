namespace RssReaderFs

open System
open System.Collections.Generic
open FsYaml

module RssReader =
  let empty =
    {
      FeedMap         = Map.empty
      TagMap          = Map.empty
      SourceMap       = Map.empty
      UnreadItems     = Set.empty
    }

  let internal feedMap (rr: RssReader) =
    rr.FeedMap

  let tagMap (rr: RssReader) =
    rr.TagMap

  let sourceMap (rr: RssReader) =
    rr.SourceMap

  let unreadItems (rr: RssReader) =
    rr.UnreadItems

  let allFeeds rr =
    rr.FeedMap
    |> Map.toArray
    |> Array.map snd

  /// The maximum source
  let allFeedSource rr: RssSource =
    rr
    |> allFeeds
    |> Array.map RssSource.ofFeed
    |> Set.ofArray
    |> (fun srcs -> RssSource.union "ALL" srcs)

  let alreadyReadItems rr =
    rr
    |> allFeeds
    |> Array.map (fun src -> src.DoneSet)
    |> Array.fold (+) Set.empty

  let tryFindFeed url rr =
    rr |> feedMap |> Map.tryFind url

  let feedName url rr =
    let name =
      match rr |> tryFindFeed url with
      | Some { Name = name } -> name + " "
      | None -> ""
    in
      sprintf "%s<%s>" name (url |> string)

  let internal addFeed feed rr =
    { rr with FeedMap = rr |> feedMap |> Map.add (feed.Url) feed }

  let internal removeFeed url rr =
    { rr with FeedMap = rr |> feedMap |> Map.remove url }

  let updateFeeds feeds rr =
    let feedMap' =
      feeds
      |> Seq.fold
          (fun feedMap feed -> feedMap |> Map.add (feed.Url) feed)
          (rr |> feedMap)
    in
      { rr with FeedMap = feedMap' }

  /// src にタグを付ける処理のうち、TagMap を更新する部分
  let internal addTagImpl tagName src rr =
    let srcs' =
      match rr |> tagMap |> Map.tryFind tagName with
      | None -> Set.singleton src
      | Some srcs -> srcs |> Set.add src
    in { rr with TagMap = rr |> tagMap |> Map.add tagName srcs' }

  /// src からタグを外す処理のうち、TagMap を更新する部分
  let internal removeTagImpl tagName src rr =
    let srcs' =
      match rr |> tagMap |> Map.tryFind tagName with
      | None -> Set.empty
      | Some srcs -> srcs |> Set.remove src
    let tagMap' =
      if srcs' |> Set.isEmpty
      then rr |> tagMap |> Map.remove tagName
      else rr |> tagMap |> Map.add tagName srcs'
    in { rr with TagMap = tagMap' }

  let tryFindSource srcName rr =
    rr |> sourceMap |> Map.tryFind srcName

  let addSource src rr =
    let rr =
      match src with
      | Feed feed -> rr |> addFeed feed
      | _ -> rr
    in
      { rr with SourceMap = rr |> sourceMap |> Map.add (src |> RssSource.name) src }

  let removeSource srcName rr =
    match rr |> tryFindSource srcName with
    | None -> rr
    | Some src ->
        let rr =
          match src with
          | Feed feed -> rr |> removeFeed (feed.Url)
          | _ -> rr
        in
          { rr with SourceMap = rr |> sourceMap |> Map.remove srcName }

  /// src にタグを付ける
  let addTag tagName src rr =
    let rr = rr |> addTagImpl tagName src
    let rr =
      match rr |> tryFindSource tagName with
      | Some (Union (tagName, srcs)) ->
        let sourceMap' =
          rr
          |> sourceMap
          |> Map.add tagName (srcs |> Set.add src |> RssSource.union tagName)
        in { rr with SourceMap = sourceMap' }
      | _ ->
        rr
        |> addSource (RssSource.union tagName (Set.singleton src))
    in rr

  /// src からタグを外す
  let removeTag tagName src rr =
    let rr = rr |> removeTagImpl tagName src
    let rr =
      match rr |> tryFindSource tagName with
      | Some (Union (tagName, srcs)) ->
          let srcs' = srcs |> Set.remove src
          let sourceMap' =
            if srcs' |> Set.isEmpty
            then rr |> sourceMap |> Map.remove tagName
            else rr |> sourceMap |> Map.add tagName (srcs' |> RssSource.union tagName)
          in { rr with SourceMap = sourceMap' }
      | _ -> rr
    in rr

  /// src についているタグの集合
  let tagSetOf src rr =
    rr
    |> tagMap
    |> Map.filter (fun tagName srcs ->
        srcs |> Set.contains src
        )
    |> Map.keySet

  let addUnreadItems items rr =
    { rr with UnreadItems = rr.UnreadItems + (items |> Set.ofSeq) }

  let readItem (item: RssItem) rr =
    let feedMap' =
      match rr |> feedMap |> Map.tryFind (item.Url) with
      | None -> rr |> feedMap
      | Some feed ->
          let feed' =
            { feed with DoneSet = feed.DoneSet |> Set.add item }
          in
            rr |> feedMap |> Map.add (feed.Url) feed'
    let unreadItems' =
      rr.UnreadItems
      |> Set.remove item
    in
      { rr with
          FeedMap         = feedMap'
          UnreadItems     = unreadItems'
      }

  let updateAsync src rr =
    async {
      let! (feeds', unreadItems) =
        src
        |> RssSource.ofUnread
        |> RssSource.fetchItemsAsync

      let rr =
        rr
        |> updateFeeds feeds'
        |> addUnreadItems unreadItems

      return (rr, unreadItems)
    }

  let updateAllAsync rr =
    rr |> updateAsync (rr |> allFeedSource)

  let toSpec rr =
    let feeds =
      rr |> allFeeds
    let tags =
      rr
      |> tagMap
      |> Map.map (fun _ src -> src |> Set.map RssSource.name)
    let srcSpecs =
      rr
      |> sourceMap
      |> Map.valueSet
      |> Set.map (RssSource.toSpec)
    in
      {
        Feeds           = feeds
        Tags            = tags
        SourceSpecSet   = srcSpecs
      }

  let ofSpec (spec: RssReaderSpec) =
    let feedMap =
      spec.Feeds
      |> Array.map (fun feed -> (feed.Url, feed))
      |> Map.ofArray
    let rr =
      feedMap
      |> Map.fold (fun rr _ feed -> rr |> addSource (Feed feed)) empty
    let rr =
      spec.SourceSpecSet
      |> Set.map (RssSource.ofSpec feedMap)
      |> Set.fold (fun rr src -> rr |> addSource src) rr
    let rr =
      spec.Tags
      |> Map.fold (fun rr tagName srcNameSet ->
          srcNameSet
          |> Set.fold (fun rr srcName ->
              match rr |> tryFindSource srcName with
              | Some src -> rr |> addTag tagName src
              | None -> rr
              ) rr
          ) rr
    in rr

  let toJson rr =
    rr |> toSpec |> Yaml.customDump

  let ofJson json =
    json |> Yaml.customTryLoad<RssReaderSpec> |> Option.get |> ofSpec

  module Serialize =
    open System.IO

    let load path =
      try
        let json =
          File.ReadAllText(path)
        in
          json |> ofJson |> Some
      with
      | _ -> None

    let loadOrEmpty path =
      match load path with
      | Some rr -> rr
      | None -> empty

    let save path rr =
      let json =
        rr |> toJson
      in
        File.WriteAllText(path, json)
