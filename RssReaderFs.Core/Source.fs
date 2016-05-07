namespace RssReaderFs.Core

open System.Linq
open Chessie.ErrorHandling

module Source =
  let private all =
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

  let allSource ctx =
    all

  let allFeeds ctx: RssFeed [] =
    (ctx |> DbCtx.set<RssFeed>) |> Array.ofSeq

  let allTwitterUsers ctx: TwitterUser [] =
    (ctx |> DbCtx.set<TwitterUser>) |> Array.ofSeq

  let allTags ctx: Set<string> =
    (ctx |> DbCtx.set<Tag>) |> Seq.map (fun tag -> tag.TagName) |> Set.ofSeq

  let allAtomicSources ctx: seq<DerivedSource> =
    seq {
      yield! allFeeds        ctx  |> Seq.map ofFeed
      yield! allTwitterUsers ctx  |> Seq.map ofTwitterUser
    }

  let tryFindFeedByUrl ctx url: option<RssFeed> =
    (ctx |> DbCtx.set<RssFeed>).FirstOrDefault(fun feed -> feed.Url = url)
    |> Option.ofObj

  let tryFindFeedByName ctx srcName =
    (ctx |> DbCtx.set<RssFeed>).FirstOrDefault(fun feed -> feed.Name = srcName)
    |> Option.ofObj

  let tryFindTwitterUser ctx (name: string): option<TwitterUser> =
    (ctx |> DbCtx.set<TwitterUser>).FirstOrDefault(fun tu -> tu.ScreenName = name)
    |> Option.ofObj
    
  let tryFindTagSource ctx (tagName: TagName): option<DerivedSource> =
    if (ctx |> DbCtx.set<Tag>).FirstOrDefault(fun tag -> tag.TagName = tagName) = null
    then None
    else ofTag tagName |> Some

  let tryFindByName ctx (srcName: string): option<DerivedSource> =
    if srcName = AllSourceName
    then all |> Some
    else
      seq {
        yield tryFindFeedByName   ctx srcName |> Option.map (ofFeed)
        yield tryFindTwitterUser  ctx srcName |> Option.map (ofTwitterUser)
        yield tryFindTagSource    ctx srcName
      }
      |> Seq.tryPick id

  let findTaggedSourceNames ctx tagName: Set<string> =
    (ctx |> DbCtx.set<Tag>)
      .Where(fun tag -> tag.TagName = tagName)
      .Select(fun tag -> tag.SourceName)
    |> Set.ofSeq

  /// src についているタグのリスト
  let tagsOf ctx (srcName: string): list<TagName> =
    (ctx |> DbCtx.set<Tag>)
      .Where(fun tag -> tag.SourceName = srcName)
      .Select(fun tag -> tag.TagName)
    |> Seq.toList

  let feedName ctx (url: string): string =
    match tryFindFeedByUrl ctx url with
    | Some feed -> sprintf "%s <%s>" feed.Name url
    | None -> sprintf "<%s>" url

  let dump ctx (src: DerivedSource): string =
    let srcName = src |> name
    match src with
    | AllSource         -> AllSourceName
    | Feed feed         -> sprintf "feed %s %s" srcName feed.Url
    | TwitterUser _     -> sprintf "twitter-user %s" srcName
    | TagSource tagName ->
        let srcNames = findTaggedSourceNames ctx tagName
        in sprintf "tag %s %s" srcName (srcNames |> String.concat " ")
