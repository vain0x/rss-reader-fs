namespace RssReaderFs

module RssSource =
  let ofFeed (feed: RssFeed) =
    Feed feed

  let ofUnread source =
    Unread source

  let union sources =
    Union sources

  let rec toFeeds =
    function
    | Feed feed ->
        Set.singleton feed
    | Unread src ->
        src |> toFeeds
    | Union srcs ->
        srcs |> Set.collect toFeeds

  /// items: このソースが受信対象とするフィードが発信したアイテムの列
  let rec filterItems items =
    function
    | Feed _ ->
        items

    | Unread src ->
        let doneSet =
          src
          |> toFeeds
          |> Set.collect RssFeed.doneSet
        in
          src
          |> filterItems items
          |> Array.filter (fun item -> doneSet |> Set.contains item |> not)

    | Union srcSet ->
        srcSet |> Set.fold filterItems items

  let fetchItemsAsync src =
    async {
      let feeds =
        src |> toFeeds

      let! feedItemsArray =
        feeds
        |> Seq.map (RssFeed.updateAsync)
        |> Async.Parallel

      let (feeds', itemsArray) =
        feedItemsArray |> Array.unzip

      let items =
        itemsArray
        |> Array.collect id
        |> flip filterItems src

      return (feeds', items)
    }
