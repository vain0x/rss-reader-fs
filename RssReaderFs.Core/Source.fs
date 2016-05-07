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

  let allFeeds (ctx: DbCtx): RssFeed [] =
    (ctx.Set<RssFeed>()) |> Array.ofSeq

  let allTwitterUsers (ctx: DbCtx): TwitterUser [] =
    (ctx.Set<TwitterUser>()) |> Array.ofSeq

  let allTags (ctx: DbCtx): Set<string> =
    (ctx.Set<Tag>()) |> Seq.map (fun tag -> tag.TagName) |> Set.ofSeq

  let allAtomicSources ctx: seq<DerivedSource> =
    seq {
      yield! allFeeds        ctx  |> Seq.map ofFeed
      yield! allTwitterUsers ctx  |> Seq.map ofTwitterUser
    }
