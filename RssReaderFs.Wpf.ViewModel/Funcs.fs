[<AutoOpen>]
module RssReaderFs.Wpf.ViewModel.Funcs

open System
open System.Linq
open RssReaderFs.Core

module MetaArticle =
  let empty =
    {
      ArticleId     = 0L  // dummy value
      Title         = ""
      Date          = ""
      FeedName      = ""
      ReadDate      = None
    }

  let ofItem (rr: RssReader) (item: Article) =
    let ctx         = rr |> RssReader.ctx
    let readDate    =
      (ctx |> DbCtx.set<ReadLog>).FirstOrDefault() |> Option.ofObj
      |> Option.map (fun log -> log.Date)
    {
      ArticleId     = item.Id
      Title         = item.Title
      Date          = item.Date.ToString("G")
      FeedName      = (Source.findSourceById ctx item.SourceId).Name
      ReadDate      = readDate 
    }
