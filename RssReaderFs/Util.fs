namespace RssReaderFs

open System
open System.Collections.Generic

[<AutoOpen>]
module Misc =
  let tap f x = f x; x

  let flip f x y = f y x

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
  let collect f self =
    self |> Seq.map f |> Set.unionMany

module Map =
  let keySet self =
    self |> Map.toList |> List.map fst |> Set.ofList

  let valueSet self =
    self |> Map.toList |> List.map snd |> Set.ofList

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

module Url =
  let ofString = Url
  let toString (Url s) = s

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

module ObjectElementSeq =
  open System
  open System.Linq
  open Microsoft.FSharp.Reflection

  let cast (t: Type) (xs: obj seq) =
    let enumerable = typeof<Enumerable>
    let cast =
      let nonGeneric = enumerable.GetMethod("Cast")
      nonGeneric.MakeGenericMethod([| t |])
    cast.Invoke(null, [| xs |])

  let toSet (t: Type) (xs: obj seq) =
    let setType         = typedefof<Set<_>>.MakeGenericType(t)
    let parameter       = xs |> cast t
    let parameterType   = typedefof<seq<_>>.MakeGenericType([| t |])
    let constructor'    = setType.GetConstructor([| parameterType |])
    in constructor'.Invoke([| parameter |])

module Yaml =
  open FsYaml
  open FsYaml.NativeTypes
  open FsYaml.RepresentationTypes
  open FsYaml.CustomTypeDefinition

  let setDef =
    {
      Accept = isGenericTypeDef (typedefof<Set<_>>)
      Construct = fun construct' t ->
        function
        | Sequence (s, _) ->
            let elemType = t.GetGenericArguments().[0]
            let elems = s |> List.map (construct' elemType)
            in ObjectElementSeq.toSet elemType elems
        | otherwise -> raise (mustBeSequence t otherwise)
      Represent =
        representSeqAsSequence
    }

  let customDefs =
    [
      setDef
    ]

  let customDump x =
    Yaml.dumpWith customDefs x

  let customTryLoad<'t> =
    Yaml.tryLoadWith<'t> customDefs
