namespace RssReaderFs.Core.Test

open RssReaderFs.Core
open Chessie.ErrorHandling
open Chessie.UnitTest.Operators

module SourceTest =
  let tests =
    [
      test "allFeedTest" {
        let expected  = defaultFeeds |> List.map fst |> Set.ofList
        let actual    = ctx |> Source.allFeeds |> Array.map Source.name |> Set.ofArray
        do! actual |> assertEquals expected
      }

      test "allTwitterUsersTest" {
        let expected  = defaultTwitterUsers |> Set.ofList
        let actual    = ctx |> Source.allTwitterUsers |> Array.map Source.name |> Set.ofArray
        do! actual |> assertEquals expected
      }
    ]
