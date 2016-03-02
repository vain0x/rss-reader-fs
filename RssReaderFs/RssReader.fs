namespace RssReaderFs

module RssReader =
  type RssReader (feeds: RssFeed []) =

    new () =
      RssReader()

    new (lastUpdate, sources: seq<RssSource>) =
      let feeds =
        sources
        |> Seq.map (Rss.emptyFeed lastUpdate)
        |> Seq.toArray
      RssReader(feeds)

    new (json: string) =
      let feeds =
        Serialize.deserializeJson<RssFeed []>(json)
      RssReader(feeds)

    member this.Add(rhs: RssReader) =
      RssReader(Array.append feeds (rhs.Feeds))

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
