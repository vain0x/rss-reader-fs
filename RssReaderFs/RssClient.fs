namespace RssReaderFs

open System

/// RSSクライアントクラス。
/// 純粋である RssReader に、自己更新、イベント生成、ファイルIOの機能を加えたもの。
type RssClient private (path: string) =
  let mutable reader =
    match path |> RssReader.Serialize.load with
    | Some rr -> rr
    | None -> failwithf "Invalid sources: %s" path

  let proj (item: RssItem) =
    item.Title

  let newItemsEvent =
    Observable.Source<RssItem []>()

  member this.Reader = reader

  member this.AddSource(src) =
    reader <- reader |> RssReader.addSource src

  member this.RemoveSource(url) =
    reader <- reader |> RssReader.removeSource url

  member this.Subscribe(obs) =
    newItemsEvent.AsObservable |> Observable.subscribe obs

  member this.ReadItem(item) =
    reader <- reader |> RssReader.readItem item

  member this.UpdateAsync(pred) =
    async {
      let! (reader', items) = reader |> RssReader.updateAsync pred
      do reader <- reader'
      if items |> Array.isEmpty |> not then
        // 新フィード受信の通知を出す
        do newItemsEvent.Next(items)
      return items
    }

  member this.UpdateAllAsync =
    this.UpdateAsync (fun _ -> true)

  static member Create(path) =
    new RssClient(path)

  member this.Save() =
    reader |> RssReader.Serialize.save path
