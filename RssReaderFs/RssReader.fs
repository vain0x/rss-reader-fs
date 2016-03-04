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
        
  [<CompiledName("Create")>]
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
    
  [<CompiledName("Sources")>]
  let sources rr =
    rr.SourceMap
    |> Dictionary.toArray
    |> Array.map snd

  [<CompiledName("Subscribe")>]
  let subscribe obs rr =
    let obsId = newObserverId () |> ObserverId
    in
      { rr with
          Subscriptions = rr |> subscriptions |> Map.add obsId obs
      }

  [<CompiledName("Unsubscribe")>]
  let unsubscribe obsId rr =
    { rr with
        Subscriptions = rr |> subscriptions |> Map.remove obsId
        }

  [<CompiledName("Add")>]
  let add (source: RssSource) rr =
    { rr with
        SourceMap =
          rr
          |> sourceMap
          |> tap (fun m -> m.Add(source.Uri, source))
    }

  [<CompiledName("Remove")>]
  let remove uri rr =
    { rr with
        SourceMap =
          rr
          |> sourceMap
          |> tap (fun s -> s.Remove(uri) |> ignore)
    }

  [<CompiledName("NewItems")>]
  let private newItems items rr =
    for KeyValue (_, obs) in rr |> subscriptions do
      obs.OnNewItems(items)
    rr

  [<CompiledName("ReadItem")>]
  let readItem item now rr =
    let sourceMap' =
      match (rr |> sourceMap).TryGetValue(item.Uri) |> Option.ofTrial with
      | None -> rr |> sourceMap
      | Some src ->
        let src =
          { src with LastUpdate = max (src.LastUpdate) now }
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
        |> flip newItems rr
    }

  [<CompiledName("UpdateAll")>]
  let updateAll rr =
    rr |> updateAllAsync |> Async.StartAsTask

  [<CompiledName("TryFindSource")>]
  let tryFindSource uri rr =
    (rr |> sourceMap).TryGetValue(uri)
    |> Option.ofTrial
