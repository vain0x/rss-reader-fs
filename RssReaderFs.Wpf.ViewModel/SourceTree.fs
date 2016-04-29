namespace RssReaderFs.Wpf.ViewModel

open RssReaderFs

type SourceTree(rc: RssClient) =
  inherit WpfViewModel.Base()

  member this.Feeds       = rc.Reader |> RssReader.allFeeds |> Array.map (fun feed -> feed.Name)
  member this.Tags        = rc.Reader.TagMap    |> Map.keySet
  member this.Sources     = rc.Reader.SourceMap |> Map.keySet
