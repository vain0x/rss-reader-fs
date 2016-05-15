namespace RssReaderFs.Core

module Error =
  let toString =
    function
    | ExnError exn ->
        exn.Message
    | SourceAlreadyExists srcName ->
        sprintf "Source '%s' does already exist." srcName
    | SourceDoesNotExist srcName ->
        sprintf "Source '%s' doesn't exist." srcName
    | SourceCannotBeRemoved srcName ->
        sprintf "Source '%s' can't be removed." srcName
    | SourceCannotBeRenamed srcName ->
        sprintf "Source '%s' can't be renamed." srcName
    | SourceDoesNotHaveTag (srcName, tagName) ->
        sprintf "Source '%s' doesn't have the tag '%s'." srcName tagName
    | SourceIsNotATag srcName ->
        sprintf "Source '%s' isn't a tag." srcName
