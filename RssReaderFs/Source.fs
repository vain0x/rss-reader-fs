namespace RssReaderFs

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

  let validate (rr: RssReader) =
    function
    | AllSource
    | TagSource _
      -> pass ()
    | Feed feed ->
        feed |> RssFeed.validate
    | TwitterUser tu ->
        rr.TwitterToken |> Twitter.tryFindUser (tu.ScreenName)
        |> Trial.ignore
