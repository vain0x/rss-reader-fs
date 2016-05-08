namespace RssReaderFs.Wpf.ViewModel

open System
open Basis.Core
open RssReaderFs.Core

type SourceView(rr: RssReader) as this =
  inherit WpfViewModel.Base()

  let mutable srcName = AllSourceName

  let srcOpt () =
    Source.tryFindByName (rr |> RssReader.ctx) srcName

  let mutable items =
    ([||]: Article [])

  let mutable selectedIndex = -1

  let selectedItem () =
    items |> Array.tryItem selectedIndex

  let selectedLink () =
    selectedItem ()
    |> Option.bind (fun item -> item.Link)
    |> Option.getOr ""

  let (linkJumpCommand, linkJumpCommandExecutabilityChanged) =
    Command.create
      (fun () -> selectedLink () |> String.IsNullOrEmpty |> not)
      (fun () -> selectedLink () |> Diagnostics.Process.Start |> ignore)

  let addNewItems (newItems: Article []) =
    items <-
      newItems
      |> Array.sortBy (fun item -> item.Date)
      |> flip Array.append items
    this.RaisePropertyChanged("Items")
    
  let updateAsync () =
    async {
      match Source.tryFindByName (rr |> RssReader.ctx) srcName with
      | None -> ()
      | Some src ->
          let! newItems = rr |> RssReader.updateAsync src
          if newItems |> Array.isEmpty |> not then
            addNewItems newItems
    }

  let checkUpdate () =
    async {
      while true do
        do! updateAsync ()
        do! Async.Sleep(3 * 60 * 1000)
    }
    |> Async.Start

  do checkUpdate ()

  do rr |> RssReader.changed |> Observable.add (fun () ->
      this.RaisePropertyChanged("Items")
      )

  member this.Items =
    items |> Array.map (MetaArticle.ofItem rr)

  member this.SelectedIndex
    with get () = selectedIndex
    and  set v  =
      selectedIndex <- v

      for name in ["SelectedRow"; "SelectedDesc"] do
        this.RaisePropertyChanged(name)

      linkJumpCommandExecutabilityChanged this

  member this.SelectedItem = selectedItem ()

  member this.SelectedRow: MetaArticle =
    match items |> Array.tryItem selectedIndex with
    | Some item -> item |> MetaArticle.ofItem rr
    | None -> MetaArticle.empty

  member this.SelectedDesc
    with get () =
      items
      |> Array.tryItem selectedIndex
      |> Option.bind (fun item -> item.Desc)
      |> Option.getOr "(No description.)"
    and  set (_: string) = ()

  member this.LinkJumpCommand = linkJumpCommand

  member this.SourceName
    with get () = srcName
    and  set newName =
      srcName <- newName

      items <-
        match Source.tryFindByName (rr |> RssReader.ctx) srcName with
        | Some src -> rr |> RssReader.unreadItems src
        | None -> [||]
      this.RaisePropertyChanged("Items")

      updateAsync () |> Async.Start

      for name in ["SourceName"; "Items"] do
        this.RaisePropertyChanged(name)
