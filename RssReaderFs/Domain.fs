namespace RssReaderFs

open System
open System.Collections.Generic
open System.Runtime.Serialization

[<AutoOpen>]
module Domain =
  type RssSource =
    {
      [<field: DataMember(Name = "name")>]
      Name: string

      [<field: DataMember(Name = "url")>]
      Url: Url
      
      [<field: DataMember(Name = "lastUpdate")>]
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
      SourceMap     : Dictionary<Url, RssSource>
      ReadFeeds     : HashSet<RssItem>
      UnreadFeeds   : HashSet<RssItem>
    }
