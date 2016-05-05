namespace RssReaderFs.Wpf.ViewModel

[<AutoOpen>]
module Types =
  type ArticleRow =
    {
      Title           : string
      Date            : string
      FeedName        : string
    }
