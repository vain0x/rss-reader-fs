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
    | Result                  of Result<unit, string>
    | ArticleSeq              of Result<Async<Article [] * PrintFormat>, string>
    | SourceSeq               of seq<Source>
    | UnknownSourceName       of string
    | UnknownCommand          of list<string>
