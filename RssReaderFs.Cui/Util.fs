namespace RssReaderFs.Cui

[<AutoOpen>]
module Misc =
  let lockConsole f =
    lock (new obj()) f
