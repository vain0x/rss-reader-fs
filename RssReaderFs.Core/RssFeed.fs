namespace RssReaderFs.Core

open System
open System.Linq
open Chessie.ErrorHandling

module RssFeed =
  let nameUrl (feed: RssFeed) =
    sprintf "%s <%s>"
      feed.Name (feed.Url)

  let downloadAsync url =
    async {
      let! xml = Net.downloadXmlAsync(url)
      return (xml |> Article.parseXml url)
    }

  let validate url =
    Trial.runRaisable (fun () ->
      url |> downloadAsync |> Async.RunSynchronously
      )
    |> Trial.lift (fun _ -> ())
    |> Trial.mapFailure (List.map ExnError)
