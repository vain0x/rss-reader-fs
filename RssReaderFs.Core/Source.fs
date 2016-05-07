namespace RssReaderFs.Core

open System.Linq
open Chessie.ErrorHandling

module Source =
  let all src =
    (src, AllSource)

  let ofFeed (src, feed) =
    (src, Feed feed)

  let ofTwitterUser (src, screenName) =
    (src, TwitterUser screenName)

  let ofTag (src, tag) =
    (src, TagSource tag)

  let name ((src: Source), _) =
    src.Name

  let allSource ctx =
    let src = (ctx |> DbCtx.set<Source>).First(fun src -> src.Name = AllSourceName)
    in (src, AllSource)

  let allFeeds ctx: (Source * RssFeed) [] =
    query {
      for feed in ctx |> DbCtx.set<RssFeed> do
      join src in (ctx |> DbCtx.set<Source>) on (feed.SourceId = src.Id)
      select (src, feed)
    }
    |> Seq.toArray

  let allTwitterUsers ctx: (Source * TwitterUser) [] =
    query {
      for tu in ctx |> DbCtx.set<TwitterUser> do
      join src in (ctx |> DbCtx.set<Source>) on (tu.SourceId = src.Id)
      select (src, tu)
    }
    |> Seq.toArray

  let allTags ctx: list<Source * Tag> =
    query {
      for tag in ctx |> DbCtx.set<Tag> do
      join src in (ctx |> DbCtx.set<Source>) on (tag.SourceId = src.Id)
      select (src, tag)
    }
    |> Seq.toList

  let allAtomicSources ctx: seq<DerivedSource> =
    seq {
      yield! allFeeds        ctx |> Seq.map ofFeed
      yield! allTwitterUsers ctx |> Seq.map ofTwitterUser
    }

  let findSourceById ctx (srcId: Id) =
    (ctx |> DbCtx.set<Source>).Find(srcId) // must exist

  let private findDerivedById<'t when 't: not struct and 't: null> ctx (srcId: Id) =
    (ctx |> DbCtx.set<'t>).Find(srcId) |> Option.ofObj

  let findById ctx (srcId: Id): DerivedSource =
    let src = findSourceById ctx srcId
    let der =
      seq {
        if src.Name = AllSourceName then
          yield Some AllSource
        yield findDerivedById<RssFeed      > ctx srcId |> Option.map Feed
        yield findDerivedById<TwitterUser  > ctx srcId |> Option.map TwitterUser
        yield findDerivedById<Tag          > ctx srcId |> Option.map TagSource
      }
      |> Seq.tryPick id
      |> Option.get  // must exist
    in (src, der)

  let tryFindSourceByName ctx (srcName: string) =
    (ctx |> DbCtx.set<Source>).FirstOrDefault(fun src -> src.Name = srcName)
    |> Option.ofObj

  let private tryFindDerivedByName<'t when 't: not struct and 't: null> ctx srcName =
    tryFindSourceByName ctx srcName |> Option.bind (fun src ->
      findDerivedById<'t> ctx (src.Id) |> Option.map (fun der ->
        (src, der)
        ))

  let tryFindByName ctx (srcName: string): option<DerivedSource> =
    if srcName = AllSourceName
    then allSource ctx |> Some
    else
      seq {
        yield tryFindDerivedByName<RssFeed      > ctx srcName |> Option.map ofFeed
        yield tryFindDerivedByName<TwitterUser  > ctx srcName |> Option.map ofTwitterUser
        yield tryFindDerivedByName<Tag          > ctx srcName |> Option.map ofTag
      }
      |> Seq.tryPick id

  let findTaggedSources ctx (tag: Tag): list<Source> =
    query {
      for tts in ctx |> DbCtx.set<TagToSource> do
      where (tts.TagId = tag.SourceId)
      join src in (ctx |> DbCtx.set<Source>) on (tts.SourceId = src.Id)
      select src
    }
    |> Seq.toList

  /// src についているタグのリスト
  let tagsOf ctx (srcName: string): list<Source * Tag> =
    query {
      for src in ctx |> DbCtx.set<Source> do
      where (src.Name = srcName)
      join tts in (ctx |> DbCtx.set<TagToSource>) on (src.Id = tts.SourceId)
      join tagSrc in (ctx |> DbCtx.set<Source>) on (tts.TagId = tagSrc.Id)
      join tag in (ctx |> DbCtx.set<Tag>) on (tagSrc.Id = tag.SourceId)
      select (tagSrc, tag)
    }
    |> Seq.toList

  let dump ctx (src: DerivedSource): string =
    let srcName = src |> name
    match src |> snd with
    | AllSource         -> AllSourceName
    | Feed feed         -> sprintf "feed %s %s" srcName feed.Url
    | TwitterUser tu    -> sprintf "twitter-user %s" srcName
    | TagSource tag     ->
        let srcs        = findTaggedSources ctx tag
        let srcNames    = srcs |> List.map (fun src -> src.Name) |> String.concat " "
        in sprintf "tag %s %s" srcName srcNames
