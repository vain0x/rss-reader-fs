namespace RssReaderFs

open System
open System.Collections.Generic

module RssReader =
  let empty =
    {
      FeedMap         = Map.empty
      UnreadItems     = Set.empty
      Sources         = Set.empty
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
    |> RssSource.union

  let alreadyReadItems rr =
    rr
    |> allFeeds
    |> Array.map (fun src -> src.DoneSet)
    |> Array.fold (+) Set.empty

  let addFeed feed rr =
    { rr with
        FeedMap =
          rr
          |> feedMap
          |> Map.add feed.Url feed
    }

  let removeFeed url rr =
    { rr with
        FeedMap =
          rr
          |> feedMap
          |> Map.remove url
    }

  let updateFeeds feeds rr =
    let feedMap' =
      feeds
      |> Seq.fold
          (fun feedMap feed -> feedMap |> Map.add (feed.Url) feed)
          (rr |> feedMap)
    in
      { rr with
          FeedMap = feedMap'
      }

  let addUnreadItems items rr =
    { rr with
        UnreadItems = rr.UnreadItems + (items |> Set.ofSeq)
    }

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

  let tryFindSource url rr =
    rr |> feedMap |> Map.tryFind url

  let sourceName url rr =
    let name =
      match rr |> tryFindSource url with
      | Some { Name = name } -> name + " "
      | None -> ""
    in
      sprintf "%s<%s>" name (url |> string)

  let toSpec rr =
    let feeds =
      rr |> allFeeds
    let srcSpecs =
      rr.Sources |> Set.map (RssSource.toSpec)
    in
      (feeds, srcSpecs)

  let ofSpec (feeds, srcSpecs) =
    let feedMap =
      feeds
      |> Array.map (fun feed -> (feed.Url, feed))
      |> Map.ofArray
    let sources =
      srcSpecs
      |> Set.map (RssSource.ofSpec feedMap)
    in
      {
        FeedMap       = feedMap
        UnreadItems   = Set.empty
        Sources       = sources
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
