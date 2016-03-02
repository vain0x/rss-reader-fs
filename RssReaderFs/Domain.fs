namespace RssReaderFs

open System
open System.Runtime.Serialization

[<AutoOpen>]
module Domain =
  type RssSource =
    {
      [<field: DataMember(Name = "name")>]
      Name: string

      [<field: DataMember(Name = "uri")>]
      Uri: Uri

      // Category, UpdateSpan, etc.
    }

  type RssItem =
    {
      Title: string
      Desc: string option
      Link: string option
      Date: DateTime
      Uri: Uri
    }

  type RssFeed =
    {
      Source      : RssSource
      LastUpdate  : DateTime
      Items       : seq<RssItem>
      OldItems    : seq<RssItem> list
    }
