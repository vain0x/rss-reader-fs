namespace RssReaderFs.Wpf.ViewModel

[<AutoOpen>]
module Types =
  type RssItemRow =
    {
      Title           : string
      Date            : string
      FeedName        : string
    }
