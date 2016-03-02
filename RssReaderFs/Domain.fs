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
    Source: RssSource
    Title: string
    Desc: string option
    Link: string option
    Date: DateTime option
  }

type RssFeed =
  {
    Source: RssSource
    Items: seq<RssItem>
  }
