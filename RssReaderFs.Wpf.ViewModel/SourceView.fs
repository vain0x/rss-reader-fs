namespace RssReaderFs.Wpf.ViewModel

open System
open RssReaderFs

type SourceView(rc: RssClient) as this =
  inherit WpfViewModel.Base()

  let mutable items =
    ([||]: RssItem [])

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

  let addNewItems newItems =
    items <-
      newItems
      |> Array.sortBy (fun item -> item.Date)
      |> flip Array.append items
    this.RaisePropertyChanged ["Items"]
    
  let checkUpdate () =
    async {
      while true do
        let! newItems = rc.UpdateAllAsync
        if newItems |> Array.isEmpty |> not then
          addNewItems newItems
        do! Async.Sleep(3 * 60 * 1000)
    }
    |> Async.Start

  do checkUpdate ()

  do rc.Changed |> Observable.add (fun () ->
      this.RaisePropertyChanged ["Items"]
      )

  member this.Items =
    items |> Array.map (RssItemRow.ofItem rc)

  member this.SelectedIndex
    with get () = selectedIndex
    and  set v  =
      selectedIndex <- v

      this.RaisePropertyChanged
        ["SelectedRow"; "SelectedDesc"]

      linkJumpCommandExecutabilityChanged this

  member this.SelectedItem = selectedItem ()

  member this.SelectedRow: RssItemRow =
    match items |> Array.tryItem selectedIndex with
    | Some item -> item |> RssItemRow.ofItem rc
    | None -> RssItemRow.empty

  member this.SelectedDesc
    with get () =
      items
      |> Array.tryItem selectedIndex
      |> Option.bind (fun item -> item.Desc)
      |> Option.getOr "(No description.)"
    and  set (_: string) = ()

  member this.LinkJumpCommand = linkJumpCommand
