namespace RssReaderFs.Gui

open System
open System.Drawing

[<AutoOpen>]
module Types =
  type UnitDelegate = delegate of unit -> unit

  type MainFormState =
    {
      Location                : Point
      Size                    : Size
      ListViewColumnWidths    : int []
    }
