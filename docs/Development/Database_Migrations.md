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

> `.config/dotnet-ef.json` gives `dotnet ef` its default options, so the commands below work from any folder
> of the repository. `project` is where migrations are written, `startupProject` is what the tool builds and
> runs; both point to `src/DataOrganizer.Database.Migrations`. An option on the command line overrides the file.

## Create the first migration

    dotnet ef migrations add InitialCreate

This creates a `Migrations/` folder in `src/DataOrganizer.Database.Migrations` with the files
`<timestamp>_InitialCreate.cs`, `<timestamp>_InitialCreate.Designer.cs` and
`SqliteDbContextModelSnapshot.cs`.

## Create the next migration (after model changes)

    dotnet ef migrations add <Name>

## Remove the last (not yet applied) migration

    dotnet ef migrations remove

## List migrations

    dotnet ef migrations list

## Generate a SQL script (without applying)

    dotnet ef migrations script

## Generate an idempotent SQL script (safe for deployment)

Checks `__EFMigrationsHistory` and applies only the missing migrations, so it can be run
against a database in any state:

    dotnet ef migrations script --idempotent

## Apply migrations to a database manually (usually not needed)

The application applies migrations on startup by itself. If needed:

    dotnet ef database update

> Design time uses a dummy connection string `Data Source=design-time.db`

## Roll back migrations

`migrations remove` only deletes the last *not yet applied* migration. To undo an *applied*
migration, update the database to an earlier one (`0` reverts everything):

    dotnet ef database update <PreviousMigrationName>
    dotnet ef database update 0

## Verify the wiring without creating files

    dotnet ef dbcontext info

The output should contain `Provider name: Microsoft.EntityFrameworkCore.Sqlite`
and `Options: MigrationsAssembly=DataOrganizer.Database.Migrations`.

## Check that the snapshot matches the model

Useful as a sanity check / in CI — fails if the model changed but no migration was added:

    dotnet ef migrations has-pending-model-changes

## Troubleshooting

Append `--verbose` to any command above to see detailed diagnostic output when something fails.
