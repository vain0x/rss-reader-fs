namespace RssReaderFs

open System
open CoreTweet

[<AutoOpen>]
module Domain =
  type TagName          = string

  type Source =
    | AllSource
    | Feed              of RssFeed
    | TwitterUser       of TwitterUser
    | TagSource         of TagName

  type RssReader =
    {
      Ctx               : DbCtx
      TwitterToken      : OAuth2Token
      ChangedEvent      : Event<unit>
    }

  type Error =
    | ExnError                      of Exception
    | SourceAlreadyExists           of string
    | SourceDoesNotExist            of string
    | SourceCannotBeRemoved         of string
    | SourceCannotBeRenamed         of string
    | SourceDoesNotHaveTag          of srcName: string * tagName: TagName

  [<Literal>]
  let AllSourceName = "ALL"

  [<Literal>]
  let DefaultConfigName = "Default"
