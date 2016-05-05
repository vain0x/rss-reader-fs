namespace RssReaderFs.Wpf.ViewModel

open System
open RssReaderFs

type SourceView(rc: RssClient) as this =
  inherit WpfViewModel.Base()

  let mutable srcName = AllSourceName

  let srcOpt () =
    rc.Reader |> RssReader.tryFindSource srcName

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
      match rc.Reader |> RssReader.tryFindSource srcName with
      | None -> ()
      | Some src ->
          let! newItems = rc.UpdateAsync(src)
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

  do rc.Changed |> Observable.add (fun () ->
      this.RaisePropertyChanged("Items")
      )

  member this.Items =
    items |> Array.map (ArticleRow.ofItem rc)

  member this.SelectedIndex
    with get () = selectedIndex
    and  set v  =
      selectedIndex <- v

      for name in ["SelectedRow"; "SelectedDesc"] do
        this.RaisePropertyChanged(name)

      linkJumpCommandExecutabilityChanged this

  member this.SelectedItem = selectedItem ()

  member this.SelectedRow: ArticleRow =
    match items |> Array.tryItem selectedIndex with
    | Some item -> item |> ArticleRow.ofItem rc
    | None -> ArticleRow.empty

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
      items <- [||]
      updateAsync () |> Async.Start

      for name in ["SourceName"; "Items"] do
        this.RaisePropertyChanged(name)
