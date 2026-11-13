# Debugging tests in Visual Studio

## The symptom

`Debug All Tests` (Ctrl+R, Ctrl+A) stops on about thirty exceptions, while a plain run is clean. Every
one of them is thrown on purpose: the suite asserts a wrong password, damaged data, a refused write, a
locked keeper.

## Why it happens

Two independent debugger rules fire, and only one of them is obvious.

- **Break When Thrown** — the checkboxes in Exception Settings.
- **The user-unhandled rule of Just My Code** — it fires even with every checkbox cleared, whenever an
  exception leaves our code into someone else's. `.Should().Throw<T>()` and `Assert.Throws` do exactly
  that: the throw is caught inside NUnit or AwesomeAssertions, and that is not our code.

Clearing the checkboxes alone therefore changes nothing, which is what makes this confusing.

## The fix

Both steps are needed:

1. Press Ctrl+Alt+E and clear the root checkbox of **Common Language Runtime Exceptions**.
2. Right-click the same node and check **Continue When Unhandled in User Code**.

The second step can also be done coarsely, through Tools > Options > Debugging > **Enable Just My
Code** turned off — but then the debugger steps into library code and the call stack fills with frames
nobody asked for.

## What it does not change

Nothing outside the debugger. `Run All Tests`, `dotnet test` and CI attach no debugger, so for them the
settings do not exist. No exception is swallowed and no `catch` is skipped; only the pause is gone.

The price is real but narrow: the second setting also silences exceptions that were **not** expected,
when a library catches them. While chasing an obscure failure, put it back for the length of the
search.

## Why this is not in the repository

The checkboxes live in the binary per-user `.suo` inside `.vs`, and Just My Code is a global setting of
the IDE. `.editorconfig`, `.runsettings` and `Directory.Build.props` reach none of it, and an exported
`.vssettings` carries only the Just My Code half.

So the settings are lost from time to time — cleaning `.vs`, updating the IDE or opening the solution
from another directory (a fresh clone, a worktree) wipes them. Nothing is broken then; the two steps
simply have to be repeated.

## Calls to Debugger.Break

These settings do not govern `Debugger.Break()`. Such a call stops the debugger whatever is configured,
so they are guarded by `IsRunningFromNUnit()` — as in `SerilogExtensions` and `EntityLoader`. If the
debugger still stops somewhere after both steps, look for an unguarded one.
