[<AutoOpen>]
module RssReaderFs.Wpf.ViewModel.Funcs

open RssReaderFs.Core

module ArticleRow =
  let empty =
    {
      Title         = ""
      Date          = ""
      FeedName      = ""
    }

  let ofItem (rr: RssReader) (item: Article) =
    {
      Title         = item.Title
      Date          = item.Date.ToString("G")
      FeedName      = (Source.findSourceById (rr |> RssReader.ctx) item.SourceId).Name
    }
