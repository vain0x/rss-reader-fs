namespace RssReaderFs.Cui

open System
open System.IO
open System.Collections.Generic
open Chessie.ErrorHandling
open RssReaderFs

type CommandResult =
  | Result                  of Result<unit, string>
  | ResultAsync             of Result<Async<unit>, string>
  | SourceSeq               of seq<Source>
  | UnknownSourceName       of string
  | UnknownCommand          of list<string>

type View (rc: RssClient) =
  let reader () =
    rc.Reader

  member this.PrintCount(items) =
    let len = items |> Array.length
    in
      if len = 0
      then printfn "No new items."
      else printfn "New %d items!" len

  member this.PrintItem(item: Article, ?header) =
    let header =
      match header with
      | Some h -> h + " "
      | None -> ""
    let src =
      reader () |> RssReader.tryFindFeed (item.Url)
    let () =
      printfn "%s%s" header (item.Title)
      printfn "* Date: %s" (item.Date.ToString("G"))
      printfn "* Link: %s" (item.Link |> Option.getOr "(no link)")
      src |> Option.iter (fun feed ->
          printfn "* From: %s" (feed.Name)
          )
      item.Desc |> Option.iter (printfn "* Desc:\r\n%s")
    let () =
      rc.ReadItem(item) |> ignore
    in ()

  member this.PrintItems(items: Article []) =
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

  member this.PrintItemTitles(items: Article []) =
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

  member this.PrintSource(src) =
    printfn "%s" (rc.Reader |> RssReader.dumpSource src)

  member this.PrintSources(srcs) =
    for src in srcs do 
      this.PrintSource(src)

  member this.PrintMessages(msgs) =
    msgs |> List.iter (eprintfn "%s")

  member this.PrintResult(result) =
    match result with
    | Pass () -> printfn "Succeeded."
    | Warn ((), msgs)
    | Fail msgs -> this.PrintMessages(msgs)

  member this.PrintCommandResult(result) =
    async {
      match result with
      | Result r -> this.PrintResult(r)
      | ResultAsync r ->
          match r with
          | Pass a ->
              do! a
              printfn "Succeeded."
          | Warn (a, msgs) ->
              do! a
              this.PrintMessages(msgs)
          | Fail msgs ->
              this.PrintMessages(msgs)
      | SourceSeq srcs ->
          this.PrintSources(srcs)
      | UnknownSourceName srcName ->
          eprintfn "Unknown source name: %s" srcName
      | UnknownCommand command ->
          eprintfn "Unknown command: %s" (command |> String.concat " ")
    }
