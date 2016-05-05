namespace RssReaderFs

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

  [<Literal>]
  let AllSourceName = "ALL"

  [<Literal>]
  let DefaultConfigName = "Default"
