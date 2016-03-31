namespace RssReaderFs

open System

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
  
  type TagName = SourceName

  type RssSourceT<'Feed when 'Feed: comparison> =
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
      /// 各フィードについて、対応するソースが SourceMap に加えられる。
      FeedMap       : Map<Url, RssFeed>
      /// タグ全体。
      /// 各タグについて、それがついたソース全体からなる Union が SourceMap に加えられる。
      TagMap        : Map<TagName, Set<RssSource>>
      /// 使用できるソース全体。
      /// 常に、FeedMap に含まれるすべてのフィードを RssSource.Feed として含む。
      /// 常に、TagMap に含まれるタグ付き集合を RssSource.Union として含む。
      SourceMap     : Map<SourceName, RssSource>
    }

  /// 情報の重複が少ない形式。
  /// シリアライズするときなどに使う。
  type RssReaderSpec =
    {
      Feeds         : RssFeed []
      Tags          : Map<TagName, Set<SourceName>>
      SourceSpecSet : Set<RssSourceSpec>
    }

  type Error =
    | NameConflict

  /// 全フィードからなる RssSource の名前
  [<Literal>]
  let AllSourceName = "ALL"
