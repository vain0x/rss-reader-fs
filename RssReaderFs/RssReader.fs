namespace RssReaderFs

open System
open System.Collections.Generic

module RssReader =
  type RssReader
    ( feeds: RssFeed []
    , sourceMap: Dictionary<Uri, RssSource>
    ) =

    new () =
      RssReader([])

    new (feeds: RssFeed []) =
      let dict =
        feeds
        |> Seq.map (fun feed ->
            let src = feed.Source
            in (src.Uri, src)
            )
        |> Dictionary.ofSeq
      in
        RssReader(feeds, dict)

    new (sources: seq<RssSource>) =
      let feeds =
        sources
        |> Seq.map (fun src -> RssFeed(src))
        |> Seq.toArray
      in
        RssReader(feeds)

    member this.Add(rhs: RssReader) =
      RssReader(Array.append feeds (rhs.Feeds))

    member this.SourceFilter(pred) =
      let (feeds, removed) =
        feeds
        |> Array.partition pred
      (RssReader(feeds), removed)
      
    member this.Update() =
      async {
        // TODO: 実装
        do! Async.Sleep(1000)
      }
      
    member this.TryFindSource(uri) =
      sourceMap.TryGetValue(uri)
      |> Option.ofTrial

    member this.Sources =
      feeds
      |> Array.map (fun feed -> feed.Source)

    member this.Feeds =
      feeds

  module Serialize =
    open System.IO

    let load path =
      try
        let json =
          File.ReadAllText(path)
        let sources =
          Serialize.deserializeJson<RssSource []>(json)
        in
          RssReader(sources) |> Some
      with
      | _ -> None

    let save path (r: RssReader) =
      let json =
        Serialize.serializeJson(r.Sources)
      in
        File.WriteAllText(path, json)
