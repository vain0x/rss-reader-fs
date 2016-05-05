namespace RssReaderFs.Wpf.ViewModel

open System
open System.ComponentModel
open System.Windows.Threading
open RssReaderFs

type MainWindow() =
  inherit WpfViewModel.Base()

  let rc = RssClient.Create()

  let sourceTree = SourceTree(rc)

  let sourceView = SourceView(rc)

  let feedsWindow = FeedsWindow(rc)

  let (feedsCommand, _) =
    Command.create
      (fun () -> true)
      (fun () -> feedsWindow.Show(()))

  member this.FeedsWindow = feedsWindow
  
  member this.FeedsCommand = feedsCommand

  member this.SourceTree = sourceTree

  member this.SourceView = sourceView

  member this.Save() =
    rc.Save()
