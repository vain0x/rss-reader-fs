namespace RssReaderFs.Wpf.ViewModel

open System
open System.ComponentModel
open System.Windows
open System.Windows.Input
open System.Windows.Threading
open Chessie.ErrorHandling
open RssReaderFs

type AddFeedPanel(rc: RssClient, raiseError: seq<string> -> unit) as this =
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
        let feed    =
          RssFeed(Name = this.Name, Url = this.Url)
        match rc.TryAddSource(RssSource.ofFeed feed) with
        | Ok ((), _) ->
            this.Reset() |> ignore
        | Bad msgs ->
            msgs |> raiseError
        )
    |> fst

type FollowPanel(rc: RssClient, raiseError: seq<string> -> unit) as this =
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
          let twitterUser = Entity.TwitterUser(ScreenName = this.Name)
          match rc.TryAddSource(RssSource.ofTwitterUser twitterUser) with
          | Ok ((), _) ->
              this.Reset()
          | Bad msgs ->
              raiseError msgs
          )
    |> fst

type FeedsWindow(rc: RssClient) as this =
  inherit WpfViewModel.DialogBase<unit>()

  let mutable error = ""

  let raiseError msgs =
    error <- msgs |> String.concat Environment.NewLine
    this.RaisePropertyChanged("Error")
    this.RaisePropertyChanged("ErrorVisibility")

  let addFeedPanel = AddFeedPanel(rc, raiseError)

  let followPanel = FollowPanel(rc, raiseError)
  
  do rc.Changed |> Observable.add (fun () ->
      this.RaisePropertyChanged("Feeds")
      )

  member this.Feeds =
    rc.Reader |> RssReader.allFeeds

  member this.TwitterUsers =
    rc.Reader |> RssReader.twitterUsers |> Array.map (fun tu -> tu.ScreenName)

  member this.AddFeedPanel = addFeedPanel

  member this.FollowPanel = followPanel 

  member this.Error = error

  member this.ErrorVisibility =
    if String.IsNullOrWhiteSpace(this.Error)
    then Visibility.Collapsed
    else Visibility.Visible
