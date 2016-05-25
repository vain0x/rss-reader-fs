﻿namespace RssReaderFs.Core

open System
open System.Linq
open CoreTweet
open Chessie.ErrorHandling

module Twitter =
  let tryGetAppOnlyTokenAsync () =
    SecretSettings.consumerKeySecret |> Option.map (fun (key, secret) ->
      OAuth2.GetTokenAsync(key, secret) |> Async.AwaitTask
      )

  let tryGetAppOnlyToken () =
    tryGetAppOnlyTokenAsync () |> Option.map Async.RunSynchronously

  let tryCreateAppOnlyToken bearToken =
    SecretSettings.consumerKeySecret |> Option.map (fun (key, secret) ->
      OAuth2Token.Create(key, secret, bearToken)
      )

  /// Get or create using cache
  let tryFetchAppOnlyToken ctx =
    let btcs = ctx |> DbCtx.set<BearTokenCache>
    match btcs.FirstOrDefault() |> Option.ofObj with
    | Some btc ->
        tryCreateAppOnlyToken btc.BearToken
    | None ->
        tryGetAppOnlyTokenAsync () |> Option.map (fun a ->
          a |> Async.RunSynchronously
          |> tap (fun token ->
            btcs.Add(BearTokenCache(BearToken = token.BearerToken)) |> ignore
            ))

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

  let validate (name: string) token =
    tryFindUser name token
    |> Trial.ignore
    |> Trial.mapFailure (List.map ExnError)

  module Status =
    let permanentLink (status: Status) =
      sprintf "https://twitter.com/%s/status/%d" status.User.ScreenName status.Id
