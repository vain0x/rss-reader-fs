namespace RssReaderFs

open System
open System.Runtime.Serialization

[<AutoOpen>]
module Domain =
  type RssSource =
    {
      Name: string
      Url: Url
      LastUpdate: DateTime

      // Category, UpdateSpan, etc.
    }

  type RssItem =
    {
      Title: string
      Desc: string option
      Link: string option
      Date: DateTime
      Url: Url
    }

  type RssReader =
    {
      SourceMap     : Map<Url, RssSource>
      ReadFeeds     : Set<RssItem>
      UnreadFeeds   : Set<RssItem>
    }
