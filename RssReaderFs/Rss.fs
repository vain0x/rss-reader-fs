namespace RssReaderFs

open System
open System.Xml

module Rss =
  let parseRss url (xml: XmlDocument) =
    let getTextElem xpath =
        Xml.selectSingleNode xpath
        >> Option.map (Xml.innerText)

    let tryBuildItem (xnode: XmlNode) =
        let at = flip getTextElem xnode
        let title = at "title"
        let date  =
            at "pubDate"
            |> Option.bind (DateTime.tryParse)
            |> Option.map (fun time -> time.ToLocalTime())
        in
          match (title, date) with
          | (Some title, Some date) ->
              {
                Title = title
                Desc  = at "description"
                Link  = at "link"
                Date  = date
                Url   = url
              } |> Some
          | _ -> None
    in
      xml
      |> Xml.selectNodes "rss/channel/item"
      |> Seq.choose tryBuildItem

  let downloadRssAsync (source: RssSource) =
    async {
      let url = source.Url
      let! xml = Net.downloadXmlAsync(url)
      return (xml |> parseRss url)
    }

  let updateRssAsync src =
    async {
      return! src |> downloadRssAsync
    }

  let sourceFromUrl name (url: string) =
    {
      Name        = name
      Url         = Url.ofString (url)
    }
    
  module Serialize =
    open System.IO

    let load path =
      try
        let json =
          File.ReadAllText(path)
        let sources =
          Serialize.deserializeJson<RssSource []>(json)
        in
          sources |> Some
      with
      | _ -> None

    let save path (sources) =
      let json =
        Serialize.serializeJson(sources)
      in
        File.WriteAllText(path, json)
