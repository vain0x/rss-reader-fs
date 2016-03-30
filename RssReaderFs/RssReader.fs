namespace RssReaderFs

open System
open System.Collections.Generic

module RssReader =
  let create(sources: RssSource []) =
    {
      SourceMap =
        sources
        |> Array.map (fun src -> (src.Url, src))
        |> Map.ofSeq
      UnreadItems =
        Set.empty
    }

  let internal sourceMap (rr: RssReader) =
    rr.SourceMap

  let unreadItems (rr: RssReader) =
    rr.UnreadItems

  let sources rr =
    rr.SourceMap
    |> Map.toArray
    |> Array.map snd

  let alreadyReadItems rr =
    rr
    |> sources
    |> Array.map (fun src -> src.DoneSet)
    |> Array.fold (+) Set.empty

  let addSource (source: RssSource) rr =
    { rr with
        SourceMap =
          rr
          |> sourceMap
          |> Map.add source.Url source
    }

  let removeSource url rr =
    { rr with
        SourceMap =
          rr
          |> sourceMap
          |> Map.remove url
    }

  let updateSources sources rr =
    let sourceMap' =
      sources
      |> Seq.fold
          (fun sourceMap src -> sourceMap |> Map.add (src.Url) src)
          (rr |> sourceMap)
    in
      { rr with
          SourceMap = sourceMap'
      }

  let addUnreadItems items rr =
    { rr with
        UnreadItems = rr.UnreadItems + (items |> Set.ofSeq)
    }

  let readItem (item: RssItem) rr =
    let sourceMap' =
      match rr |> sourceMap |> Map.tryFind (item.Url) with
      | None -> rr |> sourceMap
      | Some src ->
          let src' =
            { src with DoneSet = src.DoneSet |> Set.add item }
          in
            rr |> sourceMap |> Map.add (src.Url) src'
    let unreadItems' =
      rr.UnreadItems
      |> Set.remove item
    in
      { rr with
          SourceMap       = sourceMap'
          UnreadItems     = unreadItems'
      }

  let updateAsync pred rr =
    async {
      let! srcItemsArray =
        rr
        |> sources
        |> Array.filter pred
        |> Array.map (RssSource.updateAsync)
        |> Async.Parallel

      let (sources', unreadItemsArray) =
        srcItemsArray |> Array.unzip

      let unreadItems =
        unreadItemsArray
        |> Array.collect id

      let rr =
        rr
        |> updateSources sources'
        |> addUnreadItems unreadItems

      return (rr, unreadItems)
    }

  let updateAllAsync rr =
    rr |> updateAsync (fun _ -> true)

  let tryFindSource url rr =
    rr |> sourceMap |> Map.tryFind url

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
      |> RssSource.Serialize.load
      |> Option.map create

    let loadOrEmpty path =
      match load path with
      | Some rr -> rr
      | None -> create [||]
      
    let save path rr =
      rr
      |> sources
      |> RssSource.Serialize.save path
