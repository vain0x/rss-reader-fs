namespace RssReaderFs.Core

open System.Data.Entity
open SQLite.CodeFirst

module Database =
  type SampleDbContext() =
    inherit DbContext("RssReaderFsDb")

    override this.OnModelCreating(mb: DbModelBuilder) =
      // configure tables
      mb.Entity<Article       >() |> ignore
      mb.Entity<ReadLog       >() |> ignore
      mb.Entity<Source        >() |> ignore
      mb.Entity<TwitterUser   >() |> ignore
      mb.Entity<RssFeed       >() |> ignore
      mb.Entity<Tag           >() |> ignore
      mb.Entity<TagToSource   >() |> ignore
      mb.Entity<BearTokenCache>() |> ignore

      Database.SetInitializer(SampleDbInitializer(mb))

  and SampleDbInitializer(mb) =
    inherit SqliteDropCreateDatabaseWhenModelChanges<SampleDbContext>(mb)

    override this.Seed(mb) =
      mb.Set<Source>().Add(Source(Name = "ALL")) |> ignore
      mb.SaveChanges() |> ignore

[<AutoOpen>]
module DatabaseExtension =
  type DbCtx = Database.SampleDbContext

module DbCtx =
  let set<'t when 't: not struct and 't: null> (ctx: DbCtx) =
    ctx.Set<'t>()

  let saving (ctx: DbCtx) x =
    x |> tap (fun _ -> ctx.SaveChanges() |> ignore)

  let withTransaction f (ctx: DbCtx) =
    let transaction = ctx.Database.BeginTransaction()
    try
      f transaction
      |> tap (fun _ -> transaction.Commit())
    with
    | _ ->
        transaction.Rollback()
        reraise ()
