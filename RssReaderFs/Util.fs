namespace RssReaderFs

open System

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

module Net =
  open System.Net
  open System.Xml

  let downloadXmlAsync (feedUri: Uri) =
    async {
      let req       = WebRequest.Create(feedUri)
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

module Serialize =
  open System.Text
  open System.Runtime.Serialization
  open System.Runtime.Serialization.Json

  let private toString = Encoding.UTF8.GetString
  let private toBytes (x : string) = Encoding.UTF8.GetBytes x

  let serializeJson<'a> (x : 'a) = 
      let jsonSerializer = new DataContractJsonSerializer(typedefof<'a>)

      use stream = new IO.MemoryStream()
      jsonSerializer.WriteObject(stream, x)
      toString <| stream.ToArray()

  let deserializeJson<'a> (json : string) =
      let jsonSerializer = new DataContractJsonSerializer(typedefof<'a>)

      use stream = new IO.MemoryStream(toBytes json)
      jsonSerializer.ReadObject(stream) :?> 'a
