namespace RssReaderFs

open System
open System.Collections.Generic

module RssReader =
  let empty =
    {
      FeedMap         = Map.empty
      SourceMap       = Map.empty
      UnreadItems     = Set.empty
    }

  let internal feedMap (rr: RssReader) =
    rr.FeedMap

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
    |> (fun srcs -> RssSource.union ("ALL", srcs))

  let alreadyReadItems rr =
    rr
    |> allFeeds
    |> Array.map (fun src -> src.DoneSet)
    |> Array.fold (+) Set.empty

  let addFeed feed rr =
    { rr with FeedMap = rr |> feedMap |> Map.add (feed.Url) feed }

  let removeFeed url rr =
    { rr with FeedMap = rr |> feedMap |> Map.remove url }

  let updateFeeds feeds rr =
    let feedMap' =
      feeds
      |> Seq.fold
          (fun feedMap feed -> feedMap |> Map.add (feed.Url) feed)
          (rr |> feedMap)
    in
      { rr with FeedMap = feedMap' }

  let addSource src rr =
    { rr with SourceMap = rr.SourceMap |> Map.add (src |> RssSource.name) src }

  let removeSource srcName rr =
    { rr with SourceMap = rr.SourceMap |> Map.remove srcName }

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

  let tryFindFeed url rr =
    rr |> feedMap |> Map.tryFind url

  let feedName url rr =
    let name =
      match rr |> tryFindFeed url with
      | Some { Name = name } -> name + " "
      | None -> ""
    in
      sprintf "%s<%s>" name (url |> string)

  let toSpec rr =
    let feeds =
      rr |> allFeeds
    let srcSpecs =
      rr.SourceMap
      |> Map.valueSet
      |> Set.map (RssSource.toSpec)
    in
      (feeds, srcSpecs)

  let ofSpec (feeds, srcSpecs) =
    let feedMap =
      feeds
      |> Array.map (fun feed -> (feed.Url, feed))
      |> Map.ofArray
    let sourceMap =
      srcSpecs
      |> Set.map (RssSource.ofSpec feedMap)
      |> Set.map (fun src -> (src |> RssSource.name, src))
      |> Map.ofSeq
    in
      {
        FeedMap       = feedMap
        SourceMap     = sourceMap
        UnreadItems   = Set.empty
      } 

  let toJson rr =
    rr |> toSpec |> Serialize.serializeJson<RssReaderSpec>

  let ofJson json =
    json |> Serialize.deserializeJson<RssReaderSpec> |> ofSpec

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
