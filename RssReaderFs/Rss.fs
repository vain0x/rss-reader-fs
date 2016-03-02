module Rss

open System
open System.Xml

let parseRss source (xml: XmlDocument) =
  let getTextElem xpath =
      Xml.selectSingleNode xpath
      >> Option.map (Xml.innerText)

  let buildItem (xnode: XmlNode) =
      let at = flip getTextElem xnode
      {
        Source = source
        Title = at "title" |> Option.getOr "(untitled)"
        Desc  = at "description"
        Link  = at "link"
        Date  =
          at "pubDate"
          |> Option.bind (DateTime.tryParse)
      }
  in
    xml
    |> Xml.selectNodes "rss/channel/item"
    |> Seq.map buildItem

let downloadRssAsync (source: RssSource) =
  async {
    let! xml = Net.downloadXmlAsync(source.Uri)
    return
      {
        Source = source
        Items = (xml |> parseRss source)
      }
  }
