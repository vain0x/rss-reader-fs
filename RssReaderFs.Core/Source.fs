namespace RssReaderFs.Core

open Chessie.ErrorHandling

module Source =
  let all =
    AllSource

  let ofFeed feed =
    Feed feed

  let ofTwitterUser screenName =
    TwitterUser screenName

  let ofTag tagName =
    TagSource tagName

  let name =
    function
    | AllSource         -> AllSourceName
    | Feed feed         -> feed.Name
    | TwitterUser tu    -> tu.ScreenName
    | TagSource tagName -> tagName
