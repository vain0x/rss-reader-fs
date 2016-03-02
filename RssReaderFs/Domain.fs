[<AutoOpen>]
module Domain

open System

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
    Date: DateTime option
    Uri: Uri
  }

type RssFeed =
  {
    Source      : RssSource
    LastUpdate  : DateTime
    Items       : seq<RssItem>
    OldItems    : seq<RssItem> list
  }
