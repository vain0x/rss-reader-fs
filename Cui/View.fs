namespace RssReaderFs.Cui

open System
open System.IO
open System.Collections.Generic
open RssReaderFs

type View (rc: RssClient) =
  let reader () =
    rc.Reader

  member this.PrintCount(items) =
    let len = items |> Array.length
    let body () =
      if len = 0
      then printfn "No new items."
      else printfn "New %d items!" len
    in lockConsole body

  member this.PrintItem(item: RssItem, ?header) =
    let header =
      match header with
      | Some h -> h + " "
      | None -> ""
    let src =
      reader () |> RssReader.tryFindFeed (item.Url)
    let body () =
      printfn "%s%s" header (item.Title)
      printfn "* Date: %s" (item.Date.ToString("G"))
      printfn "* Link: %s" (item.Link |> Option.getOr "(no link)")
      src |> Option.iter (fun { Name = name } ->
          printfn "* From: %s" name
          )
      item.Desc |> Option.iter (printfn "* Desc:\r\n%s")

      rc.ReadItem(item)
    in lockConsole body

  member this.PrintItems(items) =
    let len = items |> Seq.length
    let body () =
      if len = 0
      then printfn "No new items."
      else
        items
        |> Seq.sortBy (fun item -> item.Date)
        |> Seq.iteri (fun i item ->
            if i > 0 then
              printfn "..."
              Console.ReadKey() |> ignore

            printfn "----------------"
            this.PrintItem
              ( item
              , (sprintf "[%3d/%3d]" i len)
              )
            )
    in lockConsole body
