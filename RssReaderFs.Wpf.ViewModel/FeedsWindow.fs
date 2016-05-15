namespace RssReaderFs.Wpf.ViewModel

open System
open System.ComponentModel
open System.Windows
open System.Windows.Input
open System.Windows.Threading
open Chessie.ErrorHandling
open RssReaderFs.Core

type AddFeedPanel(rr: RssReader, raiseError: seq<Error> -> unit) as this =
  inherit WpfViewModel.Base()

  let mutable name = ""
  let mutable url  = ""

  member this.Name
    with get () = name
    and  set v  = name <- v; this.RaisePropertyChanged("Name")

  member this.Url
    with get () = url
    and  set v  = url <- v; this.RaisePropertyChanged("Url")

  member this.Reset() =
    this.Name     <- ""
    this.Url      <- ""
    raiseError []

  member val AddFeedCommand =
    Command.create
      (fun _ -> true)
      (fun _ ->
        match rr |> RssReader.addFeed this.Name this.Url with
        | Ok ((), _) ->
            this.Reset() |> ignore
        | Bad msgs ->
            msgs |> raiseError
        )
    |> fst

type FollowPanel(rr: RssReader, raiseError: seq<Error> -> unit) as this =
  inherit WpfViewModel.Base()

  let mutable name = ""

  member this.Name
    with get () = name
    and  set v  = name <- v; this.RaisePropertyChanged("Name")

  member this.Reset() =
    this.Name <- ""
    raiseError []

  member val FollowCommand =
    Command.create
      (fun _ -> true)
      (fun _ ->
          match rr |> RssReader.addTwitterUser this.Name with
          | Ok ((), _) ->
              this.Reset()
          | Bad msgs ->
              raiseError msgs
          )
    |> fst

type FeedsWindow(rr: RssReader) as this =
  inherit WpfViewModel.DialogBase<unit>()

  let mutable error = ""

  let raiseError msgs =
    error <- msgs |> Seq.map Error.toString |> String.concat Environment.NewLine
    this.RaisePropertyChanged("Error")
    this.RaisePropertyChanged("ErrorVisibility")

  let addFeedPanel = AddFeedPanel(rr, raiseError)

  let followPanel = FollowPanel(rr, raiseError)
  
  do rr |> RssReader.changed |> Observable.add (fun () ->
      for name in ["Feeds"; "TwitterUsers"] do
        this.RaisePropertyChanged(name)
      )

  member this.Feeds =
    Source.allFeeds (rr |> RssReader.ctx)

  member this.TwitterUsers =
    Source.allTwitterUsers (rr |> RssReader.ctx) |> Array.map Source.name

  member this.AddFeedPanel = addFeedPanel

  member this.FollowPanel = followPanel 

  member this.Error = error

  member this.ErrorVisibility =
    if String.IsNullOrWhiteSpace(this.Error)
    then Visibility.Collapsed
    else Visibility.Visible
