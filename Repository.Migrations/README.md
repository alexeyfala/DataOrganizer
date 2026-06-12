# Repository.Migrations

## Setup (once)

`dotnet ef` is registered as a local tool (`.config/dotnet-tools.json`, version 10.0.9).
After cloning the repository it must be restored:

    dotnet tool restore

> All commands below are run from the solution root.
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

## Apply migrations to a database manually (usually not needed)

The application applies migrations on startup by itself. If needed:

    dotnet ef database update --project Repository.Migrations --startup-project Repository.Migrations

> Design time uses a dummy connection string `Data Source=design-time.db`

## Verify the wiring without creating files

    dotnet ef dbcontext info --project Repository.Migrations --startup-project Repository.Migrations

The output should contain `Provider name: Microsoft.EntityFrameworkCore.Sqlite`
and `Options: MigrationsAssembly=Repository.Migrations`.
