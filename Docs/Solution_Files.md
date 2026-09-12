# Справочник по не-кодовым файлам решения

Порядок и вложенность — как в обозревателе решений Visual Studio.

- 📁 **`Deployment/`** — упаковка приложения; рецепты сборки — [Publish.md](Publish.md).
  - 📁 **`PupNet/`** — упаковка для Linux; оба файла лежат в корне репозитория.
    - ⚙️ [`app.metainfo.xml`](../app.metainfo.xml) — AppStream-метаданные для центров приложений; большую часть полей подставляет PupNet. **Когда:** руками заполняются категории, ключевые слова, рейтинг, скриншоты.
    - ⚙️ [`app.pupnet.conf`](../app.pupnet.conf) — конфигурация PupNet: идентификатор, описание, лицензия, иконки, аргументы `dotnet publish`, настройки `.deb`, `.rpm`, AppImage, Flatpak, zip. Версия передаётся через `--app-version`. **Когда:** меняются зависимости пакетов или аргументы публикации.
- 📁 **`Docs/`**
  - 📁 **`Database/`**
    - 📄 [`Migrations.md`](Database/Migrations.md) — команды EF Core: создать и откатить миграцию, применить её к базе, сгенерировать SQL. **Когда:** изменилась модель данных.
  - 📁 **`Images/`**
    - 🖼️ `*.png` — скриншоты для `README.md`.
  - 📁 **`PupNet_Instructions/`**
    - 📄 [`1_Setup.md`](PupNet_Instructions/1_Setup.md) — этап 1: окружение WSL с Ubuntu.
    - 📄 [`2_Config.md`](PupNet_Instructions/2_Config.md) — этап 2: генерация и заполнение `app.pupnet.conf`.
    - 📄 [`3_AppStream.md`](PupNet_Instructions/3_AppStream.md) — этап 3: что в `app.metainfo.xml` заполняется руками.
    - 📄 [`4_Build.md`](PupNet_Instructions/4_Build.md) — этап 4: сборка `.deb`, `.rpm`, AppImage, Flatpak, zip и проверка пакета.
  - 📄 [`Encryption_Format.md`](Encryption_Format.md) — разметка зашифрованных блобов и правило «новая разметка — новый байт версии». **Когда:** правка стека шифрования.
  - 📄 [`GitHub_Release.md`](GitHub_Release.md) — чек-лист выпуска релиза на GitHub.
  - 📄 [`Publish.md`](Publish.md) — рецепты сборки дистрибутивов для Windows, Linux, macOS.
  - 📄 [`release-notes.template.md`](release-notes.template.md) — шаблон описания релиза с подстановкой `{version}`. **Когда:** изменился набор артефактов релиза.
  - 📄 [`Solution_Files.md`](Solution_Files.md) — этот файл. **Когда:** в решении появился или исчез не-кодовый файл.
- 📁 **`Solution Items/`**
  - 📁 **`.config/`**
    - ⚙️ [`dotnet-tools.json`](../.config/dotnet-tools.json) — локальные инструменты: `dotnet-ef`. Восстановление — `dotnet tool restore`. **Когда:** обновление EF Core, версия инструмента не ниже версии пакетов. Команды миграций — [Database/Migrations.md](Database/Migrations.md).
  - 📁 **`.github/`**
    - 📁 **`ISSUE_TEMPLATE/`**
      - ⚙️ [`bug_report.yml`](../.github/ISSUE_TEMPLATE/bug_report.yml) — форма отчёта об ошибке, метка `bug`.
      - ⚙️ [`config.yml`](../.github/ISSUE_TEMPLATE/config.yml) — запрещает пустые issue, уводит вопросы в Discussions, а уязвимости — в приватную форму advisory.
      - ⚙️ [`feature_request.yml`](../.github/ISSUE_TEMPLATE/feature_request.yml) — форма предложения, метка `enhancement`.
    - 📁 **`workflows/`**
      - ⚙️ [`ci.yml`](../.github/workflows/ci.yml) — сборка Desktop-проекта и четыре тестовых проекта на ubuntu, windows, macos. **Когда:** добавлен тестовый проект — дописать шагом.
    - ⚙️ [`dependabot.yml`](../.github/dependabot.yml) — еженедельные обновления NuGet и GitHub Actions, minor и patch одним pull request. **Когда:** обновлений приходит слишком много или мало.
    - 📄 [`PULL_REQUEST_TEMPLATE.md`](../.github/PULL_REQUEST_TEMPLATE.md) — заготовка описания pull request с чек-листом.
    - ⚙️ [`release.yml`](../.github/release.yml) — категории и метки для кнопки «Generate release notes»; метка `ignore-for-release` прячет pull request. **Когда:** изменился набор меток.
  - 📁 **`.vscode/`** — только для VS Code; `launch.json` и `tasks.json` берут имена из `settings.json` через `${config:...}`, согласованность проверяет `VsCodeConfigConsistencyTests`.
    - ⚙️ [`launch.json`](../.vscode/launch.json) — запуск Debug и Release; на macOS запускается `.app`. **Когда:** меняются целевая платформа или пути.
    - ⚙️ [`settings.json`](../.vscode/settings.json) — `app.name`, аргументы сборки, скрытие `bin` и `obj`. **Когда:** переименование приложения — правится только здесь.
    - ⚙️ [`tasks.json`](../.vscode/tasks.json) — задачи `build-app` и `build-app-release`; на macOS собирается `.MacOS`. **Когда:** меняются конфигурации или аргументы сборки.
  - 📁 **`Community/`** — виртуальная папка, файлы лежат в корне репозитория.
    - 📄 [`CODE_OF_CONDUCT.md`](../CODE_OF_CONDUCT.md) — Contributor Covenant. **Когда:** сменился контакт для жалоб.
    - 📄 [`CONTRIBUTING.md`](../CONTRIBUTING.md) — способы участия и требования к pull request. **Когда:** изменился процесс работы.
    - 📄 [`SECURITY.md`](../SECURITY.md) — политика безопасности: приватный канал, состав отчёта, границы модели угроз. **Когда:** изменились гарантии шифрования или поддерживаемые версии.
  - 📁 **`License/`** — виртуальная папка, файлы лежат в корне репозитория.
    - ⚖️ [`LICENSE`](../LICENSE) — Apache 2.0; единственная рукописная копия лицензии, не править.
    - ⚖️ [`NOTICE`](../NOTICE) — уведомление по Apache 2.0: копирайт, LGPL-компонент libuiohook, оговорка о названии продукта. **Когда:** сменился год копирайта или появился компонент с требованием уведомления.
    - ⚖️ [`THIRD-PARTY-NOTICES.txt`](../THIRD-PARTY-NOTICES.txt) — сторонние компоненты и их лицензии; генерируется `tools/gen-third-party-notices.ps1` из `project.assets.json`, руками не правится. **Когда:** перегенерировать перед релизом.
  - 📁 **`tools/`** — PowerShell 5.1, запуск из корня репозитория.
    - 💻 [`gen-license-rtf.ps1`](../tools/gen-license-rtf.ps1) — `LICENSE` → `Setup/LICENSE.rtf`; запускается сам из PreBuild проекта `Setup`.
    - 💻 [`gen-release-notes.ps1`](../tools/gen-release-notes.ps1) — подставляет версию в шаблон, пишет `Publish/release-notes.md`, копирует текст в буфер обмена. **Когда:** выпуск релиза.
    - 💻 [`gen-third-party-notices.ps1`](../tools/gen-third-party-notices.ps1) — пересобирает `THIRD-PARTY-NOTICES.txt` из `project.assets.json`. **Когда:** перед релизом и после смены зависимостей; нужен свежий `dotnet restore`, в выводе не должно быть `UNKNOWN`.
  - ⚙️ [`.editorconfig`](../.editorconfig) — стиль кода и правила именования; вместе с `EnforceCodeStyleInBuild` нарушения идут в вывод компилятора. Секция `[*.{csproj,wixproj,props,targets,wxs,wxi}]` держит табы в MSBuild и WiX. **Когда:** меняется соглашение.
  - ⚙️ [`.gitattributes`](../.gitattributes) — нормализация окончаний строк (`* text=auto`); остальное — закомментированный шаблон Visual Studio. **Когда:** почти никогда.
  - ⚙️ [`.gitignore`](../.gitignore) — что не попадает в репозиторий: `bin/`, `obj/`, `.vs/`, `Publish/` (готовые установщики и архивы), `Setup/LICENSE.rtf`. **Когда:** появился новый генерируемый артефакт.
  - ⚙️ [`Directory.Build.props`](../Directory.Build.props) — источник версии и имён: `AppVersion`, `AppName`, `AppDisplayName`, `Manufacturer`, `License`, иконка, имя MSI; плюс метаданные сборок. Читают все проекты, оба WiX-проекта, `gen-release-notes.ps1` и PupNet. **Когда:** перед релизом — поднять `AppVersion`.
  - 📄 [`README.md`](../README.md) — витрина на GitHub: возможности, скриншоты, требования, сборка, лицензия. **Когда:** изменились возможности или требования.

---

📁 папка · 📄 документация · ⚙️ конфигурация · 💻 скрипт · ⚖️ правовое · 🖼️ картинки
