namespace RssReaderFs.Core

open System
open System.Linq
open Chessie.ErrorHandling

module RssFeed =
  let downloadAsync srcId url =
    async {
      let! xml = Net.downloadXmlAsync(url)
      if (url |> Uri.tryParse |> Option.map Uri.extension) = Some ".atom" then
        return
          Atom.ofXml xml
          |> Trial.either (fun (atom, _) -> atom.Entries) (fun _ -> Seq.empty)
          |> Seq.map (Article.ofAtomEntry srcId)
      else
        let channel = xml |> Rss.ofXml
        return channel.Items |> Seq.map (Article.ofRssItem srcId)
    }

  let validate url =
    Trial.runRaisable (fun () ->
      // source id はダミー値で問題ない
      url |> downloadAsync 0L |> Async.RunSynchronously
      )
    |> Trial.lift (fun _ -> ())
    |> Trial.mapFailure (List.map ExnError)
