namespace RssReaderFs

open System
open Chessie.ErrorHandling

/// RSSクライアントクラス。
/// RssReader に更新通知の機能を加えたもの。
type RssClient private () =
  let reader =
    RssReader.create ()

  let changed = Event<unit>()
  let raiseChanged _ = changed.Trigger()

  member this.Changed = changed.Publish

  member this.Reader = reader

  member this.TryAddSource(src) =
    reader |> RssReader.tryAddSource src
    |> tap raiseChanged

  member this.TryRemoveSource(srcName) =
    reader |> RssReader.tryRemoveSource srcName
    |> tap raiseChanged

  member this.RenameSource(oldName, newName) =
    reader |> RssReader.renameSource oldName newName
    |> tap raiseChanged
     
  member this.AddTag(tagName, srcName) =
    reader |> RssReader.addTag tagName srcName
    |> tap raiseChanged

  member this.RemoveTag(tagName, srcName) =
    reader |> RssReader.removeTag tagName srcName
    |> tap raiseChanged

  member this.ReadItem(item) =
    reader |> RssReader.readItem item
    |> tap raiseChanged

  member this.UpdateAsync(src) =
    reader |> RssReader.updateAsync src
    |> tap raiseChanged

  member this.UpdateAllAsync =
    this.UpdateAsync(Source.all)

  static member Create() =
    new RssClient()

  member this.Save() =
    reader.Ctx.SaveChanges() |> ignore
