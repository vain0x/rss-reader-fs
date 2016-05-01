namespace RssReaderFs

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

  let userTweetsAsync name (token: OAuth2Token) =
    token.Statuses.UserTimelineAsync(Map.singleton "screen_name" name)
    |> Async.AwaitTask

  let tryFindUser name (token: OAuth2Token) =
    try
      token.Users.Lookup(Map.singleton "screen_name" name).Item(0)
      |> pass
    with
    | _ -> fail (Exception("No user found."))
