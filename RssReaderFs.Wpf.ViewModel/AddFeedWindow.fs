namespace RssReaderFs.Wpf.ViewModel

open System
open System.Windows
open RssReaderFs

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
        let bak     = rc.AddSource(RssSource.ofFeed feed)
        in
          // TODO: validation 処理をモデルに移す
          if bak |> Option.isSome
          then this.Error <- "This name has already been taken."
          else
            try
              // Try download
              feed |> RssFeed.downloadAsync |> Async.RunSynchronously |> ignore
              // Pass
              this.Hide() |> ignore
            with
            | :? AggregateException as e ->
                this.Error <- e.InnerExceptions |> Seq.map (fun e -> e.Message) |> String.concat Environment.NewLine
            | e ->
                this.Error <- e.Message
        )

  member this.AddCommand                  = this.AddCommandPair |> fst 
  member this.AddCommandCanExecuteChanged = this.AddCommandPair |> snd

  member this.RssClient
    with get () = this.Data
    and  set value =
      this.Data <- value
      this.RaisePropertyChanged ["RssClient"]
      this.AddCommandCanExecuteChanged()
