namespace RssReaderFs

open System
open System.Runtime.Serialization

[<AutoOpen>]
module Domain =
  type RssSource =
    {
      Name          : string
      Url           : Url

      // Category, UpdateSpan, etc.
    }

  type RssItem =
    {
      Title         : string
      Desc          : option<string>
      Link          : option<string>
      Date          : DateTime
      Url           : Url
    }

  type RssReader =
    {
      SourceMap     : Map<Url, RssSource>
      ReadFeeds     : Set<RssItem>
      UnreadFeeds   : Set<RssItem>
    }
