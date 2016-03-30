namespace RssReaderFs.Gui

open System
open System.Drawing
open System.IO
open System.Windows.Forms
open RssReaderFs

type MainForm () as this =
  inherit Form
    ( Text        = "RssReaderFs.Gui"
    , MinimumSize = Size(320, 240)
    )

  let pathState = @"RssReaderFsGuiMainFormState.json"
  let path = @"feeds.json"

  let rc = RssClient.Create(path)
  
  let reader () = rc.Reader

  let listView =
    new ListView
      ( Location    = Point(5, 5)
      , View        = View.Details
      , Font        = yuGothic10
      )
    // Add columns
    |> tap (fun listView ->
        let columns =
          {
            Title     = "Title"
            Read      = "✓"
            Date      = "Date"
            Source    = "Source"
          }
          |> MainListviewColumns.toArray
          |> Array.map (fun text ->
              new ColumnHeader(Text = text)
              )
        do listView.Columns.AddRange(columns)
        )

  let subitems (item: ListViewItem) =
    let sis = item.SubItems
    let () =
      assert (sis.Count >= 4)
    in
      [ for i in 0..(sis.Count - 1) -> sis.[i] ]
      |> MainListviewColumns.ofSeq
      |> Option.get  // use assumption

  let titleLabel =
    new Label
      ( Font        = new Font("Yu Gothic", float32 14)
      )

  let linkLabel =
    new LinkLabel
      ( Size        = Size(300, 20)
      , Font        = yuGothic10
      )

  let sourceLabel =
    new Label
      ( Size = Size(200, 20)
      , Font = yuGothic10
      )

  let textBox =
    new TextBox
      ( Multiline   = true
      , Font        = yuGothic10
      )

  let sourceListButton =
    new Button
      ( Size        = Size(80, 25)
      , Font        = yuGothic10
      , Text        = "Sources"
      )

  let controls =
    [|
      listView      :> Control
      titleLabel    :> Control
      linkLabel     :> Control
      sourceLabel   :> Control
      textBox       :> Control
      sourceListButton      :> Control
    |]

  let resize () =
    let textBoxHeight   = 100
    let listViewHeight  = this.ClientSize.Height - textBoxHeight - 95
    do
      listView.Size <-
        Size(this.ClientSize.Width - 10, listViewHeight)

      textBox.Location <-
        Point(5, this.ClientSize.Height - textBoxHeight - 35)
      textBox.Size <-
        Size(this.ClientSize.Width - 10, textBoxHeight)
      
      titleLabel.Location <-
        Point(5, listViewHeight + 5)
      titleLabel.Size <-
        Size(this.ClientSize.Width - 10, 25)

      linkLabel.Location <-
          Point(5, 5 + listViewHeight + 5 + titleLabel.Size.Height + 5)
      
      sourceLabel.Location <-
        Point
          ( 5 + linkLabel.Size.Width + 5
          , linkLabel.Location.Y
          )

      sourceListButton.Location <-  
        Point(5, this.ClientSize.Height - 30)

  let showSourceListForm =
    Form.singletonSubform
      (fun () -> new SourceListForm(rc))

  let listViewItemsFromNewItems (items: RssItem []) =
    [|
      for item in items do
        let subItems =
          {
            Title     = item.Title
            Read      = ""
            Date      = item.Date.ToString("G")
            Source    = reader () |> RssReader.feedName (item.Url)
          }
          |> MainListviewColumns.toArray
          |> Array.map (fun text ->
              ListViewItem.ListViewSubItem(Text = text)
              )
        yield ListViewItem(subItems, 0)
    |]

  let addNewItems items =
    let items =
      items |> Array.sortBy (fun item -> item.Date)
    let lvItems = listViewItemsFromNewItems items
    let body () = listView.Items.AddRange(lvItems)
    do
      if listView.InvokeRequired
      then listView.BeginInvoke(UnitDelegate(body)) |> ignore
      else body ()

  let unshow () =
    titleLabel.Text     <- "(now loading...)"
    textBox.Text        <- ""
    linkLabel.Text      <- ""
    sourceLabel.Text    <- ""

  let showItem (item: RssItem) =
    titleLabel.Text     <- item.Title
    textBox.Text        <- item.Desc |> Option.getOr "(no_description)"
    linkLabel.Text      <- item.Link |> Option.getOr "(no_link)"
    sourceLabel.Text    <- reader () |> RssReader.feedName (item.Url)

  let readItem item =
    do rc.ReadItem(item)
    do showItem item

  let checkUpdate () =
    async {
      let! newItems = rc.UpdateAllAsync
      do
        if newItems |> Array.isEmpty |> not then
          addNewItems newItems
    }
    |> Async.RunSynchronously

  let updateTimer =
    new Timer(Interval = 3 * 60 * 1000)
    |> tap (fun timer ->
        timer.Tick.Add (fun e -> checkUpdate ())
        )

  let loadState () =
    try
      let json =
        File.ReadAllText(pathState)
      let state =
        Serialize.deserializeJson<MainFormState> (json)
      do
        this.Location <- state.Location
        this.Size     <- state.Size
        
        state.ListViewColumnWidths
        |> Array.iteri (fun i w ->
            listView.Columns.Item(i).Width <- w
            )
    with
    | e -> ()

  let saveState () =
    let state =
      {
        Location    = this.Location
        Size        = this.Size
        ListViewColumnWidths =
          [|
            for i in 0..(listView.Columns.Count - 1) do
              let col = listView.Columns.Item(i)
              yield col.Width
          |]
      }
    let json =
      Serialize.serializeJson<MainFormState> (state)
    do
      File.WriteAllText(pathState, json)

  // Add handlers
  do
    listView.ItemSelectionChanged.Add (fun e ->
      let columns = e.Item |> subitems
      let title = columns.Title.Text
      do
        reader ()
        |> RssReader.unreadItems
        |> Seq.tryFind (fun item -> item.Title = title)
        |> Option.iter (fun item ->
            readItem item
            columns.Read.Text <- "✓"
            )
      )

    sourceListButton.Click.Add (fun e ->
      showSourceListForm ()
      )

    this.SizeChanged.Add (fun e -> resize ())

    this.FormClosed.Add (fun e ->
      rc.Save()
      saveState()
      )

  // Init controls
  do
    unshow ()
    resize ()
    loadState ()
    base.Controls.AddRange(controls)

  // Init reader
  do
    checkUpdate ()
    updateTimer.Start()
