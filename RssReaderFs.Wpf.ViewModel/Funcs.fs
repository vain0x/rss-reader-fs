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

  let ofItem (rc: RssReader) (item: Article) =
    {
      Title         = item.Title
      Date          = item.Date.ToString("G")
      FeedName      = rc |> RssReader.feedName item.Url
    }
