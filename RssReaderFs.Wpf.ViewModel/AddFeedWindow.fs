namespace RssReaderFs.Wpf.ViewModel

open System
open System.Windows
open RssReaderFs
open Chessie.ErrorHandling

type AddFeedWindow(rc: RssClient) as this =
  inherit WpfViewModel.DialogBase<unit>()

  let mutable error = (null: string)

  member val Name   = "" with get, set
  member val Url    = "" with get, set
  member this.Error
    with get () = error
    and  set v  =
      error <- v
      this.RaisePropertyChanged ["Error"]

  member val AddCommand =
    Command.create
      (fun _ -> true)
      (fun _ ->
        let feed    =
          { Name = this.Name; Url = Url(this.Url); DoneSet = Set.empty }
        match rc.TryAddSource(RssSource.ofFeed feed) with
        | Ok ((), _) ->
            this.Hide() |> ignore
        | Bad msgs ->
            this.Error <- msgs |> String.concat (Environment.NewLine)
        )
    |> fst
