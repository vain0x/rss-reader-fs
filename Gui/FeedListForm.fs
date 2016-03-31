namespace RssReaderFs.Gui

open System
open System.Drawing
open System.Windows.Forms
open RssReaderFs

type FeedListForm (rc: RssClient) as this =
  inherit Form
    ( Text    = "Feeds - RssReaderFs.Gui"
    , Size    = Size(480, 360)
    )

  let lvItemFromRssFeed (feed: RssFeed) =
    ListViewItem([| feed.Name; feed.Url |> Url.toString |])

  let listView =
    new ListView
      ( Location    = Point(5, 5)
      , Size        = Size(this.ClientSize.Width - 10, this.ClientSize.Height - 40)
      , Font        = yuGothic10
      , View        = View.Details
      )
    |> tap (fun listView ->
        // Add columns

        let columns =
          ({
            Name      = "Name"
            Url       = "Url"
          }: SourceListviewColumns<_, _>)
          |> SourceListviewColumns.toArray
          |> Array.map (fun text -> new ColumnHeader(Text = text))
        do listView.Columns.AddRange(columns)

        // Add initial rows

        rc.Reader |> RssReader.allFeeds
        |> Array.map lvItemFromRssFeed
        |> (fun lvItems ->
            listView.Items.AddRange(lvItems)
            )
        )

  let subitems (item: ListViewItem) =
    let sis = item.SubItems
    let () = assert (sis.Count >= 2)
    in
      [ for i in 0..(sis.Count - 1) -> sis.[i] ]
      |> SourceListviewColumns.ofSeq
      |> Option.get  // use assumption

  let addButton =
    new Button
      ( Location    = Point(5, this.ClientSize.Height - 5 - 25)
      , Size        = Size(80, 25)
      , Font        = yuGothic10
      , Text        = "Add"
      )

  let removeButton =
    new Button
      ( Location    = Point(5 + addButton.Size.Width + 5, addButton.Location.Y)
      , Size        = addButton.Size
      , Font        = yuGothic10
      , Text        = "Remove"
      )

  let controls =
    [|
      listView      :> Control
      addButton     :> Control
      removeButton  :> Control
    |]

  let showAddForm =
    Form.singletonSubform
      (fun () ->
        new FeedAddForm
          (fun feed ->
            if feed.Name <> "" then
              rc.AddSource(feed |> RssSource.ofFeed) |> ignore

            listView.Items.Add(lvItemFromRssFeed feed) |> ignore
            )
        )

  do
    addButton.Click.Add (fun e ->
      showAddForm ()
      )

    removeButton.Click.Add (fun e ->
      let selectedItems = listView.SelectedItems
      for i in 0..(selectedItems.Count - 1) do
        let lvItem = selectedItems.Item(i)
        let columns = lvItem |> subitems
        do rc.RemoveSource(columns.Name.Text) |> ignore
      )

    base.Controls.AddRange(controls)

    listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent)
