namespace RssReaderFs

open System

/// RSSクライアントクラス。
/// 純粋である RssReader に、自己更新、ファイルIOの機能を加えたもの。
type RssClient private (path: string) =
  let mutable reader =
    RssReader.Serialize.loadOrEmpty path

  member this.Reader = reader

  member internal this.AddFeed(feed) =
    reader <- reader |> RssReader.addFeed feed

  member internal this.RemoveFeed(url) =
    reader <- reader |> RssReader.removeFeed url

  member this.AddSource(src) =
    let (rr', old) = reader |> RssReader.addSource src
    let () = reader <- rr'
    in old

  member this.RemoveSource(srcName) =
    let (rr', old) = reader |> RssReader.removeSource srcName
    let () = reader <- rr'
    in old

  member this.AddTag(tagName, src) =
    let (rr', old) = reader |> RssReader.addTag tagName src
    let () = reader <- rr'
    in old

  member this.RemoveTag(tagName, src) =
    let (rr', old) = reader |> RssReader.removeTag tagName src
    let () = reader <- rr'
    in old

  member this.ReadItem(item) =
    reader <- reader |> RssReader.readItem item

  member this.UpdateAsync(src) =
    async {
      let! (reader', items) = reader |> RssReader.updateAsync src
      do reader <- reader'
      return items
    }

  member this.UpdateAllAsync =
    async {
      let! (reader', items) = reader |> RssReader.updateAllAsync
      do reader <- reader'
      return items
    }

  static member Create(path) =
    new RssClient(path)

  member this.Save() =
    reader |> RssReader.Serialize.save path
