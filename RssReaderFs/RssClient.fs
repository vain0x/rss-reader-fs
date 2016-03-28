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

  member this.UpdateAsync(pred) =
    async {
      let! items = reader |> RssReader.updateAsync pred

      // 取得済みのフィードを取り除いたもの
      let items =
        items |> Array.filter (fun item ->
          feeds |> Map.containsKey (proj item) |> not
          )

      // 新フィードを保存する
      let feeds' =
        items |> Array.fold (fun feeds item ->
          feeds |> Map.add (item.Title) item
          ) feeds

      do feeds <- feeds'

      // 新フィード受信の通知を出す
      do newFeedsEvent.Next(items)
    }

  member this.UpdateAllAsync =
    this.UpdateAsync (fun _ -> true)

  static member Create(path) =
    new RssClient(path)

  member this.Save() =
    reader |> RssReader.sources |> Rss.Serialize.save path
