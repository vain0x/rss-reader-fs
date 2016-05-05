namespace RssReaderFs

open System
open System.Linq
open System.Collections.Generic
open FsYaml
open Chessie.ErrorHandling

module RssReader =
  let create () =
    let ctx             = new DbCtx()
    let configOpt       = ctx.Set<Config>().Find(DefaultConfigName) |> Option.ofObj
    let token           =
      match configOpt with
      | Some config ->
          Twitter.createAppOnlyToken(config.BearToken)
      | None ->
          Twitter.getAppOnlyToken()
          |> tap (fun token ->
              // Save BearToken
              let config = Config(Name = DefaultConfigName, BearToken = token.BearerToken)
              ctx.Set<Config>().Add(config) |> ignore
              ctx.SaveChanges() |> ignore
              )
    in
      {
        Ctx             = ctx
        TwitterToken    = token
      }

  let ctx (rr: RssReader) =
    rr.Ctx

  let set<'t when 't: not struct> rr =
    (rr |> ctx).Set<'t>()

  let allFeeds rr =
    rr |> set<RssFeed> |> Array.ofSeq

  let allTags rr =
    rr |> set<Tag> |> Seq.map (fun tag -> tag.TagName) |> Set.ofSeq

  let twitterUsers rr =
    rr |> set<TwitterUser> |> Array.ofSeq

  let allFeedSource rr: RssSource =
    RssSource.all

  let tryFindFeed url rr =
    (rr |> set<RssFeed>).FirstOrDefault(fun feed -> feed.Url = url)
    |> Option.ofObj

  let tryFindTwitterUser (name: string) rr =
    (rr |> set<TwitterUser>).Find(name)
    |> Option.ofObj
    
  let findTaggedSourceNames tagName rr =
    (rr |> set<Tag>)
      .Where(fun tag -> tag.TagName = tagName)
      .Select(fun tag -> tag.SourceName)
    |> Set.ofSeq

  let tryFindTagSource (tagName: TagName) rr =
    if (rr |> set<Tag>).FirstOrDefault(fun tag -> tag.TagName = tagName) = null
    then None
    else RssSource.ofTag tagName |> Some

  let feedName url rr =
    match rr |> tryFindFeed url with
    | Some feed -> feed |> RssFeed.nameUrl
    | None -> sprintf "<%s>" url

  let tryFindSource srcName rr =
    if srcName = AllSourceName
    then RssSource.all |> Some
    else
      seq {
        yield rr |> tryFindFeed         srcName |> Option.map (RssSource.ofFeed)
        yield rr |> tryFindTwitterUser  srcName |> Option.map (RssSource.ofTwitterUser)
        yield rr |> tryFindTagSource    srcName
      }
      |> Seq.tryPick id

  let allAtomicSources rr =
    seq {
      yield! rr |> allFeeds       |> Seq.map RssSource.ofFeed
      yield! rr |> twitterUsers   |> Seq.map RssSource.ofTwitterUser
    }

  let addFeed feed rr =
    (rr |> ctx).Set<RssFeed>().Add(feed) |> DbCtx.saving (rr |> ctx)

  let addTwitterUser tu rr =
    (rr |> ctx).Set<TwitterUser>().Add(tu) |> DbCtx.saving (rr |> ctx) |> ignore

  let tryAddSource src rr =
    trial {
      let srcName = src |> RssSource.name
      match rr |> tryFindSource srcName with
      | Some _ ->
          return! () |> warn (sprintf "The name has already been taken: %s." srcName)
      | None ->
          do! src |> RssSource.validate rr |> Trial.mapExnToMessage
          match src with
          | AllSource
          | TagSource _     -> () // never
          | Feed feed       -> (rr |> set<RssFeed>).Add(feed) |> ignore
          | TwitterUser tw  -> (rr |> set<TwitterUser>).Add(tw) |> ignore
          (rr |> ctx).SaveChanges() |> ignore
    }

  let tryRemoveSource srcName rr =
    trial {
      match rr |> tryFindSource srcName with
      | Some src ->
          match src with
          | AllSource ->
              return! Trial.failf "Source '%s' can't be removed." AllSourceName
          | Feed feed ->
              (rr |> set<RssFeed>).Remove(feed) |> ignore
          | TwitterUser tu ->
              (rr |> set<TwitterUser>).Remove(tu) |> ignore
          | TagSource tagName ->
              let rows = (rr |> set<Tag>).Where(fun tag -> tag.TagName = tagName)
              (rr |> set<Tag>).RemoveRange(rows) |> ignore
          (rr |> ctx).SaveChanges() |> ignore
      | None ->
          return! Trial.failf "Source '%s' doesn't exist." srcName
    }

  /// src にタグを付ける
  /// TODO: 循環的なタグづけを禁止する
  let addTag tagName srcName rr =
    trial {
      match rr |> tryFindSource tagName with
      | Some (TagSource _)
      | None ->
          let tag = Tag(TagName = tagName, SourceName = srcName)
          (rr |> set<Tag>).Add(tag) |> ignore
          |> DbCtx.saving (rr |> ctx)
      | Some src ->
          return! Trial.failf "Source '%s' does exist." (src |> RssSource.name)
    }

  /// src からタグを外す
  let removeTag tagName srcName rr =
    trial {
      let rows =
        (rr |> set<Tag>).Where(fun tag -> tag.TagName = tagName && tag.SourceName = srcName)
      if rows.Any() then
        (rr |> set<Tag>).RemoveRange(rows) |> ignore
        |> DbCtx.saving (rr |> ctx)
      else
        return! Trial.warnf () "Source '%s' doesn't has the tag '%s'." srcName tagName
    }

  /// src についているタグの集合
  let tagSetOf srcName rr =
    (rr |> set<Tag>)
      .Where(fun tag -> tag.SourceName = srcName)
      .Select(fun tag -> tag.TagName)
    |> Set.ofSeq

  /// Note: The read date of items already read can't be updated.
  let readItem (item: RssItem) rr =
    (rr |> set<ReadLog>).Find(item.Id)
    |> Option.ofObj
    |> Option.getOrElse (fun () ->
      (rr |> set<ReadLog>).Add(ReadLog(RssItemId = item.Id, Date = DateTime.Now))
      )

  let rec fetchItemsAsync src rr =
    async {
      match src with
      | AllSource ->
          let! itemArrayArray =
            rr |> allAtomicSources
            |> Seq.map (fun src -> rr |> fetchItemsAsync src)
            |> Async.Parallel
          return itemArrayArray |> Array.collect id
      | Feed feed ->
          let! items = feed |> RssFeed.downloadAsync
          return items |> Seq.toArray
      | TwitterUser tu ->
          let! statuses   = rr.TwitterToken |> Twitter.userTweetsAsync (tu.ScreenName)
          let items       = [| for status in statuses -> RssItem.ofTweet status |]
          return items
      | TagSource tagName ->
          let! itemArrayArray =
            rr |> findTaggedSourceNames tagName
            |> Seq.choose (fun src ->
                rr |> tryFindSource src
                |> Option.map (fun src -> rr |> fetchItemsAsync src))
            |> Async.Parallel
          return itemArrayArray |> Array.collect id
   }

  let updateAsync src rr =
    let ctx = rr |> ctx
    ctx.SaveChanges() |> ignore
    ctx |> DbCtx.withTransaction (fun _ -> async {
        let! items = rr |> fetchItemsAsync src
        let (news, _) =
          items |> Array.uniqueBy (fun item -> (item.Title, item.Date, item.Url))
          |> Array.partition (fun item -> item |> RssItem.insert ctx)
        ctx.SaveChanges() |> ignore
        return news
      })
