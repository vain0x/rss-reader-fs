namespace RssReaderFs.Core

open System
open CoreTweet
open Chessie.ErrorHandling

module Twitter =
  let getAppOnlyTokenAsync () =
    OAuth2.GetTokenAsync
      ( SecretSettings.consumerKey
      , SecretSettings.consumerSecret
      ) |> Async.AwaitTask

  let getAppOnlyToken () =
    getAppOnlyTokenAsync () |> Async.RunSynchronously

  let createAppOnlyToken bearToken =
    OAuth2Token.Create(SecretSettings.consumerKey, SecretSettings.consumerSecret, bearToken)

  let userTweetsAsync (name: string) (sinceId: int64) (token: OAuth2Token) =
    let args =
      [ yield ("screen_name", name :> obj)
        yield ("count", 200 :> obj)
        if sinceId > 0L then yield ("since_id", sinceId :> obj)
      ] |> Map.ofList
    in
      token.Statuses.UserTimelineAsync(args)
      |> Async.AwaitTask

  let tryFindUser (name: string) (token: OAuth2Token) =
    try
      token.Users.Lookup(Map.singleton "screen_name" name).Item(0)
      |> pass
    with
    | _ -> fail (Exception("No user found."))

  module Status =
    let permanentLink (status: Status) =
      sprintf "https://twitter.com/%s/status/%d" status.User.ScreenName status.Id
