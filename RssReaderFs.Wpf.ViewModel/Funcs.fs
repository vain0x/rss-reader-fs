[<AutoOpen>]
module RssReaderFs.Wpf.ViewModel.Funcs

open RssReaderFs.Core

module MetaArticle =
  let empty =
    {
      ArticleId     = 0L  // dummy value
      Title         = ""
      Date          = ""
      FeedName      = ""
    }

  let ofItem (rr: RssReader) (item: Article) =
    {
      ArticleId     = item.Id
      Title         = item.Title
      Date          = item.Date.ToString("G")
      FeedName      = (Source.findSourceById (rr |> RssReader.ctx) item.SourceId).Name
    }
