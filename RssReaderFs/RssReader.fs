namespace RssReaderFs

open System
open System.Collections.Generic

module RssReader =
  let create(sources: RssSource []) =
    {
      SourceMap =
        sources
        |> Array.map (fun src -> (src.Uri, src))
        |> Dictionary.ofSeq
    }

  let internal sourceMap (rr: RssReader) =
    rr.SourceMap

  let sources rr =
    rr.SourceMap
    |> Dictionary.toArray
    |> Array.map snd

  let addSource (source: RssSource) rr =
    { rr with
        SourceMap =
          rr
          |> sourceMap
          |> tap (fun m -> m.Add(source.Uri, source))
    }

  let remove uri rr =
    { rr with
        SourceMap =
          rr
          |> sourceMap
          |> tap (fun s -> s.Remove(uri) |> ignore)
    }

  let readItem item rr =
    let sourceMap' =
      match (rr |> sourceMap).TryGetValue(item.Uri) |> Option.ofTrial with
      | None -> rr |> sourceMap
      | Some src ->
        let src =
          { src with LastUpdate = max (src.LastUpdate) (item.Date) }
        in
          rr
          |> sourceMap
          |> tap (fun m -> m.[item.Uri] <- src)
    in
      { rr with
          SourceMap = sourceMap'
      }

  let updateAsync pred rr =
    async {
      let! itemsList =
        rr
        |> sources
        |> Array.filter pred
        |> Array.map (Rss.updateRssAsync)
        |> Async.Parallel
      return
        itemsList
        |> Seq.collect id
        |> Seq.toArray
        |> Array.sortBy (fun item -> item.Date)
    }

  let tryFindSource uri rr =
    (rr |> sourceMap).TryGetValue(uri)
    |> Option.ofTrial

  let sourceName uri rr =
    let name =
      match rr |> tryFindSource uri with
      | Some { Name = name } -> name + " "
      | None -> ""
    in
      sprintf "%s<%s>" name (uri |> string)
