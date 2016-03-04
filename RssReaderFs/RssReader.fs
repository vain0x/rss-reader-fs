namespace RssReaderFs

open System
open System.Collections.Generic

[<AutoOpen>]
module RssReaderExtension =
  let mutable observerId = 0

  let newObserverId () =
    observerId
    |> tap (fun obsId ->
        observerId <- observerId + 1
        )

  type RssReader with
    static member Create(sources: RssSource []) =
      {
        SourceMap =
          sources
          |> Array.map (fun src -> (src.Uri, src))
          |> Dictionary.ofSeq
        Subscriptions =
          Map.empty
      }

    member this.Sources =
      this.SourceMap
      |> Dictionary.toArray
      |> Array.map snd

    member this.Subscribe(obs) =
      let obsId = newObserverId () |> ObserverId
      in
        { this with
            Subscriptions = this.Subscriptions |> Map.add obsId obs
        }

    member this.Unsubscribe(obsId) =
      { this with
          Subscriptions = this.Subscriptions |> Map.remove obsId
          }

    member this.NewItems (items: RssItem []) =
      for KeyValue (_, obs) in this.Subscriptions do
        obs.OnNewItems(items)
      this

    member this.ReadItem(item, ?now) =
      let now =
        defaultArg now (DateTime.Now)
      let sourceMap' =
        match this.SourceMap.TryGetValue(item.Uri) |> Option.ofTrial with
        | None -> this.SourceMap
        | Some src ->
          let src =
            { src with LastUpdate = max (src.LastUpdate) now }
          in
            this.SourceMap
            |> tap (fun m -> m.[item.Uri] <- src)
      in
        { this with
            SourceMap = sourceMap'
        }

    member this.UpdateAllAsync() =
      async {
        let! itemsList =
          this.Sources
          |> Array.map (Rss.updateRssAsync)
          |> Async.Parallel
        return
          itemsList
          |> Seq.collect id
          |> Seq.toArray
          |> this.NewItems
      }

    member this.TryFindSource(uri: Uri) =
      this.SourceMap.TryGetValue(uri)
      |> Option.ofTrial
