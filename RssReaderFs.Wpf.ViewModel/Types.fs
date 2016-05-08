namespace RssReaderFs.Wpf.ViewModel

open System
open RssReaderFs.Core

[<AutoOpen>]
module Types =
  type MetaArticle(articleId, title, date, feedName, readDate) =
    inherit WpfViewModel.Base()

    let mutable readDate        = (readDate: option<DateTime>)

    member this.ArticleId       = (articleId: Id)
    member this.Title           = (title: string)
    member this.Date            = (date: string)
    member this.FeedName        = (feedName: string)

    member this.ReadDate
      with get () = readDate
      and  set v  = readDate <- v; this.RaisePropertyChanged("ReadDate")
