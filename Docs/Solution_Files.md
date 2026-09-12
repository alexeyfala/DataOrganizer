# Справочник по не-кодовым файлам решения

## Корень

| Файл | Зачем нужен | Когда сюда лезть |
|---|---|---|
| `.editorconfig` | Стиль кода и правила именования; вместе с `EnforceCodeStyleInBuild` нарушения идут в вывод компилятора. | Меняется соглашение. Секция `[*.{csproj,wixproj,props,targets,wxs,wxi}]` держит табы в MSBuild и WiX. |
| `.gitattributes` | Нормализация окончаний строк (`* text=auto`); остальное — закомментированный шаблон Visual Studio. | Почти никогда. |
| `.gitignore` | Что не попадает в репозиторий: `bin/`, `obj/`, `.vs/`, `Publish/`, `Setup/LICENSE.rtf`. | Появился новый генерируемый артефакт. |
| `Directory.Build.props` | Источник версии и имён: `AppVersion`, `AppName`, `AppDisplayName`, `Manufacturer`, `License`, иконка, имя MSI; плюс метаданные сборок. | Перед релизом — поднять `AppVersion`. |
| `DataOrganizerApp.slnx` | Решение: проекты, платформы, виртуальные папки. WiX-проекты помечены `Build="false"` и обычной сборкой не собираются. | Добавлен проект или файл, который должен быть виден в обозревателе решений. |
| `README.md` | Витрина на GitHub: возможности, скриншоты, требования, сборка, лицензия. | Изменились возможности или требования. |
| `CODE_OF_CONDUCT.md` | Contributor Covenant. | Сменился контакт для жалоб. |
| `CONTRIBUTING.md` | Способы участия и требования к pull request. | Изменился процесс работы. |
| `SECURITY.md` | Политика безопасности: приватный канал, состав отчёта, границы модели угроз. | Изменились гарантии шифрования или поддерживаемые версии. |
| `LICENSE` | Apache 2.0; единственная рукописная копия лицензии. | Не править. |
| `NOTICE` | Уведомление по Apache 2.0: копирайт, LGPL-компонент libuiohook, оговорка о названии продукта. | Сменился год копирайта или появился компонент с требованием уведомления. |
| `THIRD-PARTY-NOTICES.txt` | Сторонние компоненты и их лицензии; генерируется `tools/gen-third-party-notices.ps1`. | Не править руками — перегенерировать перед релизом. |

`app.pupnet.conf` и `app.metainfo.xml` тоже лежат в корне — см. [Deployment](#deployment).

## `.config/`

| Файл | Зачем нужен | Когда сюда лезть |
|---|---|---|
| `dotnet-tools.json` | Локальные инструменты: `dotnet-ef`. Восстановление — `dotnet tool restore`. | Обновление EF Core: версия инструмента не ниже версии пакетов. Команды миграций — [Repository.Migrations/README.md](../Repository.Migrations/README.md). |

## `.github/`

| Файл | Зачем нужен | Когда сюда лезть |
|---|---|---|
| `dependabot.yml` | Еженедельные обновления NuGet и GitHub Actions; minor и patch — одним pull request. | Обновлений приходит слишком много или мало. |
| `release.yml` | Категории и метки для кнопки «Generate release notes»; метка `ignore-for-release` прячет pull request. | Изменился набор меток. |
| `PULL_REQUEST_TEMPLATE.md` | Заготовка описания pull request с чек-листом. | Меняется чек-лист. |
| `ISSUE_TEMPLATE/bug_report.yml` | Форма отчёта об ошибке, метка `bug`. | Меняется набор полей. |
| `ISSUE_TEMPLATE/feature_request.yml` | Форма предложения, метка `enhancement`. | Меняется набор полей. |
| `ISSUE_TEMPLATE/config.yml` | Запрещает пустые issue, уводит вопросы в Discussions, а уязвимости — в приватную форму advisory. | Сменились ссылки. |
| `workflows/ci.yml` | CI: сборка Desktop-проекта и четыре тестовых проекта на ubuntu, windows, macos. | Добавлен тестовый проект — дописать шагом. |

## `.vscode/`

Только для VS Code. `launch.json` и `tasks.json` берут имена из `settings.json` через `${config:...}`; согласованность проверяет `VsCodeConfigConsistencyTests`.

| Файл | Зачем нужен | Когда сюда лезть |
|---|---|---|
| `settings.json` | `app.name`, аргументы сборки, скрытие `bin` и `obj`. | Переименование приложения — правится только здесь. |
| `tasks.json` | Задачи `build-app` и `build-app-release`; на macOS собирается `.MacOS`. | Меняются конфигурации или аргументы сборки. |
| `launch.json` | Запуск Debug и Release; на macOS запускается `.app`. | Меняются целевая платформа или пути. |

## Deployment

Windows — WiX (`Setup/` собирает `.msi`, `Bundle/` заворачивает его в `.exe` с рантаймом), Linux — PupNet, macOS — без отдельной конфигурации. Рецепты сборки — [Publish.md](Publish.md).

### `Setup/` — установщик `.msi`

| Файл | Зачем нужен | Когда сюда лезть |
|---|---|---|
| `Setup.wixproj` | Проект WiX 7: в PreBuild генерирует `LICENSE.rtf` и публикует Desktop-проект. Выход — `Publish/DataOrganizer-<версия>-win-x64.msi`. | Меняются платформа публикации или шаги сборки. |
| `Package.wxs` | Корень пакета: диалоги `WixUI_Advanced`, иконка в «Программах и компонентах», показ лицензии, обязательная функция установки. | Меняются интерфейс установщика или метаданные пакета. |
| `Components.wxs` | Что ставится: `DataOrganizer.exe`, остальное содержимое `Publish/**`, ярлыки в меню «Пуск» и на рабочем столе. | Меняются ярлыки или аргументы запуска. |
| `Folders.wxs` | Каталоги установки: `APPLICATIONFOLDER` и папка в меню «Пуск». | Почти никогда. |
| `Predefines.wxi` | Переменные препроцессора: имена, суффиксы `-Debug` и `-64bit`, `ProductComponentGUID`, `UpgradeCode`. | `UpgradeCode` после первой установки менять нельзя — обновление поставит вторую копию. |
| `IssueWorkaround.wxi` | Обход дефекта `WixUI_Advanced`: 64-битная программа ставилась в `Program Files (x86)`. | Не трогать, пока дефект не исправят. |
| `LICENSE.rtf` | Лицензия для диалога установщика; генерируется из `LICENSE`, в репозиторий не попадает. | Не править — правится `LICENSE`. |

### `Bundle/` — установщик вместе с рантаймом

| Файл | Зачем нужен | Когда сюда лезть |
|---|---|---|
| `Bundle.wixproj` | Burn-бандл: сам собирает `Setup.wixproj`, затем удаляет промежуточный `.msi` из `Publish/`. Версия рантайма — `DotNetVersion`. | Переход на новую версию .NET. |
| `Bundle.wxs` | Цепочка: поиск .NET 10 x64 → установка рантайма при необходимости → вложенный `.msi`. | Меняется состав цепочки. |
| `Localizations/HyperlinkTheme.en-us.wxl` | Строки интерфейса бандла на английском. | Правка текстов установщика. |
| `Localizations/HyperlinkTheme.ru-ru.wxl` | Строки на русском; выбор файла — по `SetupCulture`. | Правка текстов установщика. |
| `Themes/HyperlinkTheme.xml` | Разметка окна бандла (тема `hyperlinkLicense`). | Меняются размеры окна или набор элементов. |
| `Resources/dotnet-runtime-10.0.11-win-x64.exe` | Установщик рантайма (~30 МБ) внутри бандла — установка работает без интернета. | Новая версия .NET: скачать файл, обновить ссылку в `Bundle.wxs` и `DotNetVersion`, старый удалить. |

### PupNet — пакеты для Linux

| Файл | Зачем нужен | Когда сюда лезть |
|---|---|---|
| `app.pupnet.conf` | Конфигурация PupNet: идентификатор, описание, лицензия, иконки, аргументы `dotnet publish`, настройки `.deb`, `.rpm`, AppImage, Flatpak, zip. Версия передаётся через `--app-version`. | Меняются зависимости пакетов или аргументы публикации. |
| `app.metainfo.xml` | AppStream-метаданные для центров приложений; большую часть полей подставляет PupNet. | Руками — категории, ключевые слова, рейтинг, скриншоты. |

## `Docs/`

| Файл | Зачем нужен | Когда сюда лезть |
|---|---|---|
| [`Encryption_Format.md`](Encryption_Format.md) | Разметка зашифрованных блобов и правило «новая разметка — новый байт версии». | Правка стека шифрования. |
| [`GitHub_Release.md`](GitHub_Release.md) | Чек-лист выпуска релиза на GitHub. | Выпуск релиза. |
| [`Publish.md`](Publish.md) | Рецепты сборки дистрибутивов для Windows, Linux, macOS. | Сборка дистрибутива. |
| [`release-notes.template.md`](release-notes.template.md) | Шаблон описания релиза с подстановкой `{version}`. | Изменился набор артефактов релиза. |
| `Solution_Files.md` | Этот файл. | В решении появился или исчез не-кодовый файл. |
| `Images/*.png` | Скриншоты для `README.md`. | Заметно изменился интерфейс. |
| [`PupNet_Instructions/1_Setup.md`](PupNet_Instructions/1_Setup.md) | Этап 1: окружение WSL с Ubuntu. | Настройка новой машины. |
| [`PupNet_Instructions/2_Config.md`](PupNet_Instructions/2_Config.md) | Этап 2: генерация и заполнение `app.pupnet.conf`. | Пересоздание конфигурации. |
| [`PupNet_Instructions/3_AppStream.md`](PupNet_Instructions/3_AppStream.md) | Этап 3: что в `app.metainfo.xml` заполняется руками. | Правка метаданных. |
| [`PupNet_Instructions/4_Build.md`](PupNet_Instructions/4_Build.md) | Этап 4: сборка `.deb`, `.rpm`, AppImage, Flatpak, zip и проверка пакета. | Сборка пакетов Linux. |

## `tools/`

PowerShell 5.1, запуск из корня репозитория.

| Файл | Зачем нужен | Когда сюда лезть |
|---|---|---|
| `gen-license-rtf.ps1` | `LICENSE` → `Setup/LICENSE.rtf`. | Запускается сам из PreBuild `Setup.wixproj`. |
| `gen-release-notes.ps1` | Подставляет версию в шаблон, пишет `Publish/release-notes.md`, копирует текст в буфер обмена. | Выпуск релиза. |
| `gen-third-party-notices.ps1` | Пересобирает `THIRD-PARTY-NOTICES.txt` из `project.assets.json`. | Перед релизом и после смены зависимостей; нужен свежий `dotnet restore`, в выводе не должно быть `UNKNOWN`. |

## Чего нет в репозитории

| Путь | Что это |
|---|---|
| `Publish/` | Установщики, архивы, текст описания релиза. |
| `bin/`, `obj/` | Промежуточные результаты сборки. |
| `.vs/` | Кэш Visual Studio. |
| `Setup/LICENSE.rtf` | Создаётся при сборке `Setup.wixproj`. |

## Кто кого читает

- `Directory.Build.props` → проекты, `Setup.wixproj` и `Bundle.wixproj`, `gen-release-notes.ps1`, PupNet (`--app-version`)
- `LICENSE` → `gen-license-rtf.ps1` → `Setup/LICENSE.rtf` → диалог лицензии в `.msi`
- `project.assets.json` → `gen-third-party-notices.ps1` → `THIRD-PARTY-NOTICES.txt`
- `Docs/release-notes.template.md` → `gen-release-notes.ps1` → `Publish/release-notes.md`
- `.vscode/settings.json` → `.vscode/launch.json`, `.vscode/tasks.json`
- `Setup.wixproj` → `Bundle.wixproj` (бандл сам собирает вложенный `.msi`)
