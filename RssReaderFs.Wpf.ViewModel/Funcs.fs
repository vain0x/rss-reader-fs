[<AutoOpen>]
module RssReaderFs.Wpf.ViewModel.Funcs

open System
open System.Linq
open RssReaderFs.Core

module MetaArticle =
  let empty =
    MetaArticle(0L, "", "", "", None)

  let ofItem (rr: RssReader) (item: Article) =
    let ctx         = rr |> RssReader.ctx
    let readDate    =
      (ctx |> DbCtx.set<ReadLog>).FirstOrDefault() |> Option.ofObj
      |> Option.map (fun log -> log.Date)
    let feedName    =
      (Source.findSourceById ctx item.SourceId).Name
    in
      MetaArticle(item.Id, item.Title, item.Date.ToString("G"), feedName, readDate)
