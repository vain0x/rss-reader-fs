namespace Chessie

open System
open Chessie.ErrorHandling

module UnitTest =
  type TestName = TestName of string

  type TestResult =
    | Success
    | Violated      of string
    | Raised        of exn

  let violated msg =
    () |> warn (Violated msg)

  let raised e =
    fail (Raised e)

  type TestBuilder(name: TestName) =
    member this.Zero()          = trial { return () }
    member this.Return(x)       = trial { return x }
    member this.ReturnFrom(x)   = x
    member this.Bind(x, f)      = Trial.bind f x
    member this.Delay(f)        = f
    member this.Run(f)          =
      fun () ->
        let r =
          try f ()
          with | e -> raised e
        match r with
        | Pass () -> [(name, Success)]
        | Warn ((), msgs)
        | Fail msgs -> msgs |> List.map (fun msg -> (name, msg))

  let runParallel (fs: seq<unit -> list<'x>>): list<'x> =
    fs
    |> Seq.map (fun f -> async { return f () })
    |> Async.Parallel
    |> Async.RunSynchronously
    |> Array.toList
    |> List.collect id

  module Console =
    type ResultCount =
      {
        Success         : int
        Violated        : int
        Raised          : int
      }

    let count results =
      let init =
        {
          Success       = 0
          Violated      = 0
          Raised        = 0
        }
      let folder k (_, r) =
        match r with
        | Success       -> { k with Success     = k.Success  + 1 }
        | Violated _    -> { k with Violated    = k.Violated + 1 }
        | Raised _      -> { k with Raised      = k.Raised   + 1 }
      in
        results |> List.fold folder init

    let printResults (results: list<TestName * TestResult>) =
      let printSeparator () =
        printfn "------------------------"
      let eprintResult =
        function
        | (_, Success) -> ()
        | (TestName name, Violated msg) ->
            printSeparator ()
            printfn "## '%s' VIOLATED" name
            printfn "%s" msg
        | (TestName name, Raised e) ->
            printSeparator ()
            printfn "## '%s' RAISED" name
            printfn "### Message"
            printfn "%s" e.Message
            printfn "### StackTrace"
            printfn "%s" e.StackTrace
      let total = count results
      do
        results |> List.iter eprintResult
        printSeparator ()
        printfn "(%d succeeded, %d violated, %d raised) from %d assertions"
          total.Success total.Violated total.Raised (results |> List.length)

  module Operators =
    let test name = TestBuilder(TestName name)

    let assertEquals expected actual =
      if expected = actual
      then pass ()
      else
        violated
          ( sprintf "  Expected: %A" expected + Environment.NewLine
          + sprintf "  Actual  : %A" actual   + Environment.NewLine )
