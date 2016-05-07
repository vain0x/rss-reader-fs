namespace RssReaderFs.Wpf.ViewModel

open RssReaderFs.Core

type SourceTree(rc: RssReader) as this =
  inherit WpfViewModel.Base()

  do rc |> RssReader.changed |> Observable.add (fun () ->
      for name in ["Feeds"; "Tags"; "Sources"] do
        this.RaisePropertyChanged(name)
      )

  member this.Feeds       = Source.allFeeds (rc |> RssReader.ctx) |> Array.map (Source.name)
  member this.Tags        = Source.allTags (rc |> RssReader.ctx) |> Seq.map (Source.name)
  member this.Sources     = Source.allAtomicSources (rc |> RssReader.ctx) |> Seq.map (Source.name)
