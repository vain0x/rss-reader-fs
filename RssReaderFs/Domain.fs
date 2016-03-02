namespace RssReaderFs

open System

[<AutoOpen>]
module Domain =
  type RssSource =
    {
      Name: string
      Uri: Uri

      // Category, UpdateSpan, etc.
    }

  type RssItem =
    {
      Title: string
      Desc: string option
      Link: string option
      Date: DateTime
      Uri: Uri
    }

  type RssFeed =
    {
      Source      : RssSource
      LastUpdate  : DateTime
      Items       : seq<RssItem>
      OldItems    : seq<RssItem> list
    }
