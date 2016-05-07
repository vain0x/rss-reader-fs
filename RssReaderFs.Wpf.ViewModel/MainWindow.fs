namespace RssReaderFs.Wpf.ViewModel

open System
open System.ComponentModel
open System.Windows.Threading
open RssReaderFs.Core

type MainWindow() =
  inherit WpfViewModel.Base()

  let rr = RssReader.create()

  let sourceTree = SourceTree(rr)

  let sourceView = SourceView(rr)

  let feedsWindow = FeedsWindow(rr)

  let (feedsCommand, _) =
    Command.create
      (fun () -> true)
      (fun () -> feedsWindow.Show(()))

  member this.FeedsWindow = feedsWindow
  
  member this.FeedsCommand = feedsCommand

  member this.SourceTree = sourceTree

  member this.SourceView = sourceView

  member this.Save() =
    rr |> RssReader.save
