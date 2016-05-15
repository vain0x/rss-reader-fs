namespace RssReaderFs.Core.Test

open Chessie
open Chessie.UnitTest.Operators

module Program =
  [<EntryPoint>]
  let main args =
    SourceTest.tests
    |> UnitTest.runParallel
    |> UnitTest.Console.printResults
    0
