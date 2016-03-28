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
      ReadFeeds =
        Set.empty
      UnreadFeeds =
        Set.empty
    }

  let internal sourceMap (rr: RssReader) =
    rr.SourceMap

  let unreadFeeds (rr: RssReader) =
    rr.UnreadFeeds

  let readFeeds (rr: RssReader) =
    rr.ReadFeeds

  let sources rr =
    rr.SourceMap
    |> Map.toArray
    |> Array.map snd

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

  let readItem item rr =
    let sourceMap' =
      match rr |> sourceMap |> Map.tryFind item.Url with
      | None -> rr |> sourceMap
      | Some src ->
        let src =
          { src with LastUpdate = max (src.LastUpdate) (item.Date) }
        in
          rr
          |> sourceMap
          |> Map.add item.Url src
    let unreadFeeds' =
      rr.UnreadFeeds
      |> Set.remove item
    let readFeeds' =
      rr.ReadFeeds
      |> Set.add item
    in
      {
        SourceMap       = sourceMap'
        ReadFeeds       = readFeeds'
        UnreadFeeds     = unreadFeeds'
      }

  let updateAsync pred rr =
    async {
      let! items =
        rr
        |> sources
        |> Array.filter pred
        |> Array.map (Rss.updateRssAsync)
        |> Async.Parallel
      let items =
        items
        |> Seq.collect id
        |> Seq.toArray
        |> Array.sortBy (fun item -> item.Date)
      let newItems =
        items
        |> Array.filter (fun item ->
            rr.ReadFeeds |> Set.contains item |> not
            )
      return newItems
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
