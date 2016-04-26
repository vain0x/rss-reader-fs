[<AutoOpen>]
module RssReaderFs.Wpf.ViewModel.Funcs

open RssReaderFs

module RssItemRow =
  let empty =
    {
      Title         = ""
      Date          = ""
      FeedName      = ""
    }

  let ofItem (rc: RssClient) (item: RssItem) =
    {
      Title         = item.Title
      Date          = item.Date.ToString("G")
      FeedName      = rc.Reader |> RssReader.feedName item.Url
    }
