# Repository.Migrations

## Setup (once)

`dotnet ef` is registered as a local tool (`.config/dotnet-tools.json`, version 10.0.9).
After cloning the repository, restore it from the solution root:

    dotnet tool restore

> Keep the `dotnet-ef` version `>=` the `Microsoft.EntityFrameworkCore.*` package version.
> When you upgrade EF Core, bump the tool too: `dotnet tool update dotnet-ef --version <version>`.
> An older tool within the same major only warns; a different major fails.

> All commands below are run from the solution root (if run from the `Repository.Migrations`
> folder, the `--project` and `--startup-project` arguments can be omitted).
> `--project` is where migrations are written, `--startup-project` is what the tool
> builds and runs. Here both point to `Repository.Migrations`.

## Create the first migration

    dotnet ef migrations add InitialCreate --project Repository.Migrations --startup-project Repository.Migrations

This creates a `Migrations/` folder in `Repository.Migrations` with the files
`<timestamp>_InitialCreate.cs`, `<timestamp>_InitialCreate.Designer.cs` and
`SqliteDbContextModelSnapshot.cs`.

## Create the next migration (after model changes)

    dotnet ef migrations add <Name> --project Repository.Migrations --startup-project Repository.Migrations

## Remove the last (not yet applied) migration

    dotnet ef migrations remove --project Repository.Migrations --startup-project Repository.Migrations

## List migrations

    dotnet ef migrations list --project Repository.Migrations --startup-project Repository.Migrations

## Generate a SQL script (without applying)

    dotnet ef migrations script --project Repository.Migrations --startup-project Repository.Migrations

## Generate an idempotent SQL script (safe for deployment)

Checks `__EFMigrationsHistory` and applies only the missing migrations, so it can be run
against a database in any state:

    dotnet ef migrations script --idempotent --project Repository.Migrations --startup-project Repository.Migrations

## Apply migrations to a database manually (usually not needed)

The application applies migrations on startup by itself. If needed:

    dotnet ef database update --project Repository.Migrations --startup-project Repository.Migrations

> Design time uses a dummy connection string `Data Source=design-time.db`

## Roll back migrations

`migrations remove` only deletes the last *not yet applied* migration. To undo an *applied*
migration, update the database to an earlier one (`0` reverts everything):

    dotnet ef database update <PreviousMigrationName> --project Repository.Migrations --startup-project Repository.Migrations
    dotnet ef database update 0 --project Repository.Migrations --startup-project Repository.Migrations

## Verify the wiring without creating files

    dotnet ef dbcontext info --project Repository.Migrations --startup-project Repository.Migrations

The output should contain `Provider name: Microsoft.EntityFrameworkCore.Sqlite`
and `Options: MigrationsAssembly=Repository.Migrations`.

## Check that the snapshot matches the model

Useful as a sanity check / in CI — fails if the model changed but no migration was added:

    dotnet ef migrations has-pending-model-changes --project Repository.Migrations --startup-project Repository.Migrations

## Troubleshooting

Append `--verbose` to any command above to see detailed diagnostic output when something fails.
