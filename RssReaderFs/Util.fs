namespace RssReaderFs

open System
open System.Collections.Generic
open Printf

[<AutoOpen>]
module Misc =
  let tap f x = f x; x

  let flip f x y = f y x

  let replace src dst self =
    if self = src
    then dst
    else self

module Nullable =
  let toOption =
    function
    | null -> None
    | x -> Some x

module Option =
  let getOr def =
    function
    | None -> def
    | Some x -> x

  let ofTrial =
    function
    | (true, value) -> Some value
    | _ -> None

module Seq =
  let inline ofCollection
      (self: ^T when ^T: (member Item: int -> _) and ^T: (member Count: int))
      =
    let len = (^T: (member Count: int) self)
    seq {
      for i in 0..(len - 1) ->
        (^T: (member Item: int -> _) (self, i))
      }

module Array =
  let tryItem i self =
    if 0 <= i && i < (self |> Array.length)
    then Some (self.[i])
    else None

  let replace src dst self =
    self |> Array.map (replace src dst)

module Dictionary =
  open System.Collections.Generic

  let ofSeq kvs =
    Dictionary()
    |> tap (fun dict ->
        kvs
        |> Seq.iter (fun (k, v) -> dict.Add(k, v) |> ignore)
        )

  let toArray (dict: Dictionary<_, _>) =
    [|
      for KeyValue (k, v) in dict do
        yield (k, v)
      |]

module Set =
  let ofOption: option<'x> -> Set<'x> =
    function
    | Some x -> Set.singleton x
    | None -> Set.empty

  let collect f self =
    self |> Seq.map f |> Set.unionMany

  let choose (f: 'x -> option<'y>) (self: Set<'x>): Set<'y> =
    self |> collect (fun x -> f x |> ofOption)

  let tryFind value self =
    if self |> Set.contains value
    then Some value
    else None

module Map =
  let keySet self =
    self |> Map.toList |> List.map fst |> Set.ofList

  let valueSet self =
    self |> Map.toList |> List.map snd |> Set.ofList

  let update key valueOpt self =
    let old     = self |> Map.tryFind key
    let self'   =
      match valueOpt with
      | Some value  -> self |> Map.add key value
      | None        -> self |> Map.remove key
    in (self', old)

  let replaceKey oldKey newKey self =
    match self |> Map.tryFind oldKey with
    | None -> self
    | Some value ->
        self
        |> Map.remove oldKey
        |> Map.add newKey value

module DateTime =
  let tryParse s =
    DateTime.TryParse(s)
    |> Option.ofTrial

module Xml =
  open System.Xml

  let innerText (xnode: XmlNode) =
    xnode.InnerText

  let selectSingleNode xpath (xnode: XmlNode) =
    xnode.SelectSingleNode(xpath)
    |> Nullable.toOption

  let selectNodes xpath (xnode: XmlNode) =
    xnode.SelectNodes(xpath)
    |> Seq.ofCollection

[<AutoOpen>]
module UrlType =
  type Url = 
    | Url of string
  with
    override this.ToString() =
      let (Url s) = this in s

module Url =
  let ofString = Url
  let toString (Url s) = s

module Exn =
  let message (e: exn) = e.Message

module Net =
  open System.Net
  open System.Xml

  let downloadXmlAsync (feedUrl: Url) =
    async {
      let req       = WebRequest.Create(feedUrl |> Url.toString)
      let! resp     = req.GetResponseAsync() |> Async.AwaitTask
      let stream    = resp.GetResponseStream()
      let xmlReader = new XmlTextReader(stream)
      let xmlDoc    = new XmlDocument()
      xmlDoc.Load(xmlReader)
      return xmlDoc
    }

module Async =
  open System.Threading.Tasks

  let AwaitTaskVoid : (Task -> Async<unit>) =
    Async.AwaitIAsyncResult >> Async.Ignore

module Trial =
  open Chessie.ErrorHandling

  let failf fmt =
    kprintf fail fmt

  /// Runs a raisable function. Wraps the exception into Result.
  let runRaisable (f: unit -> 't): Result<'t, exn> =
    try
      f () |> pass
    with
    | :? AggregateException as e ->
        e.InnerExceptions |> Seq.toList |> Result.Bad
    | e ->
        fail e

  /// Map the message list by function f.
  let mapMessages
      (f: list<'t> -> list<'u>) (self: Result<'x, 't>): Result<'x, 'u>
    =
    match self with
    | Result.Ok (r, msgs)     -> Result.Ok  (r, msgs |> f)
    | Result.Bad msgs         -> Result.Bad (msgs |> f)

  let mapExnToMessage (self: Result<_, exn>): Result<_, string> =
    self |> mapMessages (List.map Exn.message)
