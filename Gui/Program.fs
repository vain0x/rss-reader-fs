
open System
open System.Windows
open System.Windows.Forms

[<EntryPoint>]
let main argv =
  let onclick (sender: obj) (e: EventArgs) =
    Forms.MessageBox.Show("on click!")
    |> ignore

  let menuItems =
    [ ("click me", onclick)
    ]
    |> List.map (fun (text, h) -> new MenuItem(text, EventHandler(h)))
    |> List.toArray

  use iconMenu =
    new ContextMenu(menuItems)

  use icon =
    new NotifyIcon
      ( Icon         = Drawing.SystemIcons.Application
      , Text         = "RssReaderFs"
      , Visible      = true
      , ContextMenu  = iconMenu
      )

  async {
    while true do
      do! Async.Sleep(100)
  } |> Async.RunSynchronously

  // exit code
  0
