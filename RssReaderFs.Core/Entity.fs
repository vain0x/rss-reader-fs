namespace RssReaderFs.Core

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema

[<AutoOpen>]
module Entity =
  [<AllowNullLiteral>]
  type EntityWithId() =
    [<Key>]
    member val Id = 0L with get, set

  [<AllowNullLiteral>]
  type Article() =
    inherit EntityWithId()

    [<Required>]
    member val Title = "" with get, set

    [<Required; Index>]
    member val Url = "" with get, set

    member val Desc = (None: option<string>) with get, set

    member val Link = (None: option<string>) with get, set

    [<Required; Index>]
    member val Date = DateTime.Now with get, set

  [<AllowNullLiteral>]
  type ReadLog() =
    [<Key>]
    member val ArticleId = 0L with get, set

    [<Required>]
    member val Date = DateTime.Now with get, set

  [<AllowNullLiteral>]
  type TwitterUser() =
    [<Key>]
    member val ScreenName = "" with get, set

    member val ReadDate = DateTime.Now with get, set

  [<AllowNullLiteral>]
  type RssFeed() =
    inherit EntityWithId()

    [<Required; Index(IsUnique = true)>]
    member val Name = "" with get, set

    [<Required; Index(IsUnique = true)>]
    member val Url = "" with get, set

  [<AllowNullLiteral>]
  type Tag() =
    inherit EntityWithId()

    [<Required; Index>]
    member val TagName = "" with get, set

    [<Required; Index>]
    member val SourceName = "" with get, set

  [<AllowNullLiteral>]
  type Config() =
    [<Key>]
    member val Name = "" with get, set

    member val BearToken = "" with get, set
