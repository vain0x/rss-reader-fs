namespace RssReaderFs.Core

open System
open System.Linq
open System.Collections.Generic
open Basis.Core
open FsYaml
open Chessie.ErrorHandling

module RssReader =
  let create (): RssReader =
    let ctx             = new DbCtx()
    in
      {
        Ctx             = ctx
        TwitterToken    = Twitter.fetchAppOnlyToken ctx
        ChangedEvent    = Event<unit>()
      }

  let ctx (rr: RssReader) =
    rr.Ctx

  let set<'t when 't: not struct> rr =
    (rr |> ctx).Set<'t>()

  let private changedEvent (rr: RssReader) = rr.ChangedEvent

  let changed rr: IEvent<unit> =
    (rr |> changedEvent).Publish

  let raisingChanged rr (x: 'x): 'x =
    x |> tap (fun _ -> (rr |> changedEvent).Trigger())

  let private addSource srcName rr =
    trial {
      match Source.tryFindSourceByName (rr |> ctx) srcName with
      | Some _ ->
          return! Trial.fail (SourceAlreadyExists srcName)
      | None ->
          return
            (rr |> set<Entity.Source>).Add(Entity.Source(Name = srcName))
            |> DbCtx.saving (rr |> ctx)
    }

  let addFeed name url rr =
    trial {
      do! RssFeed.validate url
      let! src      = rr |> addSource name
      let feed      = RssFeed(SourceId = src.Id, Url = url)
      (rr |> set<RssFeed>).Add(feed) |> ignore
      |> DbCtx.saving (rr |> ctx)
      |> raisingChanged rr
    }

  let addTwitterUser name rr =
    trial {
      do! rr.TwitterToken |> Twitter.validate name
      let! src      = rr |> addSource name
      let tu        = Entity.TwitterUser(SourceId = src.Id)
      (rr |> set<TwitterUser>).Add(tu) |> ignore
      |> DbCtx.saving (rr |> ctx)
      |> raisingChanged rr
    }

  let private createTag name rr =
    trial {
      let! src      = rr |> addSource name
      let tag       = Entity.Tag(SourceId = src.Id)
      (rr |> set<Tag>).Add(tag) |> ignore
      |> DbCtx.saving (rr |> ctx)
      |> raisingChanged rr
      return tag
    }

  let tryRemoveSource (srcName: string) rr: Result<unit, Error> =
    trial {
      match Source.tryFindByName (rr |> ctx) srcName with
      | Some src ->
          match src |> snd with
          | AllSource ->
              return! fail (SourceCannotBeRemoved AllSourceName)
          | Feed feed ->
              (rr |> set<RssFeed>).Remove(feed) |> ignore
          | TwitterUser tu ->
              (rr |> set<TwitterUser>).Remove(tu) |> ignore
          | TagSource tag ->
              let taggeds =
                (rr |> set<TagToSource>).Where(fun tts -> tts.TagId = tag.SourceId)
              in
                (rr |> set<TagToSource>).RemoveRange(taggeds) |> ignore
          (rr |> ctx).SaveChanges() |> ignore
      | None ->
          return! fail (SourceDoesNotExist srcName)
    }
    |> Trial.lift (raisingChanged rr)

  let renameSource (oldName: string) (newName: string) rr: Result<unit, Error> =
    trial {
      match
        ( Source.tryFindByName (rr |> ctx) oldName
        , Source.tryFindByName (rr |> ctx) newName
        )
        with
      | (Some src, None) ->
          match src |> snd with
          | AllSource
          | TwitterUser _
            -> return! fail (SourceCannotBeRenamed oldName)
          | Feed _
          | TagSource _
            -> (src |> fst).Name <- newName |> DbCtx.saving (rr |> ctx)
      | (None, _) ->
          return! fail (SourceDoesNotExist oldName)
      | (_, Some _) ->
          return! fail (SourceAlreadyExists newName)
    }
    |> Trial.lift (raisingChanged rr)

  let private addTagToSource (tag: Tag) (src: Source) rr =
    let tagToSource = TagToSource(TagId = tag.SourceId, SourceId = src.Id)
    (rr |> set<TagToSource>).Add(tagToSource) |> ignore
    |> DbCtx.saving (rr |> ctx)
    |> raisingChanged rr

  /// src にタグを付ける
  /// TODO: 循環的なタグづけを禁止する
  let addTag (tagName: TagName) (srcName: string) rr: Result<unit, Error> =
    trial {
      match
        ( (Source.tryFindByName (rr |> ctx) tagName)
        , (Source.tryFindByName (rr |> ctx) srcName)
        ) with
      | (Some (_, TagSource tag), Some (src, _)) ->
          rr |> addTagToSource tag src
      | (None, Some (src, _)) ->
          let! tag = rr |> createTag tagName
          in rr |> addTagToSource tag src
      | (Some src, _) ->
          return! fail (src |> Source.name |> SourceAlreadyExists)
      | (_, None) ->
          return! fail (srcName |> SourceDoesNotExist)
    }

  /// src からタグを外す
  let removeTag (tagName: TagName) (srcName: string) rr: Result<unit, Error> =
    trial {
      match
        ( (Source.tryFindByName (rr |> ctx) tagName)
        , (Source.tryFindByName (rr |> ctx) srcName) ) with
      | (Some (_, TagSource tag), Some (src, _)) ->
          match
            (rr |> set<TagToSource>)
              .FirstOrDefault(fun tts -> tts.TagId = tag.SourceId && tts.SourceId = src.Id)
            |> Option.ofObj
            with
          | Some tts ->
              (rr |> set<TagToSource>).Remove(tts) |> ignore
              |> DbCtx.saving (rr |> ctx)
              |> raisingChanged rr
          | None ->
              return! () |> warn (SourceDoesNotHaveTag (srcName, tagName))
      | (_, None) ->
          return! fail (srcName |> SourceDoesNotExist)
      | _ ->
          return! fail (tagName |> SourceIsNotATag)
    }

  /// Note: The read date of items already read can't be updated.
  let readItem (articleId: Id) rr: ReadLog =
    (rr |> set<ReadLog>).Find(articleId)
    |> Option.ofObj
    |> Option.getOrElse (fun () ->
      (rr |> set<ReadLog>).Add(ReadLog(ArticleId = articleId, Date = DateTime.Now))
      )
    |> raisingChanged rr

  let unreadItems src rr: Article [] =
    let allUnreads =
      let logs = rr |> set<ReadLog>
      query {
        for article in (rr |> set<Article>) do
        where (not (query {
          for log in logs do
          where (article.Id = log.ArticleId)
          exists true
          }))
        select article
      }
    let (xs: seq<Article>) =
      match src |> snd with
      | AllSource ->
          allUnreads :> seq<Article>
      | Feed feed ->
          allUnreads.Where(fun article -> article.SourceId = feed.SourceId) :> seq<Article>
      | TwitterUser tu ->
          allUnreads.Where(fun article -> article.SourceId = tu.SourceId) :> seq<Article>
      | TagSource tag ->
          allUnreads |> Seq.filter (fun article ->
            article.SourceId |> Source.isTaggedBy (rr |> ctx) tag
            )
    in xs |> Seq.toArray

  let rec fetchItemsAsync (src: DerivedSource) rr: Async<Article []> =
    async {
      match src |> snd with
      | AllSource ->
          let! itemArrayArray =
            Source.allAtomicSources (rr |> ctx)
            |> Seq.map (fun src -> rr |> fetchItemsAsync src)
            |> Async.Parallel
          return itemArrayArray |> Array.collect id
      | Feed feed ->
          let! items = feed.Url |> RssFeed.downloadAsync feed.SourceId
          return items |> Seq.toArray
      | TwitterUser tu ->
          let screenName  = src |> Source.name
          let! statuses   = rr.TwitterToken |> Twitter.userTweetsAsync screenName (tu.SinceId)
          let items       = [| for status in statuses -> Article.ofTweet status tu.SourceId |]
          if items |> Array.length > 0 then
            let maxId = statuses |> Seq.map (fun status -> status.Id) |> Seq.max
            do tu.SinceId <- max maxId tu.SinceId
          return items
      | TagSource tag ->
          let! itemArrayArray =
            Source.findTaggedSources (rr |> ctx) tag
            |> Seq.choose (fun src ->
                Source.tryFindByName (rr |> ctx) src.Name
                |> Option.map (fun src -> rr |> fetchItemsAsync src))
            |> Async.Parallel
          return itemArrayArray |> Array.collect id
   }

  /// Returns only new articles.
  let updateAsync (src: DerivedSource) rr: Async<Article []> =
    let ctx = rr |> ctx
    ctx.SaveChanges() |> ignore
    ctx |> DbCtx.withTransaction (fun _ -> async {
        let! items = rr |> fetchItemsAsync src
        let (news, _) =
          items |> Array.uniqueBy (fun item -> (item.Title, item.Date, item.SourceId))
          |> Array.partition (fun item -> item |> Article.insert ctx)
        ctx.SaveChanges() |> ignore
        return news
      })
    |> raisingChanged rr

  let save rr: unit =
    (rr |> ctx).SaveChanges() |> ignore
