module Rss

open System
open System.Xml

let parseRss uri (xml: XmlDocument) =
  let getTextElem xpath =
      Xml.selectSingleNode xpath
      >> Option.map (Xml.innerText)

  let buildItem (xnode: XmlNode) =
      let at = flip getTextElem xnode
      {
        Title = at "title" |> Option.getOr "(untitled)"
        Desc  = at "description"
        Link  = at "link"
        Date  =
          at "pubDate"
          |> Option.bind (DateTime.tryParse)
          |> Option.map (fun time -> time.ToLocalTime())
        Uri = uri
      }
  in
    xml
    |> Xml.selectNodes "rss/channel/item"
    |> Seq.map buildItem

let downloadRssAsync (source: RssSource) =
  async {
    let uri = source.Uri
    let! xml = Net.downloadXmlAsync(uri)
    return (xml |> parseRss uri)
  }

let downloadFeedAsync (source: RssSource) =
  async {
    let! items = source |> downloadRssAsync
    return
      {
        LastUpdate  = DateTime.Now
        Items       = items
        OldItems    = []
        Source      = source
      }
  }

let updateFeedAsync (feed: RssFeed) =
  async {
    let! newItems = feed.Source |> downloadRssAsync

    // 前回の取得時刻より新しいアイテムのみ
    let newItems =
      newItems
      |> Seq.filter (fun item ->
          match item.Date with
          | None -> false  // TODO: 時刻未定義な項目はこの仕組みでは扱えない
          | Some date -> date >= feed.LastUpdate
          )

    return
      { feed
        with
          LastUpdate  = DateTime.Now
          OldItems    = feed.Items :: feed.OldItems
          Items       = newItems
      }
  }
