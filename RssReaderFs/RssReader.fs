namespace RssReaderFs

open System
open System.Collections.Generic

module RssReader =
  let mutable observerId = 0

  let private newObserverId () =
    observerId
    |> tap (fun obsId ->
        observerId <- observerId + 1
        )
    |> ObserverId

  let create(sources: RssSource []) =
    {
      SourceMap =
        sources
        |> Array.map (fun src -> (src.Uri, src))
        |> Dictionary.ofSeq
      Subscriptions =
        Map.empty
    }

  let internal sourceMap (rr: RssReader) =
    rr.SourceMap

  let internal subscriptions (rr: RssReader) =
    rr.Subscriptions
    
  let sources rr =
    rr.SourceMap
    |> Dictionary.toArray
    |> Array.map snd

  let subscribe obs rr =
    let obsId = newObserverId ()
    in
      { rr with
          Subscriptions = rr |> subscriptions |> Map.add obsId obs
      }

  let unsubscribe obsId rr =
    { rr with
        Subscriptions = rr |> subscriptions |> Map.remove obsId
        }

  let add (source: RssSource) rr =
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

  let private newItems items rr =
    for KeyValue (_, obs) in rr |> subscriptions do
      obs.OnNewItems(items)

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

  let updateAllAsync rr =
    async {
      let! itemsList =
        rr
        |> sources
        |> Array.map (Rss.updateRssAsync)
        |> Async.Parallel
      return
        itemsList
        |> Seq.collect id
        |> Seq.toArray
        |> Array.sortBy (fun item -> item.Date)
        |> flip newItems rr
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
