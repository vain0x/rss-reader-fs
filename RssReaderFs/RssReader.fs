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
        |> Seq.map (Rss.emptyFeed)
        |> Seq.toArray
      in
        RssReader(feeds)

    new (json: string) =
      let feeds =
        Serialize.deserializeJson<RssFeed []>(json)
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
        let! newFeeds =
          feeds
          |> Seq.map (Rss.updateFeedAsync)
          |> Async.Parallel
        return RssReader(newFeeds)
      }
      
    // 全アイテムの時系列順
    member this.Timeline =
      feeds
      |> Seq.collect (fun feed -> feed.Items)
      |> Seq.toList
      |> List.sortBy (fun item -> item.Date)
      |> List.rev

    member this.Sources =
      feeds
      |> Array.map (fun feed -> feed.Source)

    member this.Feeds =
      feeds

    member this.EmptyFeeds =
      feeds
      |> Array.map (fun feed -> 
          { feed
            with
              Items = []
              OldItems = []
          })
          
    member this.SerializedFeeds =
      Serialize.serializeJson(this.EmptyFeeds)
