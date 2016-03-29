namespace RssReaderFs.Gui

open System
open System.Drawing
open System.Windows.Forms
open RssReaderFs

type SourceAddForm (onRegister: RssSource -> unit) as this =
  inherit Form
    ( Text    = "Add Source - RssReaderFs.Gui"
    , Size    = Size(360, 240)
    )

  let nameLabel =
    new Label
      ( Location    = Point(5, 5)
      , Size        = Size(60, 25)
      , Font        = yuGothic10
      , Text        = "Name:"
      )

  let urlLabel =
    new Label
      ( Location    = Point(5, 5 + nameLabel.Size.Height + 5)
      , Size        = nameLabel.Size
      , Font        = yuGothic10
      , Text        = "URL:"
      )

  let nameBox =
    new TextBox
      ( Location    = Point(5 + nameLabel.Size.Width + 5, 5)
      , Size        = Size(260, 20)
      , Font        = yuGothic10
      )

  let urlBox =
    new TextBox
      ( Location    = Point(nameBox.Location.X, urlLabel.Location.Y)
      , Size        = nameBox.Size
      , Font        = yuGothic10
      )

  let okButton =
    new Button
      ( Location    =
          Point
            ( this.ClientSize.Width - 70
            , this.ClientSize.Height - 35
            )
      , Size        = Size(60, 25)
      , Font        = yuGothic10
      , Text        = "OK"
      )

  let controls =
    [|
      nameLabel     :> Control
      urlLabel      :> Control
      nameBox       :> Control
      urlBox        :> Control
      okButton      :> Control
    |]

  do
    okButton.Click.Add (fun e ->
      let item =
        {
          Name        = nameBox.Text
          Url         = Url.ofString (urlBox.Text)
          LastUpdate  = DateTime.Now
        }
      do
        onRegister item
        this.Close()
      )

    base.Controls.AddRange(controls)
