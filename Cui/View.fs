namespace RssReaderFs.Cui

open System
open System.IO
open System.Collections.Generic
open Chessie.ErrorHandling
open RssReaderFs.Core

type View (rc: RssReader) =
  let reader () =
    rc

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
      rc |> RssReader.readItem item |> ignore
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
    printfn "%s" (rc |> RssReader.dumpSource src)

  member this.PrintSources(srcs) =
    for src in srcs do 
      this.PrintSource(src)

  member this.PrintArticles(items, fmt) =
    match fmt with
    | Count     -> this.PrintCount(items)
    | Titles    -> this.PrintItemTitles(items)
    | Details   -> this.PrintItems(items)

  member this.PrintMessages(msgs) =
    msgs |> List.iter (Error.toString >> eprintfn "%s")

  member this.PrintResult(result) =
    match result with
    | Pass () -> printfn "Succeeded."
    | Warn ((), msgs)
    | Fail msgs -> this.PrintMessages(msgs)

  member this.PrintCommandResult(result) =
    async {
      match result with
      | Result r -> this.PrintResult(r)
      | ArticleSeq r ->
          match r with
          | Pass a ->
              let! (items, fmt) = a
              this.PrintArticles(items, fmt)
          | Warn (a, msgs) ->
              let! (items, fmt) = a
              this.PrintMessages(msgs)
              this.PrintArticles(items, fmt)
          | Fail msgs ->
              this.PrintMessages(msgs)
      | SourceSeq srcs ->
          this.PrintSources(srcs)
      | UnknownCommand command ->
          eprintfn "Unknown command: %s" (command |> String.concat " ")
    }

  member this.Interactive(rrc: Ctrl) =
    let rec loop () =
      async {
        let! line = Console.In.ReadLineAsync() |> Async.AwaitTask
        return! rrc.ProcCommandLine(loop (), line)
      }
    in loop ()
