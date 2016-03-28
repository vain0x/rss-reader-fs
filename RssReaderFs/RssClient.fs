namespace RssReaderFs

open System

type RssClient private (path: string) =
  let mutable reader =
    match path |> Rss.Serialize.load with
    | Some sources -> RssReader.create(sources)
    | None -> failwithf "Invalid sources: %s" path

  let mutable feeds =
    (Map.empty: Map<string, RssItem>)

  let proj (item: RssItem) =
    item.Title

  let procNewFeeds (items: RssItem []) =
    items
    |> Array.filter (fun item ->  // 取得済みのフィードを取り除く
        feeds |> Map.containsKey (proj item) |> not
        )
    |> tap (fun items ->  // 新フィードを保存する
        feeds <-
          items
          |> Array.fold (fun feeds item ->
              feeds |> Map.add (item.Title) item
              ) feeds
        )

  member this.Reader = reader

  member this.Feeds = feeds

  member this.Add(src) =
    reader <- reader |> RssReader.add src

  member this.Remove(uri) =
    reader <- reader |> RssReader.remove uri

  member this.Subscribe(obs: RssSubscriber) =
    let myObs =
      { new RssSubscriber with
          member this.OnNewItems(items) =
            let body () =
              let items = procNewFeeds items
              do obs.OnNewItems(items)
            in
              lock this body
      }
    reader <- reader |> RssReader.subscribe myObs

  member this.Unsubscribe(obsId) =
    reader <- reader |> RssReader.unsubscribe obsId

  member this.ReadItem(item) =
    reader <- reader |> RssReader.readItem item

  static member Create(path) =
    new RssClient(path)

  member this.Save() =
    reader |> RssReader.sources |> Rss.Serialize.save path
