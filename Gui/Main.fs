namespace RssReaderFs.Gui

open System
open System.Drawing
open System.Windows.Forms
open RssReaderFs

type Main () as this =
  inherit Form
    ( Text        = "RssReaderFs.Gui"
    , Size        = Size(640, 480)
    )

  let path = @"feeds.json"

  let rc = RssClient.Create(path)
  
  let reader () = rc.Reader
  let feeds  () = rc.Feeds

  let listViewHeight  = 150
  let textBoxHeight   = this.ClientSize.Height - 70 - listViewHeight

  let listView =
    new ListView
      ( Location    = Point(5, 5)
      , Size        = Size(this.ClientSize.Width - 10, listViewHeight)
      , View        = View.Details
      , Font        = yuGothic10
      )
    // Add columns
    |> tap (fun listView ->
        let totalWidth =
          (listView.ClientSize.Width - (* スクロールバー分 *) 20) |> float
        let columns =
          [|
            ("Title"  , 0.50)
            ("✓"     , 0.05)
            ("Date"   , 0.17)
            ("Source" , 0.28)
          |]
          |> Array.map (fun (text, ratio) ->
              new ColumnHeader
                ( Text    = text
                , Width   = (totalWidth * ratio |> int)
                )
              )
        do listView.Columns.AddRange(columns)
        )

  let titleLabel =
    new Label
      ( Location    = Point(5, listViewHeight + 5)
      , Size        = Size(this.ClientSize.Width - 10, 25)
      , Font        = new Font("Yu Gothic", float32 14)
      )

  let linkLabel =
    new LinkLabel
      ( Location    =
          Point
            ( 5
            , 5 + listViewHeight + 5 + titleLabel.Size.Height + 5
            )
      , Size        = Size(250, 20)
      , Font        = yuGothic10
      )

  let sourceLabel =
    new Label
      ( Location =
          Point
            ( 5 + linkLabel.Size.Width + 5
            , linkLabel.Location.Y
            )
      , Size =
          Size
            ( this.ClientSize.Width - linkLabel.Size.Width - 10
            , linkLabel.Size.Height
            )
      , Font = yuGothic10
      )

  let textBox =
    new TextBox
      ( Location    = Point(5, this.ClientSize.Height - textBoxHeight - 5)
      , Size        = Size(this.ClientSize.Width - 10, textBoxHeight)
      , Multiline   = true
      , Font        = yuGothic10
      )

  let controls =
    [|
      listView      :> Control
      titleLabel    :> Control
      linkLabel     :> Control
      sourceLabel   :> Control
      textBox       :> Control
    |]

  let listViewItemsFromNewFeeds (items: RssItem []) =
    [|
      for item in items do
        let subItems =
          [|
            item.Title
            ""    // unchecked
            item.Date.ToString("G")
            reader () |> RssReader.sourceName (item.Uri)
          |]
          |> Array.map (fun text ->
              ListViewItem.ListViewSubItem(Text = text)
              )
        yield ListViewItem(subItems, 0)
    |]

  let addNewFeeds items =
    let lvItems = listViewItemsFromNewFeeds items
    do
      for lvItem in lvItems |> Array.rev do
        listView.Items.Insert(0, lvItem) |> ignore

  let unshow () =
    titleLabel.Text     <- "(now loading...)"
    textBox.Text        <- ""
    linkLabel.Text      <- ""
    sourceLabel.Text    <- ""

  let showFeed (item: RssItem) =
    titleLabel.Text     <- item.Title
    textBox.Text        <- item.Desc |> Option.getOr "(no_description)"
    linkLabel.Text      <- item.Link |> Option.getOr "(no_link)"
    sourceLabel.Text    <- reader () |> RssReader.sourceName (item.Uri)

  let readFeed item =
    do rc.ReadItem(item)
    do showFeed item

  let observer =
    { new RssSubscriber with
        member this.OnNewItems(items: RssItem []) =
          addNewFeeds items
          }

  // Add handlers
  do
    listView.ItemSelectionChanged.Add (fun e ->
      let title = e.Item.SubItems.Item(0).Text
      do feeds () |> Map.tryFind title |> Option.iter (readFeed)
      do e.Item.SubItems.Item(1).Text <- "✓"
      )

    this.FormClosed.Add (fun e ->
      rc.Save()
      )

  // Init controls
  do
    unshow ()
    base.Controls.AddRange(controls)

  // Init reader
  do
    rc.Subscribe(observer)
    reader () |> RssReader.updateAllAsync |> Async.RunSynchronously
