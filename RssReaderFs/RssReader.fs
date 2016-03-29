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
      UnreadFeeds =
        Set.empty
    }

  let internal sourceMap (rr: RssReader) =
    rr.SourceMap

  let unreadFeeds (rr: RssReader) =
    rr.UnreadFeeds

  let sources rr =
    rr.SourceMap
    |> Map.toArray
    |> Array.map snd

  let readFeeds rr =
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
          |> tap (fun s -> s.Remove(url) |> ignore)
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
        UnreadFeeds = rr.UnreadFeeds + (items |> Set.ofSeq)
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
    let unreadFeeds' =
      rr.UnreadFeeds
      |> Set.remove item
    in
      { rr with
          SourceMap       = sourceMap'
          UnreadFeeds     = unreadFeeds'
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
        |> Array.sortBy (fun item -> item.Date)

      let rr =
        rr |> updateSources sources'

      return (rr, unreadItems)
    }

  let tryFindSource url rr =
    rr |> sourceMap |> Map.tryFind url

  let sourceName url rr =
    let name =
      match rr |> tryFindSource url with
      | Some { Name = name } -> name + " "
      | None -> ""
    in
      sprintf "%s<%s>" name (url |> string)
