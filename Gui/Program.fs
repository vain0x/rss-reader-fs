namespace RssReaderFs.Gui

open System
open System.Windows.Forms

module Program =
  [<STAThread; EntryPoint>]
  let main argv =
    Application.EnableVisualStyles()
    Application.SetCompatibleTextRenderingDefault(false)
    Application.Run(new MainForm())
    0
