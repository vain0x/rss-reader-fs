namespace RssReaderFs.Core

open System
open System.Xml.Linq
open System.Linq
open Basis.Core
open Basis.Core.Xml.NamespaceLess

module Article =
  let create title desc link date srcId =
    Article
      ( Title     = title
      , Desc      = desc
      , Link      = link
      , Date      = date
      , SourceId  = srcId
      )

  /// Return the id of the item if it's been already inserted; None otherwise.
  let tryFindId ctx (item: Article) =
    (ctx |> DbCtx.set<Article>).FirstOrDefault
      (fun item' ->
           item'.SourceId   = item.SourceId
        && item'.Date       = item.Date
        )
    |> Option.ofObj
    |> Option.map (fun item -> item.Id)

  /// Insert the item into the table.
  /// Doesn't save, so Id's are invalid until db context is saved.
  /// Returns if it's actually inserted or not.
  let insert ctx (item: Article) =
    if item |> tryFindId ctx |> Option.isNone then
      (ctx |> DbCtx.set<Article>).Add(item) |> ignore
      true
    else
      false

  let readDate ctx itemId =
    (ctx |> DbCtx.set<ReadLog>).Find(itemId) |> Option.ofObj

  let hasAlreadyBeenRead ctx itemId =
    itemId |> readDate ctx |> Option.isSome

  let parseXml srcId (xml: XDocument) =
    let getTextElem xpath =
      XPath.trySelectElement xpath
      >> Option.map XElement.value

    let tryBuildItem (xnode: XNode) =
      let at = flip getTextElem xnode
      let title = at "title"
      let date  =
        at "pubDate"
        |> Option.bind (DateTime.tryParse)
        |> Option.map (fun time -> time.ToLocalTime())
      in
        match (title, date) with
        | (Some title, Some date) ->
            create title (at "description") (at "link") date srcId |> Some
        | _ -> None
    in
      xml
      |> XPath.selectElements "rss/channel/item"
      |> Seq.choose tryBuildItem

  let ofTweet (status: CoreTweet.Status) srcId =
    let (header, body) = status.Text |>  Str.splitAt 50
    let desc  = if body |> Str.isNullOrWhiteSpace then None else Some body
    let title = header + (if desc |> Option.isSome then "..." else "")
    in
      create
        title
        desc
        (status |> Twitter.Status.permanentLink |> Some)
        (status.CreatedAt.DateTime.ToLocalTime())
        srcId
