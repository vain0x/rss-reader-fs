namespace RssReaderFs.Gui

open System
open System.Drawing
open System.Windows.Forms

[<AutoOpen>]
module Misc =
  let yuGothic10 =
    new Font("Yu Gothic", float32 10)

module Form =
  let singletonSubform (build: unit -> 'Form) =
    let lock =
      lock (new obj())
    let mutable curForm =
      (None: option<'Form>)
    let reset () =
      lock (fun () ->
        let form = build ()
        do (form :> Form).Show()
        do curForm <- Some form
        )
    let show () =
        match curForm with
        | Some form ->
            if form.IsDisposed
            then reset ()
            else (form :> Form).Select()
        | None ->
            reset ()
    in
      show
