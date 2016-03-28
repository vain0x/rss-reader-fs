namespace RssReaderFs

open System
open System.Collections.Generic

module RssReader =
  let create(sources: RssSource []) =
    {
      SourceMap =
        sources
        |> Array.map (fun src -> (src.Url, src))
        |> Dictionary.ofSeq
      ReadFeeds =
        HashSet<_>()
      UnreadFeeds =
        HashSet<_>()
    }

  let internal sourceMap (rr: RssReader) =
    rr.SourceMap

  let unreadFeeds (rr: RssReader) =
    (rr.UnreadFeeds :> seq<RssItem>)

  let readFeeds (rr: RssReader) =
    (rr.ReadFeeds :> seq<RssItem>)

  let sources rr =
    rr.SourceMap
    |> Dictionary.toArray
    |> Array.map snd

  let addSource (source: RssSource) rr =
    { rr with
        SourceMap =
          rr
          |> sourceMap
          |> tap (fun m -> m.Add(source.Url, source))
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
      match (rr |> sourceMap).TryGetValue(item.Url) |> Option.ofTrial with
      | None -> rr |> sourceMap
      | Some src ->
        let src =
          { src with LastUpdate = max (src.LastUpdate) (item.Date) }
        in
          rr
          |> sourceMap
          |> tap (fun m -> m.[item.Url] <- src)
    let unreadFeeds' =
      rr.UnreadFeeds
      |> tap (fun uf -> uf.Remove(item) |> ignore)
    let readFeeds' =
      rr.ReadFeeds
      |> tap (fun rf -> rf.Add(item) |> ignore)
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
            rr.ReadFeeds.Contains(item) |> not
            )
      return newItems
    }

  let tryFindSource url rr =
    (rr |> sourceMap).TryGetValue(url)
    |> Option.ofTrial

  let sourceName url rr =
    let name =
      match rr |> tryFindSource url with
      | Some { Name = name } -> name + " "
      | None -> ""
    in
      sprintf "%s<%s>" name (url |> string)
