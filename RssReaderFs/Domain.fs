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

  type SourceName = string

  type RssSourceT<'Feed when 'Feed: comparison> =
    internal
    | Feed          of 'Feed
    | Unread        of RssSourceT<'Feed>
    | Union         of SourceName * Set<RssSourceT<'Feed>>

  type RssSourceSpec =
    RssSourceT<Url>
  
  type RssSource =
    RssSourceT<RssFeed>

  type RssReader =
    {
      /// 購読しているフィード全体。
      FeedMap       : Map<Url, RssFeed>
      /// 使用できるソース全体。
      /// 常に、FeedMap に含まれるすべてのフィードを RssSource.Feed として含む。
      SourceMap     : Map<SourceName, RssSource>
      UnreadItems   : Set<RssItem>
    }

  /// Serializable version
  type RssReaderSpec =
    RssFeed [] * Set<RssSourceSpec>
