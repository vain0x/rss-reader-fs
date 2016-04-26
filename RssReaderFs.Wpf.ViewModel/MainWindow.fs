namespace RssReaderFs.Wpf.ViewModel

open System
open System.ComponentModel
open System.Windows.Threading
open RssReaderFs

type MainWindow() as this =
  let path = @"feeds.json"
  let rc = RssClient.Create(path)

  do rc.AddSource(RssSource.ofFeed {Name = "Yahoo!ニュース"; Url=Url.ofString "http://dailynews.yahoo.co.jp/fc/rss.xml"; DoneSet=set[]}) |> ignore
  do rc.AddSource(RssSource.ofFeed {Name = "NHKニュース"; Url=Url.ofString "http://www3.nhk.or.jp/rss/news/cat0.xml"; DoneSet=Set.empty}) |> ignore

  let mutable items =
    ([||]: RssItem [])

  let mutable selectedIndex = -1

  let selectedItem () =
    items |> Array.tryItem selectedIndex

  let (propertyChanged, raisePropertyChanged) =
    NotifyPropertyChanged.create this

  let selectedLink () =
    selectedItem ()
    |> Option.bind (fun item -> item.Link)
    |> Option.getOr ""

  let (linkJumpCommand, linkJumpCommandExecutabilityChanged) =
    Command.create
      (fun () -> selectedLink () |> String.IsNullOrEmpty |> not)
      (fun () -> selectedLink () |> Diagnostics.Process.Start |> ignore)

  let feedsWindow = FeedsWindow()

  let (feedsCommand, _) =
    Command.create
      (fun () -> true)
      (fun () -> feedsWindow.RssClient <- Some rc)

  let addNewItems newItems =
    items <-
      newItems
      |> Array.sortBy (fun item -> item.Date)
      |> flip Array.append items
    raisePropertyChanged "Items"
    
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

  member this.Items =
    items |> Array.map (RssItemRow.ofItem rc)

  member this.SelectedIndex
    with get () = selectedIndex
    and  set v  =
      selectedIndex <- v

      ["SelectedRow"; "SelectedDesc"]
      |> List.iter raisePropertyChanged

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

  member this.FeedsWindow = feedsWindow
  
  member this.FeedsCommand = feedsCommand

  interface INotifyPropertyChanged with
    [<CLIEvent>]
    member val PropertyChanged = propertyChanged
