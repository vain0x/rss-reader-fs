namespace RssReaderFs.Core.Test

open System
open System.IO
open RssReaderFs.Core
open Chessie.ErrorHandling

[<AutoOpen>]
module Environment =
  let defaultFeeds =
    [
      ("NHKニュース"          , "http://www3.nhk.or.jp/rss/news/cat0.xml")
      ("47ニュース/北海道"    , "http://www.47news.jp/localnews/hokkaido/index.rss")
      ("47ニュース/東京都"    , "http://www.47news.jp/localnews/tokyo/index.rss")
      ("47ニュース/沖縄県"    , "http://www.47news.jp/localnews/okinawa/index.rss")
      ("47ニュース/地域経済"  , "http://www.47news.jp/news/localkeizai.rss")
    ]
  let defaultTwitterUsers =
    [
      "ue_dai"
      "twitterjp"
      "nhk"
      "nhk_news"
    ]

  let rr =
    RssReader.create () |> tap (fun rr ->
      // Seed
      if rr |> RssReader.ctx |> DbCtx.set<RssFeed> |> Seq.isEmpty then
        for (name, url) in defaultFeeds do
          (name, rr |> RssReader.addFeed name url) |> ignore
        for name in defaultTwitterUsers do
          (name, rr |> RssReader.addTwitterUser name) |> ignore
      )

  let ctx =
    rr |> RssReader.ctx
