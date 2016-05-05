namespace RssReaderFs.Wpf.ViewModel

open RssReaderFs

type SourceTree(rc: RssClient) =
  inherit WpfViewModel.Base()

  member this.Feeds       = rc.Reader |> RssReader.allFeeds |> Array.map (fun feed -> feed.Name)
  member this.Tags        = rc.Reader |> RssReader.allTags
  member this.Sources     = rc.Reader |> RssReader.allAtomicSources |> Seq.map (RssSource.name)
