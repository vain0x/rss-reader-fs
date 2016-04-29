namespace RssReaderFs

open System
open Chessie.ErrorHandling

/// RSSクライアントクラス。
/// 純粋である RssReader に、自己更新、ファイルIOの機能を加えたもの。
type RssClient private (path: string) =
  let mutable reader =
    RssReader.Serialize.loadOrEmpty path

  let changed = Event<unit>()

  member this.Changed = changed.Publish

  member this.Reader
    with get () = reader
    and  set rr =
      reader <- rr
      changed.Trigger(())

  member this.AddSource(src) =
    let (rr', old) = reader |> RssReader.addSource src
    let () = this.Reader <- rr'
    in old

  member this.TryAddSource(src) =
    trial {
      let! rr = reader |> RssReader.tryAddSource src
      this.Reader <- rr
    }

  member this.RemoveSource(srcName) =
    let (rr', old) = reader |> RssReader.removeSource srcName
    let () = this.Reader <- rr'
    in old

  member this.TryRemoveSource(srcName) =
    trial {
      let! rr = reader |> RssReader.tryRemoveSource srcName
      this.Reader <- rr
    }

  member this.RenameSource(oldName, newName) =
    let rr  = reader
    let () = this.Reader <- reader |> RssReader.renameSource oldName newName
    in rr <> reader
     
  member this.AddTag(tagName, src) =
    let (rr', old) = reader |> RssReader.addTag tagName src
    let () = this.Reader <- rr'
    in old

  member this.RemoveTag(tagName, src) =
    let (rr', old) = reader |> RssReader.removeTag tagName src
    let () = this.Reader <- rr'
    in old

  member this.ReadItem(item) =
    this.Reader <- reader |> RssReader.readItem item

  member this.UpdateAsync(src) =
    async {
      let! (reader', items) = reader |> RssReader.updateAsync src
      do this.Reader <- reader'
      return items
    }

  member this.UpdateAllAsync =
    this.UpdateAsync(reader |> RssReader.allFeedSource)

  static member Create(path) =
    new RssClient(path)

  member this.Save() =
    reader |> RssReader.Serialize.save path
