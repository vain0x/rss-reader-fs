namespace RssReaderFs

open System
open Chessie.ErrorHandling

module RssFeed =
  let doneSet (feed: RssFeed) =
    feed.DoneSet

  let create name (url: string) =
    {
      Name        = name
      Url         = url
      DoneSet     = Set.empty
    }

  let nameUrl (feed: RssFeed) =
    sprintf "%s <%s>"
      feed.Name (feed.Url)

  let rename oldName newName (feed: RssFeed) =
    let name' = feed.Name |> replace oldName newName
    in { feed with Name = name' }

  let downloadAsync (feed: RssFeed) =
    async {
      let url = feed.Url
      let! xml = Net.downloadXmlAsync(url)
      return (xml |> RssItem.parseXml url)
    }

  let updateAsync feed =
    async {
      let! items = feed |> downloadAsync

      // 読了済みのものと分離する
      let (dones, undones) =
        items
        |> Seq.toArray
        |> Array.partition (fun item -> feed |> doneSet |> Set.contains item)

      let feed =
        { feed with
            DoneSet = dones |> Set.ofArray
        }

      return (feed, undones)
    }

  let validate feed =
    Trial.runRaisable (fun () ->
      feed |> downloadAsync |> Async.RunSynchronously
      )
    |> Trial.lift (fun _ -> ())
