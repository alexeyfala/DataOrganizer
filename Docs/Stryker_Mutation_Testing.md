# Mutation testing (Stryker)

## What it is for

A green test run says the code passes its tests. It does not say the tests would notice if the code
broke. Stryker checks that: it plants small changes in the code — `>` becomes `>=`, a statement is
dropped, a `return` gives back something else — builds them all into one copy of the project and
switches them on one at a time while it runs the tests that cover the changed line.

- The tests fail — the mutant is **killed**: that behaviour is pinned by the suite.
- The tests stay green — the mutant **survived**: nothing in the suite depends on that line being right.

A survivor is a candidate for a new test, not a defect by itself.

## Setup

`dotnet-stryker` is a local tool. After cloning the repository, from the solution root:

    dotnet tool restore

`stryker-config.json` at the solution root holds what every run shares: the calls left out as
unobservable and the reports to write. Which project is mutated and which tests have to catch the
mutations is decided by `tools/run-stryker.ps1`.

## Running it

From the solution root, every project at once:

    powershell -ExecutionPolicy Bypass -File tools/run-stryker.ps1

Some projects only, by their folder names, separated by commas without spaces:

    powershell -ExecutionPolicy Bypass -File tools/run-stryker.ps1 -Project Hierarchy,TextProcessing

The script pairs every project `X/X.csproj` with `X.UnitTests` and `X.IntegrationTests`, whichever
exist, so a renamed or a new project is picked up as long as its test project carries the same name.
A project without a test project of its own is skipped, and so is a project with Avalonia views (see
below).

Only the project's own tests catch its mutants. The score then says how well the tests of the project
pin its behaviour, not what the whole suite happens to notice. That is why the script starts Stryker
from the folder of the project: from the solution root Stryker finds `DataOrganizerApp.slnx`, switches
to solution mode and takes every test project that depends on the project, whatever `-tp` says. For
`Hierarchy` that is 3546 tests instead of its own 58, and five minutes instead of twenty seconds.

Stryker runs as many test sessions as half of the logical processors. `-Concurrency N` changes that;
more sessions than processors do not make the run faster (see *Timeout* below).

A run of every project takes about 22 minutes on 32 logical processors and changes nothing in the
repository.

At the end the script prints a table, one line per project:

- **Mutants** — every mutant Stryker planted, the ignored ones included.
- **Killed**, **Survived**, **NoCoverage**, **Timeout**, **CompileError** — see *Reading the report*.
- **Score** — the score of Stryker: the killed and timed-out mutants against all that were tested or
  left without coverage.
- **Without flips** — the same score without the flips of `ConfigureAwait` (see *What is noise*). This
  is the number to compare between runs.

Below the table come the methods Stryker dropped in safe mode (see *Methods Stryker drops*), and the
path of the report of all projects.

## Projects Stryker cannot run

**A project with Avalonia views cannot be mutated.** Stryker compiles the mutants itself. It runs the
source generators — CommunityToolkit.Mvvm, `[GeneratedRegex]`, `[LibraryImport]` and Generator.Equals
all work in the layer projects — but the Avalonia generator finds the `.axaml` files by metadata of
each file, which Stryker does not pass on. It writes no `InitializeComponent`, and the mutated
`DataOrganizer` fails to build with CS0103 before the first mutant is tested.

## Methods Stryker drops

When a mutant breaks the build, Stryker finds that mutant and drops it. Some errors cannot be pinned on
one mutant, most often CS0165, a local variable that may be unassigned. It comes from a variable
declared in a condition, such as `if (… is not { } files)` or `while (reader.TryRead(out var item))`:
a mutant that turns the condition around breaks the rule that the variable is assigned, and the
compiler reports the line that uses the variable, far from the mutant. Stryker then drops every mutant
of that method and says so:

    Safe Mode! Stryker will remove all mutations in <Method> and mark them as 'compile error'.

These mutants count as CompileError and stay out of the score, so the percentage does not show that the
method went unchecked. The script lists such methods after the table. Which methods fall out depends on
the whole set of mutants, so take the list from the latest run.

A dropped method is checked by hand. It costs about half an hour and needs no tooling:

    copy the file aside, change one line, run the tests, put the file back, compare it with the copy

The comparison at the end is not a formality. It is the only proof that nothing of the experiment
stayed behind.

## Reading the report

The script joins the reports of all projects in `StrykerOutput/<time>/mutation-report.html` (kept out
of git): every project is a folder of one tree, with its score next to it, and the html shows the source
with every mutant on its own line. In this report the flips of `ConfigureAwait` are Ignored, with the
reason given, so its scores are the *Without flips* column. `mutation-report.json` next to it is the
same report for parsing. The reports Stryker writes for each project lie in
`StrykerOutput/<time>/<Project>/reports/`, with the flips as they are.

Statuses, in the order they are worth looking at:

- **Survived** — the tests did not notice the change. Start here.
- **NoCoverage** — no test runs that line at all. Not a weak test but a missing one.
- **Killed** — the suite noticed.
- **Timeout** — the tests did not finish in time, which Stryker counts as noticed. Usually the change
  made a loop endless or left a task waiting forever. Too many test sessions for the processor give
  timeouts too: a mutant that times out only with a high `-Concurrency` is a false one.
- **Ignored** — left out by the config, or skipped because a mutant that removes the whole block
  already covers it.
- **CompileError** — the mutant did not build, or its whole method was dropped (see *Methods Stryker
  drops*).

The percentage is not a target. What pays off is walking the survivors once and deciding for each
whether a test is worth writing.

For `Repository` Stryker also takes `TestSupport` for a test project and prints a test discovery error
about it. `TestSupport` has no tests, and the run is not affected.

## What is noise

Not every survivor can be killed:

- **Equivalent mutants** — the change cannot alter the result. `digits >= count` and `digits > count`
  behave the same when the equal case computes the same value in both branches.
- **What we do not test on purpose** — wording of messages, the fact that something was logged,
  diagnostic dumps of object properties. The config leaves out the calls on `_logger` and the calls of
  the private methods that only write to the log, named exactly. A new method of that kind shows up as
  a survivor: add its name to `ignore-methods`. A pattern such as `*Log*` is not used, as it also takes
  in calls like `Math.Log2` or `LogCallback`.
- **`ConfigureAwait(false)` turned into `(true)`** — invisible to any test. It is not left out in the
  config: `ignore-methods` drops the whole call chain that ends in `ConfigureAwait`, so the removal of
  an awaited call and every mutant in its arguments would vanish with the flip. The flips stay in the
  report as survivors — Boolean mutations on `ConfigureAwait` — and pull the score of Stryker down; the
  *Without flips* column leaves them out.

A left-out call takes its arguments with it, so `ignore-methods` never names a call whose arguments do
real work.

String literals are mutated too: in the layer projects a literal is often logic — a line break, a
marker, a language id. A literal that is only wording, such as the message of an exception, survives
and is noise.

## Keeping the suite fast

Stryker runs the tests that cover a mutant for each mutant, so a slow test makes a slow run. `dotnet test`
prints the time per assembly — the cheapest check that nothing has slowed down:

    dotnet test DataOrganizerApp.slnx --no-build --nologo

Today `DataOrganizer.UnitTests` takes about 48 s, and as the assemblies run in parallel, that is the
time of the whole suite; every other test project takes 2 s or less. For per-test times run with the trx logger and open the file in Visual Studio,
where Test Explorer sorts by duration:

    dotnet test DataOrganizerApp.slnx --no-build --logger trx
