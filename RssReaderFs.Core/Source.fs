namespace RssReaderFs.Core

open System.Linq
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

  let tryFindFeed (ctx: DbCtx) url: option<RssFeed> =
    ctx.Set<RssFeed>().FirstOrDefault(fun feed -> feed.Url = url)
    |> Option.ofObj

  let tryFindTwitterUser (ctx: DbCtx) (name: string): option<TwitterUser> =
    ctx.Set<TwitterUser>().FirstOrDefault(fun tu -> tu.ScreenName = name)
    |> Option.ofObj
    
  let findTaggedSourceNames (ctx: DbCtx) tagName: Set<string> =
    ctx.Set<Tag>()
      .Where(fun tag -> tag.TagName = tagName)
      .Select(fun tag -> tag.SourceName)
    |> Set.ofSeq

  let tryFindTagSource (ctx: DbCtx) (tagName: TagName): option<DerivedSource> =
    if ctx.Set<Tag>().FirstOrDefault(fun tag -> tag.TagName = tagName) = null
    then None
    else ofTag tagName |> Some

  let tryFindSource ctx (srcName: string): option<DerivedSource> =
    if srcName = AllSourceName
    then all |> Some
    else
      seq {
        yield tryFindFeed         ctx srcName |> Option.map (ofFeed)
        yield tryFindTwitterUser  ctx srcName |> Option.map (ofTwitterUser)
        yield tryFindTagSource    ctx srcName
      }
      |> Seq.tryPick id
