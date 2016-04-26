namespace RssReaderFs.Wpf.ViewModel

open System.Windows
open RssReaderFs

type AddFeedWindow() =
  inherit WpfViewModel.DialogBase<RssClient>()

  member this.RssClient
    with get () = this.Data
    and  set value =
      this.Data <- value
      this.RaisePropertyChanged ["RssClient"]

  member this.Name = ""
  member this.Url = ""
