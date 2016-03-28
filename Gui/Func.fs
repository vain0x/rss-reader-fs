namespace RssReaderFs.Gui

module MainListviewColumns =
  let toArray (self: MainListviewColumns<_, _, _, _>) =
    [|
      self.Title
      self.Read
      self.Date
      self.Source
    |]

  let ofSeq s =
    match s |> Seq.toList with
    | title :: read :: date :: source :: _ ->
        {
          Title       = title
          Read        = read
          Date        = date
          Source      = source
        } |> Some
    | _ -> None
