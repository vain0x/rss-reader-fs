namespace RssReaderFs

open System

module RssSource =
  let downloadAsync (source: RssSource) =
    async {
      let url = source.Url
      let! xml = Net.downloadXmlAsync(url)
      return (xml |> RssItem.parseXml url)
    }

  let updateAsync src =
    async {
      return! src |> downloadAsync
    }

  let create name (url: string) =
    {
      Name        = name
      Url         = Url.ofString (url)
    }
    
  module Serialize =
    open System.IO

    let load path =
      try
        let json =
          File.ReadAllText(path)
        let sources =
          Serialize.deserializeJson<RssSource []>(json)
        in
          sources |> Some
      with
      | _ -> None

    let save path (sources) =
      let json =
        Serialize.serializeJson(sources)
      in
        File.WriteAllText(path, json)
