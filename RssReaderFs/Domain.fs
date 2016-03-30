namespace RssReaderFs

open System
open System.Runtime.Serialization

[<AutoOpen>]
module Domain =
  type RegexPattern = string

  type RssItem =
    {
      Title         : string
      Desc          : option<string>
      Link          : option<string>
      Date          : DateTime
      Url           : Url
    }

  type RssFeed =
    {
      Name          : string
      Url           : Url
      DoneSet       : Set<RssItem>

      // Category, UpdateSpan, etc.
    }

  type RssSourceT<'Feed when 'Feed: comparison> =
    internal
    | Feed          of 'Feed
    | Unread        of RssSourceT<'Feed>
    | Union         of Set<RssSourceT<'Feed>>

  type RssSourceSpec =
    RssSourceT<Url>
  
  type RssSource =
    RssSourceT<RssFeed>

  type RssReader =
    {
      FeedMap       : Map<Url, RssFeed>
      UnreadItems   : Set<RssItem>
    }
