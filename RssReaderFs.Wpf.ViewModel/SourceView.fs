namespace RssReaderFs.Wpf.ViewModel

open System
open Basis.Core
open RssReaderFs.Core

type SourceViewPage(rr: RssReader, srcOpt: option<DerivedSource>) =
  inherit WpfViewModel.Base()

  let mutable items =
    match srcOpt with
    | None -> [||]
    | Some src ->
        rr |> RssReader.unreadItems src
        |> Array.map (MetaArticle.ofItem rr)

  member this.Items
    with get () = items
    and  set v  =
      items <- v
      this.RaisePropertyChanged("Items")

  member this.AddNewItems(newItems: Article []) =
    this.Items <-
      newItems
      |> Array.sortBy (fun item -> item.Date)
      |> Array.map (MetaArticle.ofItem rr)
      |> flip Array.append items
    
  member this.UpdateAsync() =
    async {
      match srcOpt with
      | None -> ()
      | Some src ->
          let! newItems = rr |> RssReader.updateAsync src
          if newItems |> Array.isEmpty |> not then
            this.AddNewItems(newItems)
    }

type SourceView(rr: RssReader) as this =
  inherit WpfViewModel.Base()

  let defaultPage = SourceViewPage(rr, None)

  let mutable selectedPage =
    defaultPage

  let mutable pages =
    (Map.empty: Map<Id, SourceViewPage>)

  let mutable srcName = AllSourceName

  let mutable selectedIndex = -1

  let selectedItem () =
    selectedPage.Items |> Array.tryItem selectedIndex 

  let selectedArticle () =
    selectedItem () |> Option.map (fun item ->
      (rr |> RssReader.ctx |> DbCtx.set<Article>).Find(item.ArticleId)
      )

  let selectedLink () =
    selectedArticle ()
    |> Option.bind (fun item -> item.Link)
    |> Option.getOr ""

  let (linkJumpCommand, linkJumpCommandExecutabilityChanged) =
    Command.create
      (fun () -> selectedLink () |> String.IsNullOrEmpty |> not)
      (fun () -> selectedLink () |> Diagnostics.Process.Start |> ignore)

  let checkUpdate () =
    async {
      while true do
        do! selectedPage.UpdateAsync()
        do! Async.Sleep(3 * 60 * 1000)
    }
    |> Async.Start

  do checkUpdate ()

  do rr |> RssReader.changed |> Observable.add (fun () ->
      this.RaisePropertyChanged("Items")
      )

  member this.Items =
    selectedPage.Items

  member this.SelectedIndex
    with get () = selectedIndex
    and  set v  =
      selectedIndex <- v

      for name in ["SelectedItem"; "SelectedDesc"] do
        this.RaisePropertyChanged(name)

      linkJumpCommandExecutabilityChanged this

      selectedArticle () |> Option.iter (fun item ->
        let readLog = rr |> RssReader.readItem item
        this.Items.[v].ReadDate <- Some readLog.Date
        )

  member this.SelectedArticle = selectedArticle ()

  member this.SelectedItem: MetaArticle =
    selectedItem () |> Option.getOr MetaArticle.empty

  member this.SelectedDesc
    with get () =
      selectedArticle ()
      |> Option.bind (fun item -> item.Desc)
      |> Option.getOr "(No description.)"
    and  set (_: string) = ()

  member this.LinkJumpCommand = linkJumpCommand

  member this.SelectedPage
    with get () = selectedPage
    and  set v  =
      selectedPage <- v 

      this.SelectedIndex <- -1

      selectedPage.UpdateAsync () |> Async.Start
      this.RaisePropertyChanged("Items")

  member this.SourceName
    with get () = srcName
    and  set newName =
      srcName <- newName

      let page =
        match Source.tryFindByName (rr |> RssReader.ctx) srcName with
        | None -> defaultPage
        | Some src ->
            pages |> Map.tryFind ((src |> fst).Id)
            |> Option.getOrElse (fun () -> SourceViewPage(rr, Some src))
      this.SelectedPage <- page

      this.RaisePropertyChanged("SourceName")
