
open System
open System.IO
open RssReaderFs

type RssReader = RssReader.RssReader

type Config (path) =
  member this.LoadReader() =
    let json =
      File.ReadAllText(path)
    RssReader.RssReader(json)

  member this.SaveReader(r: RssReader) =
    let json = r.SerializedFeeds
    File.WriteAllText(path, json)

type RssReaderConsole (cfg: Config) =
  let reader =
    cfg.LoadReader()

  member this.Save() =
    cfg.SaveReader(reader)

  member this.PrintItem(item, ?header) =
    let header =
      match header with
      | Some h -> h + " "
      | None -> ""

    printfn "%s%s" header (item.Title)
    printfn "* Date: %s" (item.Date.ToString("G"))
    printfn "* Link: %s" (item.Link |> Option.getOr "(no link)")
    printfn "* From: %s" (item.Uri  |> string)
    item.Desc |> Option.iter (printfn "* Desc:\r\n%s")

  member this.PrintTimeLine() =
    let items = reader.Timeline
    let len = items |> List.length
    items
    |> List.iteri (fun i item ->
        if i > 0 then
          printfn "..."
          Console.ReadKey() |> ignore

        printfn "----------------"
        this.PrintItem
          ( item
          , (sprintf "[%3d/%3d]" i len)
          )
        )

  member this.Passive() =
    async {
      while true do
        reader.Update()
        this.PrintTimeLine()
        do! Async.Sleep(1000)
    }

  member this.Interactive() =
    let rec loop () = async {
      let! line = Console.In.ReadLineAsync() |> Async.AwaitTask
      match line with
      | null | "" ->
          return! loop ()
      | "quit" | "halt" | "exit" ->
          ()
      | line ->
          return! loop ()
      }
    in
      loop ()

[<EntryPoint>]
let main argv =
  let cfg = Config(@"feeds.json")
  let rrc = RssReaderConsole(cfg)

  try
    rrc.Interactive()
    |> Async.RunSynchronously
  finally
    rrc.Save()

  // exit code
  0
