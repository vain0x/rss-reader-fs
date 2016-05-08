namespace RssReaderFs.Wpf.ViewModel

[<AutoOpen>]
module Types =
  type MetaArticle =
    {
      Title           : string
      Date            : string
      FeedName        : string
    }
