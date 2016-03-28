namespace RssReaderFs

open System

type RssClient private (path: string) =
  let mutable reader =
    match path |> Rss.Serialize.load with
    | Some sources -> RssReader.create(sources)
    | None -> failwithf "Invalid sources: %s" path

  let mutable feeds =
    (Map.empty: Map<string, RssItem>)

  let proj (item: RssItem) =
    item.Title

  let newFeedsEvent =
    Observable.Source<RssItem []>()

  let procNewFeeds (items: RssItem []) =
    items
    |> Array.filter (fun item ->  // 取得済みのフィードを取り除く
        feeds |> Map.containsKey (proj item) |> not
        )
    |> tap (fun items ->  // 新フィードを保存する
        feeds <-
          items
          |> Array.fold (fun feeds item ->
              feeds |> Map.add (item.Title) item
              ) feeds
        )
    |> (fun items ->
        newFeedsEvent.Next(items)
        )

  member this.Reader = reader

  member this.Feeds = feeds

  member this.Add(src) =
    reader <- reader |> RssReader.add src

  member this.Remove(uri) =
    reader <- reader |> RssReader.remove uri

  member this.Subscribe(obs) =
    newFeedsEvent.AsObservable |> Observable.subscribe obs

  member this.ReadItem(item) =
    reader <- reader |> RssReader.readItem item
    feeds  <- feeds |> Map.remove (proj item)

  member this.UpdateAllAsync =
    async {
      let! items = reader |> RssReader.updateAllAsync
      do procNewFeeds items
    }

  static member Create(path) =
    new RssClient(path)

  member this.Save() =
    reader |> RssReader.sources |> Rss.Serialize.save path
