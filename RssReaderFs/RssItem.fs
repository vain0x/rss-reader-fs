namespace RssReaderFs

open System
open System.Xml

module RssItem =
  let parseXml url (xml: XmlDocument) =
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
              Title   = title
              Desc    = at "description"
              Link    = at "link"
              Date    = date
              Url     = url
            } |> Some
        | _ -> None
    in
      xml
      |> Xml.selectNodes "rss/channel/item"
      |> Seq.choose tryBuildItem

  let ofTweet (status: CoreTweet.Status) =
    {
      Title       = status.Text
      Desc        = status.Text |> Some
      Link        = None
      Date        = status.CreatedAt.DateTime
      Url         = "http://twitter.com/" + status.User.Name
    }
