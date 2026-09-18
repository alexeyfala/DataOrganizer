# Mutation testing (Stryker)

## What it is for

A green test run says the code passes its tests. It does not say the tests would notice if the code
broke. Stryker checks that: it changes the code in small ways — `>` becomes `>=`, a statement is
dropped, a `return` gives back something else — then rebuilds and runs the tests that cover the changed
line.

- The tests fail — the mutant is **killed**: that behaviour is pinned by the suite.
- The tests stay green — the mutant **survived**: nothing in the suite depends on that line being right.

A survivor is a candidate for a new test, not a defect by itself.

## Setup

`dotnet-stryker` is a local tool. After cloning the repository, from the solution root:

    dotnet tool restore

The settings are in `stryker-config.json` at the solution root: which project is mutated, which test
projects have to catch the mutations, which files are taken in and what is left out.

## Running it

The default run — the `Repository` project against both of its test projects:

    dotnet dotnet-stryker

Another scope is one command line away; the arguments win over the config file:

    dotnet dotnet-stryker -p Shared.csproj -tp Shared.UnitTests/Shared.UnitTests.csproj -m "**/Extensions/**/*.cs"

A run takes one to three minutes and changes nothing in the repository.

**The application project cannot be mutated.** Stryker compiles the mutants itself and does not run
source generators, while `InitializeComponent` of Avalonia comes from one — the mutated `DataOrganizer`
project fails to build before the first mutant is tested. Only `Shared`, `Repository` and `Entities`
can be run.

## Reading the report

Reports land in `StrykerOutput/<date>/reports/` (kept out of git): `mutation-report.html` to read,
`mutation-report.json` if something has to be parsed. The html shows the source with every mutant on
its own line.

Statuses, in the order they are worth looking at:

- **Survived** — the tests did not notice the change. Start here.
- **NoCoverage** — no test runs that line at all. Not a weak test but a missing one.
- **Killed**, **Timeout** — the suite noticed; a timeout usually means the change made a loop endless.
- **Ignored** — filtered out by the config.
- **CompileError** — the mutant did not build; nothing to do with it.

The percentage at the end is not a target. What pays off is walking the survivors once and deciding for
each whether a test is worth writing.

## What is noise

Not every survivor can be killed:

- **Equivalent mutants** — the change cannot alter the result. `digits >= count` and `digits > count`
  behave the same when the equal case computes the same value in both branches.
- **What we do not test on purpose** — wording of messages, the fact that something was logged,
  diagnostic dumps of object properties.
- **`ConfigureAwait(false)` turned into `(true)`** — invisible to any test. It is filtered in the config
  together with logging calls and string literals; before the filter it was two thirds of all survivors.

## Keeping the suite fast

The whole suite runs in about six seconds, and `dotnet test` prints the time per assembly — the cheapest
check that nothing has slowed down:

    dotnet test DataOrganizerApp.slnx --no-build --nologo

Today: `DataOrganizer.UnitTests` about 4 s, `Repository.UnitTests` about 1 s, the rest under half a
second. For per-test times run with the trx logger and open the file in Visual Studio, where Test
Explorer sorts by duration:

    dotnet test DataOrganizerApp.slnx --no-build --logger trx
