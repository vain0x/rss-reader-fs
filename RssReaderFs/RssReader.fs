namespace RssReaderFs

open System
open System.Collections.Generic

module RssReader =
  let create(feeds: RssFeed []) =
    {
      FeedMap =
        feeds
        |> Array.map (fun feed -> (feed.Url, feed))
        |> Map.ofSeq
      UnreadItems =
        Set.empty
    }

  let internal feedMap (rr: RssReader) =
    rr.FeedMap

  let unreadItems (rr: RssReader) =
    rr.UnreadItems

  let allFeeds rr =
    rr.FeedMap
    |> Map.toArray
    |> Array.map snd

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

  let updateAsync pred rr =
    async {
      let! feedItemsArray =
        rr
        |> allFeeds
        |> Array.filter pred
        |> Array.map (RssFeed.updateAsync)
        |> Async.Parallel

      let (feeds', unreadItemsArray) =
        feedItemsArray |> Array.unzip

      let unreadItems =
        unreadItemsArray
        |> Array.collect id

      let rr =
        rr
        |> updateFeeds feeds'
        |> addUnreadItems unreadItems

      return (rr, unreadItems)
    }

  let updateAllAsync rr =
    rr |> updateAsync (fun _ -> true)

  let tryFindSource url rr =
    rr |> feedMap |> Map.tryFind url

  let sourceName url rr =
    let name =
      match rr |> tryFindSource url with
      | Some { Name = name } -> name + " "
      | None -> ""
    in
      sprintf "%s<%s>" name (url |> string)

  module Serialize =
    let load path =
      path
      |> RssFeed.Serialize.load
      |> Option.map create

    let loadOrEmpty path =
      match load path with
      | Some rr -> rr
      | None -> create [||]
      
    let save path rr =
      rr
      |> allFeeds
      |> RssFeed.Serialize.save path
