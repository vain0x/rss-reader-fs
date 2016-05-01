namespace RssReaderFs

open System

[<AutoOpen>]
module Domain =
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
  
  type TagName =
    | TagName of SourceName
  with
    override this.ToString() =
      let (TagName s) = this in s

  type RssSource =
    | Feed          of RssFeed
    | Unread        of SourceName
    | Union         of SourceName * Set<SourceName>

  type RssReader =
    {
      /// 購読しているフィード全体。
      /// 各 Feed ソースに対応する対を含む。
      FeedMap       : Map<Url, SourceName>
      /// タグ全体。
      /// 各タグについて、それがついたソース全体からなる Union が SourceMap に加えられる。
      TagMap        : Map<SourceName, Set<TagName>>
      /// 使用できるソース全体。
      /// 常に、FeedMap に含まれるすべてのフィードを RssSource.Feed として含む。
      /// 常に、TagMap に含まれるタグ付き集合を RssSource.Union として含む。
      SourceMap     : Map<SourceName, RssSource>
    }

  /// 全フィードからなる RssSource の名前
  [<Literal>]
  let AllSourceName = "ALL"
