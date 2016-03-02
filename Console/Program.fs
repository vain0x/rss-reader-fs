
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

  let mutable items = []

  member this.Save() =
    cfg.SaveReader(reader)

  member this.Update() =
    async {
      do! reader.Update()
      items <- reader.Timeline

      let len = items |> List.length
      if len > 0 then
        do!
          Console.Out.WriteLineAsync(sprintf "New %d feeds!" len)
          |> Async.AwaitTaskVoid
    }

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

  member this.CheckNewFeedsAsync(?timeout, ?thresh) =
    let timeout = defaultArg timeout  (5 * 60 * 1000)  // 5 min
    let thresh  = defaultArg thresh   1

    let rec loop () =
      async {
        do! this.Update()
        do! Async.Sleep(timeout)
        return! loop ()
      }
    in
      loop ()

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
    rrc.CheckNewFeedsAsync()
    |> Async.Start

    rrc.Interactive()
    |> Async.RunSynchronously
  finally
    rrc.Save()

  // exit code
  0
