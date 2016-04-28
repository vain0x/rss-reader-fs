namespace RssReaderFs.Wpf.ViewModel

open System
open System.ComponentModel
open System.Windows
open System.Windows.Input
open System.Windows.Threading
open Chessie.ErrorHandling
open RssReaderFs

type AddFeedPanel(rc: RssClient) as this =
  inherit WpfViewModel.Base()

  let mutable error = ""

  member val Name   = "" with get, set
  member val Url    = "" with get, set
  member this.Error
    with get () = error
    and  set v  =
      error <- v
      this.RaisePropertyChanged ["Error"]

  member this.Reset() =
    this.Name     <- ""
    this.Url      <- ""
    this.Error    <- ""

  member val AddFeedCommand =
    Command.create
      (fun _ -> true)
      (fun _ ->
        let feed    =
          { Name = this.Name; Url = Url(this.Url); DoneSet = Set.empty }
        match rc.TryAddSource(RssSource.ofFeed feed) with
        | Ok ((), _) ->
            this.Reset() |> ignore
        | Bad msgs ->
            this.Error <- msgs |> String.concat (Environment.NewLine)
        )
    |> fst

type FeedsWindow(rc: RssClient) as this =
  inherit WpfViewModel.DialogBase<unit>()

  let addFeedPanel = AddFeedPanel(rc)
  
  do rc.Changed |> Observable.add (fun () ->
      this.RaisePropertyChanged ["Feeds"]
      )

  member this.Feeds =
    rc.Reader |> RssReader.allFeeds

  member this.AddFeedPanel = addFeedPanel
