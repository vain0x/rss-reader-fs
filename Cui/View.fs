namespace RssReaderFs.Cui

open System
open System.IO
open System.Collections.Generic
open RssReaderFs

type View (rc: RssClient) =
  let items () =
    rc.Items

  let reader () =
    rc.Reader

  do rc.Subscribe (fun items ->
      let body () =
        printfn "New %d items!" (items |> Array.length)
      in lockConsole body
      ) |> ignore

  member this.PrintItem(item: RssItem, ?header) =
    let header =
      match header with
      | Some h -> h + " "
      | None -> ""
    let src =
      reader () |> RssReader.tryFindSource(item.Url)
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

  member this.PrintTimeLine() =
    let body () =
      let items = items ()
      let len = items |> Seq.length
      items
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
