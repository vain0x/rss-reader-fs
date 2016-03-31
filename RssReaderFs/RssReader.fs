namespace RssReaderFs

open System
open System.Collections.Generic
open FsYaml

module RssReader =
  let empty =
    {
      FeedMap         = Map.empty
      TagMap          = Map.empty
      SourceMap       = Map.empty
    }

  let internal feedMap (rr: RssReader) =
    rr.FeedMap

  let tagMap (rr: RssReader) =
    rr.TagMap

  let sourceMap (rr: RssReader) =
    rr.SourceMap

  let allFeeds rr =
    rr.FeedMap
    |> Map.toArray
    |> Array.map snd

  /// The maximum source
  let allFeedSource rr: RssSource =
    rr
    |> allFeeds
    |> Array.map RssSource.ofFeed
    |> Set.ofArray
    |> (fun srcs -> RssSource.union AllSourceName srcs)

  let tryFindFeed url rr =
    rr |> feedMap |> Map.tryFind url

  let feedName url rr =
    match rr |> tryFindFeed url with
    | Some feed -> feed |> RssFeed.nameUrl
    | None -> sprintf "<%s>" (url |> Url.toString)

  /// フィードを追加する処理のうち、FeedMap を更新する部分
  let internal addFeedImpl feed rr =
    { rr with FeedMap = rr |> feedMap |> Map.add (feed.Url) feed }

  /// フィードを除去する処理のうち、FeedMap を更新する部分
  let internal removeFeedImpl url rr =
    { rr with FeedMap = rr |> feedMap |> Map.remove url }

  let updateFeeds feeds rr =
    let feedMap' =
      feeds
      |> Seq.fold
          (fun feedMap feed -> feedMap |> Map.add (feed.Url) feed)
          (rr |> feedMap)
    in { rr with FeedMap = feedMap' }

  /// src にタグを付ける処理のうち、TagMap を更新する部分
  let internal addTagImpl tagName src rr =
    let srcs' =
      match rr |> tagMap |> Map.tryFind tagName with
      | None -> Set.singleton src
      | Some srcs -> srcs |> Set.add src
    in { rr with TagMap = rr |> tagMap |> Map.add tagName srcs' }

  /// src からタグを外す処理のうち、TagMap を更新する部分
  let internal removeTagImpl tagName src rr =
    let srcs' =
      match rr |> tagMap |> Map.tryFind tagName with
      | None -> Set.empty
      | Some srcs -> srcs |> Set.remove src
    let tagMap' =
      if srcs' |> Set.isEmpty
      then rr |> tagMap |> Map.remove tagName
      else rr |> tagMap |> Map.add tagName srcs'
    in { rr with TagMap = tagMap' }

  let tryFindSource srcName rr =
    match srcName with
    | AllSourceName -> rr |> allFeedSource |> Some
    | _ -> rr |> sourceMap |> Map.tryFind srcName

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

  let rec renameSource oldName newName rr =
    match
      ( rr |> tryFindSource oldName
      , rr |> tryFindSource newName
      ) with
    | (Some src, None) ->
        { rr with
            FeedMap =
              rr
              |> feedMap
              |> Map.map (fun _ -> RssFeed.rename oldName newName)
            TagMap = 
              rr
              |> tagMap
              |> Map.replaceKey oldName newName
              |> Map.map (fun _ -> Set.map (RssSource.rename oldName newName))
            SourceMap =
              rr
              |> sourceMap
              |> Map.replaceKey oldName newName
              |> Map.map (fun _ -> RssSource.rename oldName newName)
            }
    | _ -> rr

  /// src にタグを付ける
  let addTag tagName src rr =
    let rr = rr |> addTagImpl tagName src
    let (rr, old) =
      match rr |> tryFindSource tagName with
      | Some (Union (tagName, srcs)) as old ->
        let sourceMap' =
          rr
          |> sourceMap
          |> Map.add tagName (srcs |> Set.add src |> RssSource.union tagName)
        let rr =
          { rr with SourceMap = sourceMap' }
        in (rr, None)  // タグ付けの障害になるものはなかった、という意味で None を返す
      | _ ->
        rr
        |> addSource (RssSource.union tagName (Set.singleton src))
    in (rr, old)

  /// src からタグを外す
  let removeTag tagName src rr =
    let rr = rr |> removeTagImpl tagName src
    let (rr, old) =
      match rr |> tryFindSource tagName with
      | Some (Union (tagName, srcs)) ->
          let old     = srcs |> Set.tryFind src
          let srcs'   = srcs |> Set.remove src
          let sourceMap' =
            if srcs' |> Set.isEmpty
            then rr |> sourceMap |> Map.remove tagName
            else rr |> sourceMap |> Map.add tagName (srcs' |> RssSource.union tagName)
          let rr =
            { rr with SourceMap = sourceMap' }
          in (rr, old)
      | _ -> (rr, None)
    in (rr, old)

  /// src についているタグの集合
  let tagSetOf src rr =
    rr
    |> tagMap
    |> Map.filter (fun tagName srcs ->
        // srcs のいずれかが、src 全体を部分として含んでいること
        srcs
        |> Set.collect (RssSource.subSources)
        |> Set.contains src
        )
    |> Map.keySet

  let readItem (item: RssItem) rr =
    match rr |> feedMap |> Map.tryFind (item.Url) with
    | None -> rr
    | Some feed ->
        let feed' =
          { feed with DoneSet = feed.DoneSet |> Set.add item }
        let feedMap' =
          rr |> feedMap |> Map.add (feed.Url) feed'
        in { rr with FeedMap = feedMap' }

  let updateAsync src rr =
    async {
      let! (feeds', unreadItems) =
        src
        |> RssSource.ofUnread
        |> RssSource.fetchItemsAsync
      let rr = rr |> updateFeeds feeds'
      return (rr, unreadItems)
    }

  let updateAllAsync rr =
    rr |> updateAsync (rr |> allFeedSource)

  let toSpec rr =
    let feeds =
      rr |> allFeeds
    let tags =
      rr
      |> tagMap
      |> Map.map (fun _ src -> src |> Set.map RssSource.name)
    let srcSpecs =
      rr
      |> sourceMap
      |> Map.valueSet
      |> Set.map (RssSource.toSpec)
    in
      {
        Feeds           = feeds
        Tags            = tags
        SourceSpecSet   = srcSpecs
      }

  let ofSpec (spec: RssReaderSpec) =
    let feedMap =
      spec.Feeds
      |> Array.map (fun feed -> (feed.Url, feed))
      |> Map.ofArray
    let rr =
      feedMap
      |> Map.fold (fun rr _ feed -> rr |> addSource (Feed feed) |> fst) empty
    let rr =
      spec.SourceSpecSet
      |> Set.map (RssSource.ofSpec feedMap)
      |> Set.fold (fun rr src -> rr |> addSource src |> fst) rr
    let rr =
      spec.Tags
      |> Map.fold (fun rr tagName srcNameSet ->
          srcNameSet
          |> Set.fold (fun rr srcName ->
              match rr |> tryFindSource srcName with
              | Some src -> rr |> addTag tagName src |> fst
              | None -> rr
              ) rr
          ) rr
    in rr

  let toJson rr =
    rr |> toSpec |> Yaml.customDump

  let ofJson json =
    json |> Yaml.customTryLoad<RssReaderSpec> |> Option.get |> ofSpec

  module Serialize =
    open System.IO

    let load path =
      try
        let json =
          File.ReadAllText(path)
        in
          json |> ofJson |> Some
      with
      | _ -> None

    let loadOrEmpty path =
      match load path with
      | Some rr -> rr
      | None -> empty

    let save path rr =
      let json =
        rr |> toJson
      in
        File.WriteAllText(path, json)
