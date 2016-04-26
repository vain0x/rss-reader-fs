namespace RssReaderFs.Wpf.ViewModel

open System
open System.ComponentModel
open System.Windows
open System.Windows.Input
open System.Windows.Threading
open RssReaderFs

type FeedsWindow() =
  inherit WpfViewModel.Base()

  let mutable rcOpt = (None: option<RssClient>)

  member this.RssClient
    with get ()       = rcOpt
    and  set value    =
      rcOpt <- value
      this.RaisePropertyChanged
        ["RssClient"; "Feeds"; "Visibility"]

  member this.Visibility =
    match rcOpt with
    | Some _ -> Visibility.Visible
    | None   -> Visibility.Collapsed

  member this.Feeds =
    match rcOpt with
    | Some rc -> rc.Reader |> RssReader.allFeeds
    | None -> [||]

  /// Returns if actually hidden or not
  member this.Hide() =
    rcOpt
    |> Option.isSome
    |> tap (fun _ -> this.RssClient <- None)
