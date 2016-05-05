namespace RssReaderFs

open System
open System.Xml
open System.Linq

module RssItem =
  let create title desc link date url =
    RssItem
      ( Title     = title
      , Desc      = (desc |> Option.toObj)
      , Link      = (link |> Option.toObj)
      , Date      = date
      , Url       = url
      )

  /// Return the id of the item if it's been already inserted; None otherwise.
  let tryFindId (ctx: DbCtx) (item: RssItem) =
    ctx.Set<RssItem>().FirstOrDefault
      (fun item' ->
           item'.Url   = item.Url
        && item'.Date  = item.Date
        )
    |> Option.ofObj
    |> Option.map (fun item -> item.Id)

  /// Insert the item into the table.
  /// Doesn't save, so Id's are invalid until db context is saved.
  /// Returns if it's actually inserted or not.
  let insert (ctx: DbCtx) (item: RssItem) =
    if item |> tryFindId ctx |> Option.isNone then
      ctx.Set<RssItem>().Add(item) |> ignore
      true
    else
      false

  let readDate (ctx: DbCtx) itemId =
    ctx.Set<ReadLog>().Find(itemId) |> Option.ofObj

  let hasAlreadyBeenRead ctx itemId =
    itemId |> readDate ctx |> Option.isSome

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
            create title (at "description") (at "link") date url |> Some
        | _ -> None
    in
      xml
      |> Xml.selectNodes "rss/channel/item"
      |> Seq.choose tryBuildItem

  let ofTweet (status: CoreTweet.Status) =
    create
      (status.Text)
      (status.Text |> Some)
      (status |> Twitter.Status.permanentLink |> Some)
      (status.CreatedAt.DateTime)
      ("https://twitter.com/" + status.User.ScreenName)
