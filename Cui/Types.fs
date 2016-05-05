namespace RssReaderFs.Cui

open Chessie.ErrorHandling
open RssReaderFs

[<AutoOpen>]
module Types =
  type PrintFormat =
    | Count
    | Titles
    | Details

  type CommandResult =
    | Result                  of Result<unit, Error>
    | ArticleSeq              of Result<Async<Article [] * PrintFormat>, Error>
    | SourceSeq               of seq<Source>
    | UnknownCommand          of list<string>
