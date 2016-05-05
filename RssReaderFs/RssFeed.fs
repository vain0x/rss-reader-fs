namespace RssReaderFs

open System
open System.Linq
open Chessie.ErrorHandling

module RssFeed =
  let create name (url: string) =
    RssFeed(Name = name, Url = url)

  let nameUrl (feed: RssFeed) =
    sprintf "%s <%s>"
      feed.Name (feed.Url)

  let downloadAsync (feed: RssFeed) =
    async {
      let url = feed.Url
      let! xml = Net.downloadXmlAsync(url)
      return (xml |> RssItem.parseXml url)
    }

  let validate feed =
    Trial.runRaisable (fun () ->
      feed |> downloadAsync |> Async.RunSynchronously
      )
    |> Trial.lift (fun _ -> ())
