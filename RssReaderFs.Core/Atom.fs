namespace RssReaderFs.Core

open System
open System.Xml.Linq
open Chessie.ErrorHandling
open Basis.Core
open Basis.Core.Xml.NamespaceLess

module Atom =
  type Media =
    {
      ContentType       : string
      Length            : uint64
      Href              : Uri
    }

  type Content =
    {
      Type              : string
      Body              : string
    }

  type Entry =
    {
      Id                : string
      Title             : string
      Summary           : option<string>
      Link              : Uri
      Published         : DateTime
      Updated           : option<DateTime>
      Content           : option<Content>
      Enclosure         : list<Media>
    }

  type Feed =
    {
      Updated           : DateTime
      Entries           : seq<Entry>
    }

  let namespace' =
    @"http://www.w3.org/2005/Atom"

  let private getAtomElems name (xe: XElement) =
    xe.Elements(XName.Get(name, namespace'))

  let private tryGetAtomElem name xe =
    xe |> getAtomElems name |> Seq.tryHead

  let private tryEnclosure (xe: XElement) =
    option {
      let! rel =
        xe |> XElement.tryGetAttr "rel" 
        |> Option.filter (fun xa -> (xa |> XAttribute.value) = "enclosure")
      let! contentType =
        xe |> XElement.tryGetAttr "type" |> Option.map XAttribute.value
      let! length =
        xe |> XElement.tryGetAttr "length"
        |> Option.bind (XAttribute.value >> UInt64.TryParse >> Option.ofTrial)
      let! href =
        xe |> XElement.tryGetAttr "href"
        |> Option.bind (XAttribute.value >> Uri.tryParse)
      return
        {
          ContentType           = contentType
          Length                = length
          Href                  = href
        }
    }

  let private tryEntryOfXNode (xe: XElement) =
    trial {
      let! title =
        xe |> tryGetAtomElem "title"
        |> Option.map XElement.value
        |> Trial.failIfNone "Missing title of an entry."
      let! entryId =
        xe |> tryGetAtomElem "id"
        |> Option.map XElement.value
        |> Trial.failIfNone ("Missing id of the entry '" + title + "'.")
      let summary =
        xe |> tryGetAtomElem "summary"
        |> Option.map XElement.value
      let! link =
        xe |> tryGetAtomElem "link"
        |> Option.bind (fun xe ->
            [ xe |> XElement.value |> Some
              xe |> XElement.tryGetAttr "href" |> Option.map XAttribute.value
            ]
            |> List.tryPick (Option.bind Uri.tryParse)
            )
        |> Trial.failIfNone ("Missing link of the entry '" + title + "'.")
      let! published =
        xe |> tryGetAtomElem "published"
        |> Option.bind (XElement.value >> DateTime.tryParse)
        |> Trial.failIfNone ("Missing published of the entry '" + title + "'.")
      let updated =
        xe |> tryGetAtomElem "updated"
        |> Option.bind (XElement.value >> DateTime.tryParse)
      let content =
        xe |> tryGetAtomElem "content"
        |> Option.map (fun xe ->
          let contentType =
            xe |> XElement.tryGetAttr "type"
            |> Option.either XAttribute.value (fun () -> "text")
          in
            {
              Type                  = contentType
              Body                  = xe |> XElement.value
            })
      let enclosure =
        xe |> getAtomElems "link"
        |> Seq.choose tryEnclosure
        |> Seq.toList
      return
        {
          Title             = title
          Id                = entryId
          Link              = link
          Summary           = summary
          Published         = published
          Updated           = updated
          Content           = content
          Enclosure         = enclosure
        }
    }

  let ofXml (xml: XDocument) =
    trial {
      let feed = xml |> XDocument.root
      let! updated =
        feed |> tryGetAtomElem "updated"
        |> Option.bind (XElement.value >> DateTime.tryParse)
        |> Trial.failIfNone "Updated tag is missing or invalid."
      let! entries =
        feed |> getAtomElems "entry"
        |> Seq.map (fun xe ->
          let r = xe |> tryEntryOfXNode
          in (r |> Trial.toOption, r |> Trial.toMessages)
          )
        |> Seq.unzip
        |> (fun (entryOpts, msgListSeq) ->
            let es = entryOpts |> Seq.choose id
            in Result.Ok (es, msgListSeq |> Seq.collect id |> Seq.toList)
            )
      return
        {
          Updated           = updated
          Entries           = entries
        }
    }
