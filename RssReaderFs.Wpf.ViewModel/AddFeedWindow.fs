namespace RssReaderFs.Wpf.ViewModel

open System
open System.Windows
open RssReaderFs
open Chessie.ErrorHandling

type AddFeedWindow() as this =
  inherit WpfViewModel.DialogBase<RssClient>()

  let mutable error = (null: string)

  member val Name   = "" with get, set
  member val Url    = "" with get, set
  member this.Error
    with get () = error
    and  set v  =
      error <- v
      this.RaisePropertyChanged ["Error"]

  member val AddCommandPair =
    Command.create
      (fun _ -> this.Data |> Option.isSome)
      (fun _ ->
        let rc      = this.Data |> Option.get
        let feed    =
          { Name = this.Name; Url = Url(this.Url); DoneSet = Set.empty }
        match rc.TryAddSource(RssSource.ofFeed feed) with
        | Ok ((), _) ->
            this.Hide() |> ignore
        | Bad msgs ->
            this.Error <- msgs |> String.concat (Environment.NewLine)
        )

  member this.AddCommand                  = this.AddCommandPair |> fst 
  member this.AddCommandCanExecuteChanged = this.AddCommandPair |> snd

  member this.RssClient
    with get () = this.Data
    and  set value =
      this.Data <- value
      this.RaisePropertyChanged ["RssClient"]
      this.AddCommandCanExecuteChanged()
