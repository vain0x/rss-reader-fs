namespace RssReaderFs

open System.Data.Entity
open SQLite.CodeFirst

module Database =
  type SampleDbContext() =
    inherit DbContext("RssReaderFsDb")

    override this.OnModelCreating(mb: DbModelBuilder) =
      // configure tables
      mb.Entity<Article       >() |> ignore
      mb.Entity<ReadLog       >() |> ignore
      mb.Entity<TwitterUser   >() |> ignore
      mb.Entity<RssFeed       >() |> ignore
      mb.Entity<Tag           >() |> ignore
      mb.Entity<Config        >() |> ignore

      Database.SetInitializer(SampleDbInitializer(mb))

  and SampleDbInitializer(mb) =
    inherit SqliteDropCreateDatabaseWhenModelChanges<SampleDbContext>(mb)

[<AutoOpen>]
module DatabaseExtension =
  type DbCtx = Database.SampleDbContext

  let withDb f =
    use ctx = new DbCtx()
    f ctx
