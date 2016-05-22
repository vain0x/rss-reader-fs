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

  let ofRssItem srcId (item: Rss.Item) =
    let link =
      item.Link |> Option.map string
    in
      create item.Title item.Desc link item.PubDate srcId

  let ofAtomEntry srcId (entry: Atom.Entry) =
    let desc =
      Option.appendWith (fun x y -> x + Environment.NewLine + y)
        entry.Summary (entry.Content |> Option.map (fun c -> c.Body))
    in
      create entry.Title desc (entry.Link |> string |> Some) entry.Published srcId

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
