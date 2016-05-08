namespace RssReaderFs.Wpf.ViewModel

open System
open RssReaderFs.Core

[<AutoOpen>]
module Types =
  type MetaArticle =
    {
      ArticleId       : Id
      Title           : string
      Date            : string
      FeedName        : string
      ReadDate        : option<DateTime>
    }
