namespace RssReaderFs.Core

open System
open System.Xml.Linq
open Basis.Core.Xml.NamespaceLess

/// RSS 2.0 helper
module Rss =
  type Item =
    {
      Title             : string
      Link              : option<Uri>
      Desc              : option<string>
      PubDate           : DateTime
    }

  type Channel =
    {
      Items             : seq<Item>
    }

  let internal itemFromXElem (xe: XElement) =
    let at xpath =
      xe |> XPath.trySelectElement xpath
      |> Option.map XElement.value
    let title = at "title"
    let date  =
      at "pubDate"
      |> Option.bind DateTime.tryParse
      |> Option.map (fun time -> time.ToLocalTime())
    in
      match (title, date) with
      | (Some title, Some date) ->
          {
            Title             = title
            Link              = at "link" |> Option.bind Uri.tryParse
            Desc              = at "desc"
            PubDate           = date
          } |> Some
      | _ -> None

  let ofXml (xml: XDocument) =
    let items =
      xml
      |> XPath.selectElements "rss/channel/item"
      |> Seq.choose itemFromXElem
    in 
      {
        Items           = items
      }
