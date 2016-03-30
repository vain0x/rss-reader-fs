namespace RssReaderFs.Gui

open System
open System.Drawing
open System.Windows.Forms
open RssReaderFs

type SourceListForm (rc: RssClient) as this =
  inherit Form
    ( Text    = "Sources - RssReaderFs.Gui"
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

        rc.Reader |> RssReader.sources
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
        new SourceAddForm
          (fun feed ->
            if feed.Name <> "" then
              rc.AddFeed(feed)

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
        let url = Url.ofString (columns.Url.Text)
        do rc.RemoveFeed(url)
      )

    base.Controls.AddRange(controls)

    listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent)
