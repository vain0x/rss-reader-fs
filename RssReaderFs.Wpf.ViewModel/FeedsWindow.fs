namespace RssReaderFs.Wpf.ViewModel

open System
open System.ComponentModel
open System.Windows
open System.Windows.Input
open System.Windows.Threading
open RssReaderFs

type FeedsWindow() =
  inherit WpfViewModel.DialogBase<RssClient>()

  member this.RssClient
    with get ()       = this.Data
    and  set value    =
      this.Data <- value
      this.RaisePropertyChanged
        ["RssClient"; "Feeds"]

  member this.Feeds =
    match this.RssClient with
    | Some rc -> rc.Reader |> RssReader.allFeeds
    | None -> [||]
