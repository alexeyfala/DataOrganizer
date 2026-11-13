# Database migrations

## Setup (once)

`dotnet ef` is registered as a local tool (`.config/dotnet-tools.json`).
After cloning the repository, restore it from the solution root:

    dotnet tool restore

> Keep the `dotnet-ef` version equal to the `Microsoft.EntityFrameworkCore.*` package version;
> a guard test fails otherwise.
> When you upgrade EF Core, bump the tool too: `dotnet tool update dotnet-ef --version <version>`.
> The NuGet package manager of Visual Studio leaves the tool manifest alone.
> An older tool within the same major only warns; a different major fails.

> All commands below are run from the solution root (if run from the `src/DataOrganizer.Database.Migrations`
> folder, the `--project` and `--startup-project` arguments can be omitted).
> `--project` is where migrations are written, `--startup-project` is what the tool
> builds and runs. Here both point to `src/DataOrganizer.Database.Migrations`.

## Create the first migration

    dotnet ef migrations add InitialCreate --project src/DataOrganizer.Database.Migrations --startup-project src/DataOrganizer.Database.Migrations

This creates a `Migrations/` folder in `src/DataOrganizer.Database.Migrations` with the files
`<timestamp>_InitialCreate.cs`, `<timestamp>_InitialCreate.Designer.cs` and
`SqliteDbContextModelSnapshot.cs`.

## Create the next migration (after model changes)

    dotnet ef migrations add <Name> --project src/DataOrganizer.Database.Migrations --startup-project src/DataOrganizer.Database.Migrations

## Remove the last (not yet applied) migration

    dotnet ef migrations remove --project src/DataOrganizer.Database.Migrations --startup-project src/DataOrganizer.Database.Migrations

## List migrations

    dotnet ef migrations list --project src/DataOrganizer.Database.Migrations --startup-project src/DataOrganizer.Database.Migrations

## Generate a SQL script (without applying)

    dotnet ef migrations script --project src/DataOrganizer.Database.Migrations --startup-project src/DataOrganizer.Database.Migrations

## Generate an idempotent SQL script (safe for deployment)

Checks `__EFMigrationsHistory` and applies only the missing migrations, so it can be run
against a database in any state:

    dotnet ef migrations script --idempotent --project src/DataOrganizer.Database.Migrations --startup-project src/DataOrganizer.Database.Migrations

## Apply migrations to a database manually (usually not needed)

The application applies migrations on startup by itself. If needed:

    dotnet ef database update --project src/DataOrganizer.Database.Migrations --startup-project src/DataOrganizer.Database.Migrations

> Design time uses a dummy connection string `Data Source=design-time.db`

## Roll back migrations

`migrations remove` only deletes the last *not yet applied* migration. To undo an *applied*
migration, update the database to an earlier one (`0` reverts everything):

    dotnet ef database update <PreviousMigrationName> --project src/DataOrganizer.Database.Migrations --startup-project src/DataOrganizer.Database.Migrations
    dotnet ef database update 0 --project src/DataOrganizer.Database.Migrations --startup-project src/DataOrganizer.Database.Migrations

## Verify the wiring without creating files

    dotnet ef dbcontext info --project src/DataOrganizer.Database.Migrations --startup-project src/DataOrganizer.Database.Migrations

The output should contain `Provider name: Microsoft.EntityFrameworkCore.Sqlite`
and `Options: MigrationsAssembly=DataOrganizer.Database.Migrations`.

## Check that the snapshot matches the model

Useful as a sanity check / in CI — fails if the model changed but no migration was added:

    dotnet ef migrations has-pending-model-changes --project src/DataOrganizer.Database.Migrations --startup-project src/DataOrganizer.Database.Migrations

## Troubleshooting

Append `--verbose` to any command above to see detailed diagnostic output when something fails.
