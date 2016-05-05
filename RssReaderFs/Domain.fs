namespace RssReaderFs

open CoreTweet

[<AutoOpen>]
module Domain =
  type TagName          = string

  type RssSource =
    | AllSource
    | Feed              of RssFeed
    | TwitterUser       of TwitterUser
    | TagSource         of TagName

  type RssReader =
    {
      Ctx               : DbCtx
      TwitterToken      : OAuth2Token
    }

  [<Literal>]
  let AllSourceName = "ALL"

  [<Literal>]
  let DefaultConfigName = "Default"
