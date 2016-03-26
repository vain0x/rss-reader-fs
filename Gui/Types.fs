namespace RssReaderFs.Gui

open System
open System.Drawing

[<AutoOpen>]
module Types =
  type MainFormState =
    {
      Location                : Point
      Size                    : Size
      ListViewColumnWidths    : int []
    }
