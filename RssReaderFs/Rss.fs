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
    return
      {
        LastUpdated = DateTime.UtcNow
        Items = (xml |> parseRss uri)
        Uri = uri
      }
  }
