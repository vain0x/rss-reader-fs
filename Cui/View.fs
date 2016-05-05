namespace RssReaderFs.Cui

open System
open System.IO
open System.Collections.Generic
open Chessie.ErrorHandling
open RssReaderFs

type View (rc: RssClient) =
  let reader () =
    rc.Reader

  member this.PrintUnknownSourceNameError(srcName) =
    eprintfn "Unknown source name: %s" srcName

  member this.PrintUnknownCommand(command: list<string>) =
    eprintfn "Unknown command: %s"
      (String.Join(" ", command))

  member this.PrintCount(items) =
    let len = items |> Array.length
    in
      if len = 0
      then printfn "No new items."
      else printfn "New %d items!" len

  member this.PrintItem(item: RssItem, ?header) =
    let header =
      match header with
      | Some h -> h + " "
      | None -> ""
    let src =
      reader () |> RssReader.tryFindFeed (item.Url)
    let () =
      printfn "%s%s" header (item.Title)
      printfn "* Date: %s" (item.Date.ToString("G"))
      printfn "* Link: %s" (item.LinkOpt |> Option.getOr "(no link)")
      src |> Option.iter (fun feed ->
          printfn "* From: %s" (feed.Name)
          )
      item.DescOpt |> Option.iter (printfn "* Desc:\r\n%s")
    let () =
      rc.ReadItem(item) |> ignore
    in ()

  member this.PrintItems(items: RssItem []) =
    let len = items |> Seq.length
    in
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

  member this.PrintItemTitles(items: RssItem []) =
    let len = items |> Array.length
    in
      if len = 0
      then printfn "No new items."
      else
        items
        |> Array.sortBy (fun item -> item.Date)
        |> Array.iter (fun item ->
            printfn "%s %s"
              (item.Date.ToString("G")) item.Title
            )

  member this.PrintFeed(feed) =
    printfn "%s" (feed |> RssFeed.nameUrl)

  member this.PrintFeeds(feeds) =
    feeds |> Array.iter (this.PrintFeed)

  member this.PrintRenameSourceResult(result) =
    if result
    then printfn "Some sources are renamed."
    else printfn "No sources are renamed."

  member this.PrintSources(srcs) =
    srcs
    |> Seq.iter (fun src ->
        printfn "%s" (src |> RssSource.name)
        )

  member this.PrintTag(tagName) =
    let srcNames = rc.Reader |> RssReader.findTaggedSourceNames tagName
    printfn "(tag %s %s)"
      (string tagName)
      (srcNames |> String.concat " ")

  member this.PrintResult(result) =
    match result with
    | Pass _ -> printfn "Succeeded."
    | Warn (_, msgs)
    | Fail msgs ->
        msgs |> List.iter (eprintfn "%s")
