﻿namespace RssReaderFs

open System

type RssClient private (path: string) =
  let mutable reader =
    match path |> RssSource.Serialize.load with
    | Some sources -> RssReader.create(sources)
    | None -> failwithf "Invalid sources: %s" path

  let proj (item: RssItem) =
    item.Title

  let newFeedsEvent =
    Observable.Source<RssItem []>()

  member this.Reader = reader

  member this.Feeds =
    ( (reader |> RssReader.unreadFeeds)
    + (reader |> RssReader.readFeeds)
    )

  member this.AddSource(src) =
    reader <- reader |> RssReader.addSource src

  member this.RemoveSource(url) =
    reader <- reader |> RssReader.removeSource url

  member this.Subscribe(obs) =
    newFeedsEvent.AsObservable |> Observable.subscribe obs

  member this.ReadItem(item) =
    reader <- reader |> RssReader.readItem item

  member this.UpdateAsync(pred) =
    async {
      let! (reader', items) = reader |> RssReader.updateAsync pred
      do reader <- reader'
      if items |> Array.isEmpty |> not then
        // 新フィード受信の通知を出す
        do newFeedsEvent.Next(items)
      return items
    }

  member this.UpdateAllAsync =
    this.UpdateAsync (fun _ -> true)

  static member Create(path) =
    new RssClient(path)

  member this.Save() =
    reader |> RssReader.sources |> RssSource.Serialize.save path
