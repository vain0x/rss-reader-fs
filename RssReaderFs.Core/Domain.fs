namespace RssReaderFs.Core

open System
open CoreTweet

[<AutoOpen>]
module Domain =
  type Id               = int64
  type TagName          = string

  type DerivedSourceUnion =
    | AllSource
    | Feed              of RssFeed
    | TwitterUser       of TwitterUser
    | TagSource         of Tag

  type DerivedSource =
    Source * DerivedSourceUnion

  type RssReader =
    {
      Ctx               : DbCtx
      TwitterToken      : option<OAuth2Token>
      ChangedEvent      : Event<unit>
    }

  type Error =
    | ExnError                      of Exception
    | TwitterDisabled
    | SourceAlreadyExists           of string
    | SourceDoesNotExist            of string
    | SourceCannotBeRemoved         of string
    | SourceCannotBeRenamed         of string
    | SourceDoesNotHaveTag          of srcName: string * tagName: TagName
    | SourceIsNotATag               of srcName: string

  [<Literal>]
  let AllSourceName = "ALL"

  [<Literal>]
  let DefaultConfigName = "Default"

  module SecretSettings =
    let consumerKeySecret = (None: option<string * string>)
