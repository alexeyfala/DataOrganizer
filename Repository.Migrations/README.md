# Repository.Migrations

Отдельный проект с миграциями EF Core для `SqliteDbContext`.
Сам контекст и модель живут в проекте `Repository`, а файлы миграций — здесь,
чтобы не засорять `Repository`.

## Как это устроено

- `SqliteDbContext.MigrationsAssemblyName` = `"Repository.Migrations"` — единая константа
  с именем сборки миграций.
- Имя сборки прописано в двух местах:
  - рантайм: `App.axaml.cs` → `ConfigureDbContext` → `UseSqlite(..., x => x.MigrationsAssembly(...))`;
  - дизайн-тайм: `SqliteDbContextFactory` (этот проект).
- `SqliteDbContextFactory : IDesignTimeDbContextFactory<SqliteDbContext>` — фабрика, которую
  инструмент `dotnet ef` использует при создании/применении миграций. Благодаря ей пакет
  `Microsoft.EntityFrameworkCore.Design` нужен только здесь, а не в `Repository`.
- В приложении миграции применяются автоматически: `DbAccess` вызывает `HasMigrations()`
  (смотрит в настроенную сборку миграций) и, если миграции есть, делает `Migrate()`,
  иначе — `EnsureCreated()`.

## Подготовка (один раз)

`dotnet ef` подключён как локальный инструмент (`.config/dotnet-tools.json`, версия 10.0.9).
После клонирования репозитория его нужно восстановить:

    dotnet tool restore

> Все команды ниже выполняются из корня решения.
> `--project` — куда писать миграции, `--startup-project` — что собирает и запускает инструмент.
> У нас оба указывают на `Repository.Migrations`.

## Создать первую миграцию

    dotnet ef migrations add InitialCreate --project Repository.Migrations --startup-project Repository.Migrations

После этого в `Repository.Migrations` появится папка `Migrations/` с файлами
`<timestamp>_InitialCreate.cs`, `<timestamp>_InitialCreate.Designer.cs` и
`SqliteDbContextModelSnapshot.cs`.

## Создать следующую миграцию (после изменений в модели)

    dotnet ef migrations add <Name> --project Repository.Migrations --startup-project Repository.Migrations

## Удалить последнюю (ещё не применённую) миграцию

    dotnet ef migrations remove --project Repository.Migrations --startup-project Repository.Migrations

## Посмотреть список миграций

    dotnet ef migrations list --project Repository.Migrations --startup-project Repository.Migrations

## Сгенерировать SQL-скрипт (без применения)

    dotnet ef migrations script --project Repository.Migrations --startup-project Repository.Migrations

## Применить миграции к БД вручную (обычно не нужно)

В приложении миграции применяются сами при старте. Но при необходимости:

    dotnet ef database update --project Repository.Migrations --startup-project Repository.Migrations

> Дизайн-тайм использует фиктивную строку подключения `Data Source=design-time.db`
> (см. `SqliteDbContextFactory`) — она нужна только для построения модели; реальная БД
> приложения задаётся в `App.axaml.cs`.

## Проверка обвязки без создания файлов

    dotnet ef dbcontext info --project Repository.Migrations --startup-project Repository.Migrations

В выводе должно быть `Provider name: Microsoft.EntityFrameworkCore.Sqlite`
и `Options: MigrationsAssembly=Repository.Migrations`.
