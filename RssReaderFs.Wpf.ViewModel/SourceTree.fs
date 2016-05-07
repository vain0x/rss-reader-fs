namespace RssReaderFs.Wpf.ViewModel

open RssReaderFs.Core

type SourceTree(rr: RssReader) as this =
  inherit WpfViewModel.Base()

  do rr |> RssReader.changed |> Observable.add (fun () ->
      for name in ["Feeds"; "Tags"; "Sources"] do
        this.RaisePropertyChanged(name)
      )

  member this.Feeds       = Source.allFeeds (rr |> RssReader.ctx) |> Array.map (Source.name)
  member this.Tags        = Source.allTags (rr |> RssReader.ctx) |> Seq.map (Source.name)
  member this.Sources     = Source.allAtomicSources (rr |> RssReader.ctx) |> Seq.map (Source.name)
