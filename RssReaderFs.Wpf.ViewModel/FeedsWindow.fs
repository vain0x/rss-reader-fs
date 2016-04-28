namespace RssReaderFs.Wpf.ViewModel

open System
open System.ComponentModel
open System.Windows
open System.Windows.Input
open System.Windows.Threading
open RssReaderFs

type FeedsWindow(rc: RssClient) as this =
  inherit WpfViewModel.DialogBase<unit>()

  let addFeedWindow = AddFeedWindow(rc)
  
  do rc.Changed |> Observable.add (fun () ->
      this.RaisePropertyChanged ["Feeds"]
      )

  let (addFeedCommand, _) =
    Command.create
      (fun () -> true)
      (fun () -> addFeedWindow.Show(()))

  member this.Feeds =
    rc.Reader |> RssReader.allFeeds

  member this.AddFeedCommand = addFeedCommand

  member this.AddFeedWindow = addFeedWindow
