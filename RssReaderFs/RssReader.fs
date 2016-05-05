namespace RssReaderFs

open System
open System.Collections.Generic
open FsYaml
open Chessie.ErrorHandling

module RssReader =
  let empty =
    {
      FeedMap         = Map.empty
      TagMap          = Map.empty
      SourceMap       = Map.empty
      TwitterToken    = Twitter.getAppOnlyToken ()
    }

  let internal feedMap (rr: RssReader) =
    rr.FeedMap

  let tagMap (rr: RssReader) =
    rr.TagMap

  let sourceMap (rr: RssReader) =
    rr.SourceMap

  let allFeeds rr =
    rr |> sourceMap |> Seq.choose (fun (KeyValue (_, v)) ->
        match v with
        | Feed feed -> Some feed
        | _ -> None
        )
    |> Seq.toArray

  let twitterUsers rr =
    rr |> sourceMap |> Seq.choose (fun (KeyValue (_, v)) ->
        match v with
        | TwitterUser(name, _) -> Some name
        | _ -> None
        )
    |> Seq.toArray

  /// The maximum source
  let allFeedSource rr: RssSource =
    let allUnion =
      rr |> sourceMap
      |> Map.valueSet
      |> Set.collect (fun src ->
          src |> RssSource.atomSources rr |> Set.map (RssSource.name)
          )
    in RssSource.union AllSourceName allUnion

  let tryFindFeed url rr =
    rr |> feedMap |> Map.tryFind url
    |> Option.bind (fun name ->
        match rr |> sourceMap |> Map.tryFind name with
        | Some (Feed feed) -> Some feed
        | _ -> None
        )

  let feedName url rr =
    match rr |> tryFindFeed url with
    | Some feed -> feed |> RssFeed.nameUrl
    | None -> sprintf "<%s>" url

  /// フィードを追加する処理のうち、FeedMap を更新する部分
  let internal addFeedImpl feed (rr: RssReader) =
    { rr with FeedMap = rr |> feedMap |> Map.add (feed.Url) (feed.Name) }

  /// フィードを除去する処理のうち、FeedMap を更新する部分
  let internal removeFeedImpl url (rr: RssReader) =
    { rr with FeedMap = rr |> feedMap |> Map.remove url }

  /// src にタグを付ける処理のうち、TagMap を更新する部分
  let internal addTagImpl tagName srcName rr =
    let tags' =
      match rr |> tagMap |> Map.tryFind srcName with
      | None -> Set.singleton tagName
      | Some tags -> tags |> Set.add tagName
    in { rr with TagMap = rr |> tagMap |> Map.add srcName tags' }

  /// src からタグを外す処理のうち、TagMap を更新する部分
  let internal removeTagImpl tagName srcName rr =
    let tags' =
      match rr |> tagMap |> Map.tryFind srcName with
      | None -> Set.empty
      | Some tags -> tags |> Set.remove tagName
    let tagMap' =
      if tags' |> Set.isEmpty
      then rr |> tagMap |> Map.remove srcName
      else rr |> tagMap |> Map.add srcName tags'
    in { rr with TagMap = tagMap' }

  let tryFindSource srcName rr =
    match srcName with
    | AllSourceName -> rr |> allFeedSource |> Some
    | _ -> rr |> sourceMap |> Map.tryFind srcName

  let tryFindTaggedSources (tagName: TagName) rr =
    rr |> sourceMap |> Map.tryFind (string tagName)
    |> Option.bind (fun src ->
        match src with
        | Union (_, srcNames) ->
            srcNames |> Set.choose (fun name -> rr |> tryFindSource name) |> Some
        | _ -> None
        )

  let addSource src rr =
    let rr =
      match src with
      | Feed feed -> rr |> addFeedImpl feed
      | _ -> rr
    let (rr, old) =
      match rr |> sourceMap |> Map.update (src |> RssSource.name) (Some src) with
      | (sourceMap', None) ->
          ({ rr with SourceMap = sourceMap' }, None)
      | (_, old) -> (rr, old)
    in (rr, old)

  let removeSource srcName rr =
    match rr |> tryFindSource srcName with
    | None -> (rr, None)
    | Some src ->
        let rr =
          match src with
          | Feed feed ->
              rr |> removeFeedImpl (feed.Url)
          | Union (tagName, srcs)
            when rr |> tagMap |> Map.containsKey tagName ->
              { rr with TagMap = rr |> tagMap |> Map.remove tagName }
          | _ -> rr
        let (sourceMap', old) =
          rr |> sourceMap |> Map.update srcName None
        let rr =
          { rr with SourceMap = sourceMap' }
        in (rr, old)

  let renameSource oldName newName rr =
    match
      ( rr |> tryFindSource oldName
      , rr |> tryFindSource newName
      ) with
    | (Some src, None) ->
        { rr with
            FeedMap =
              rr
              |> feedMap
              |> Map.map (fun _ -> replace oldName newName)
            TagMap = 
              rr
              |> tagMap
              |> Map.replaceKey oldName newName
              |> Map.map (fun _ -> Set.map (replace (TagName oldName) (TagName newName)))
            SourceMap =
              rr
              |> sourceMap
              |> Map.replaceKey oldName newName
              |> Map.map (fun _ -> RssSource.rename oldName newName)
            }
    | _ -> rr

  let tryAddSource src rr =
    trial {
      match rr |> addSource src with
      | (_, Some _) ->
          return!
            rr |> warn
              ("The name has already been taken: " + (src |> RssSource.name) + ".")
      | (rr, None) ->
          do! src |> RssSource.validate rr |> Trial.mapExnToMessage
          return rr
    }

  let tryRemoveSource srcName rr =
    trial {
      match rr |> removeSource srcName with
      | (rr, Some _) ->
          return rr
      | (_, None) ->
          return! Trial.failf "Source '%s' doesn't exist." srcName
    }

  /// src にタグを付ける
  let addTag tagName srcName rr =
    let rr = rr |> addTagImpl tagName srcName
    match rr |> tryFindSource (string tagName) with
    | Some (Union (_, srcNames)) ->
        let srcNames'   = srcNames |> Set.add srcName |> RssSource.union (string tagName)
        let sourceMap'  = rr |> sourceMap |> Map.add (string tagName) srcNames'
        let rr          = { rr with SourceMap = sourceMap' }
        in (rr, None)  // タグ付けの障害になるものはなかった、という意味で None を返す
    | _ ->
        rr |> addSource (RssSource.union (string tagName) (Set.singleton srcName))

  /// src からタグを外す
  let removeTag tagName src rr =
    let rr = rr |> removeTagImpl tagName src
    match rr |> tryFindSource (string tagName) with
    | Some (Union (tagName, srcNames)) ->
        let old         = srcNames |> Set.tryFind src
        let srcNames'   = srcNames |> Set.remove src
        let sourceMap'  =
          if srcNames' |> Set.isEmpty
          then rr |> sourceMap |> Map.remove tagName
          else rr |> sourceMap |> Map.add tagName (srcNames' |> RssSource.union tagName)
        let rr =
          { rr with SourceMap = sourceMap' }
        in (rr, old)
    | _ -> (rr, None)

  /// src についているタグの集合
  let tagSetOf srcName rr =
    rr
    |> tagMap
    |> Map.tryFind srcName
    |> Option.getOr Set.empty

  let readItem (item: RssItem) rr =
    match rr |> tryFindFeed (item.Url) with
    | None -> rr
    | Some feed ->
        let feed' =
          { feed with DoneSet = feed |> RssFeed.doneSet |> Set.add item }
        let sourceMap' =
          rr |> sourceMap |> Map.add (feed.Name) (Feed feed')
        in { rr with SourceMap = sourceMap' }

  let updateSource (update: RssSourceUpdate) rr =
    rr
    |> fold' update.DoneSet
        (fun (KeyValue (srcName, doneSet')) rr ->
          match rr |> tryFindSource srcName with
          | Some (Feed feed) ->
              let src'          = Feed { feed with DoneSet = doneSet' }
              let sourceMap'    = rr |> sourceMap |> Map.add srcName src'
              in { rr with SourceMap = sourceMap' }
          | _ -> rr
        )

  let rec fetchItemsAsync src rr =
    async {
      match src with
      | Feed feed ->
          let! (doneSet, items)   = feed |> RssFeed.updateAsync
          let update              = [{ DoneSet = Map.singleton feed.Name doneSet }]
          return (update, items)
      | Unread srcName ->
          match rr |> tryFindSource srcName with
          | None ->
              return ([], [||])
          | Some src ->
              let! (updates, items)   = rr |> fetchItemsAsync src
              let rr'                 = rr |> updateSource (RssSourceUpdate.mergeMany updates)
              return (updates, src |> RssSource.unreadItems rr' items)
      | Union (srcName, srcNames) ->
          return!
            srcNames |> Set.toArray |> Array.choose (fun srcName ->
                rr |> tryFindSource srcName
                |> Option.map (fun src -> rr |> fetchItemsAsync src)
                )
            |> Async.Parallel
            |> Async.map (fun x ->
                let (updateListArray, itemArrayArray) = x |> Array.unzip
                let updates   = updateListArray |> Array.toList |> List.collect id
                let items     = itemArrayArray  |> Array.collect id
                in (updates, items)
                )
      | TwitterUser (name, date) ->
          let! statuses   = rr.TwitterToken |> Twitter.userTweetsAsync name
          let items       = [| for status in statuses -> RssItem.ofTweet status |]
          return ([], items)
   }

  let updateAsync src rr =
    async {
      let! (updates, items)   = rr |> fetchItemsAsync (src |> RssSource.ofUnread)
      let rr'                 = rr |> updateSource (updates |> RssSourceUpdate.mergeMany)
      return (rr', items)
    }

  let ofSpec (spec: RssReaderSpec) =
    {
      FeedMap         = spec.FeedMap
      TagMap          = spec.TagMap
      SourceMap       = spec.SourceMap
      TwitterToken    =
        match spec.BearToken with
        | None            -> Twitter.getAppOnlyToken()
        | Some bearToken  -> Twitter.createAppOnlyToken bearToken
    }

  let toSpec (rr: RssReader) =
    {
      FeedMap         = rr.FeedMap
      TagMap          = rr.TagMap
      SourceMap       = rr.SourceMap
      BearToken       = rr.TwitterToken.BearerToken |> Some
    }

  let toYaml rr =
    rr |> toSpec |> Yaml.dump<RssReaderSpec>

  let ofYaml yaml =
    yaml |> Yaml.load<RssReaderSpec> |> ofSpec

  module Serialize =
    open System.IO

    let load path =
      try
        let yaml =
          File.ReadAllText(path)
        in
          yaml |> ofYaml |> Some
      with
      | _ -> None

    let loadOrEmpty path =
      match load path with
      | Some rr -> rr
      | None -> empty

    let save path rr =
      let yaml =
        rr |> toYaml
      in
        File.WriteAllText(path, yaml)
