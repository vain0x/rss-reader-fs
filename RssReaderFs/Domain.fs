namespace RssReaderFs

open System
open System.Runtime.Serialization

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

  type RssSource =
    {
      Name          : string
      Url           : Url
      DoneSet       : Set<RssItem>

      // Category, UpdateSpan, etc.
    }

  type RssReader =
    {
      SourceMap     : Map<Url, RssSource>
      UnreadFeeds   : Set<RssItem>
    }
