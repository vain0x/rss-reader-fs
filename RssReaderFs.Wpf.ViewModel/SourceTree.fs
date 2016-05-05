namespace RssReaderFs.Wpf.ViewModel

open RssReaderFs.Core

type SourceTree(rc: RssReader) as this =
  inherit WpfViewModel.Base()

  do rc |> RssReader.changed |> Observable.add (fun () ->
      for name in ["Feeds"; "Tags"; "Sources"] do
        this.RaisePropertyChanged(name)
      )

  member this.Feeds       = rc |> RssReader.allFeeds |> Array.map (fun feed -> feed.Name)
  member this.Tags        = rc |> RssReader.allTags
  member this.Sources     = rc |> RssReader.allAtomicSources |> Seq.map (Source.name)
