# Building the macOS app on an Intel Mac

.NET for macOS asks for the Xcode that was released with it, and an Intel Mac can no longer run that Xcode. This
page describes a workaround that builds and starts the macOS app with the older Xcode the Mac still runs.
Microsoft does not support it: use it to build and test on such a Mac, not as the way to make releases.

## The symptom

The build of the macOS project stops with:

    This version of .NET for macOS (27.2.11039) requires Xcode 27.2. The current version of Xcode is 26.6.

With that check switched off, the build stops at the next step with error MT0179: this version of
Microsoft.macOS requires the macOS 27.2 SDK, shipped with Xcode 27.2.

## Why it happens

- Each release of .NET for macOS requires one version of Xcode. .NET 11 (workload 27.2.11039) requires
  Xcode 27.2.
- Xcode 27 installs and runs only on Apple silicon, so an Intel Mac stays on Xcode 26.
- Past the version check, the default registrar of .NET 11, `trimmable-static`, generates native code
  against the headers of the macOS SDK that the workload was made for, and only Xcode 27.2 brings them.

## The workaround

Two MSBuild properties get the build through:

- `ValidateXcodeVersion=false` skips the check of the Xcode version.
- `Registrar=dynamic` registers the Objective-C classes when the app starts instead of generating native
  code during the build, so no headers of the newer SDK are needed. The app starts a little slower.

1\. Select the Xcode the Mac runs, by its path in Applications, and check its version:

```bash
sudo xcode-select -s /Applications/Xcode.app
xcodebuild -version
```

2\. Build once from the folder of the macOS project and start the app it built:

```bash
cd src/DataOrganizer.MacOS
dotnet build -p:ValidateXcodeVersion=false -p:Registrar=dynamic
open bin/Debug/*/osx-x64/DataOrganizer.app
```

3\. For F5 in VS Code and for the recipes of [Publish.md](Publish.md), set both properties as environment
variables, which MSBuild reads as properties:

```bash
echo 'export ValidateXcodeVersion=false' >> ~/.zshrc
echo 'export Registrar=dynamic' >> ~/.zshrc
source ~/.zshrc
```

Then quit VS Code with Cmd+Q and start it again: it keeps the environment it was started with.

## What was checked

On an Intel Mac with macOS Tahoe 26.7.3, Xcode 26.6 and .NET for macOS 27.2.11039:

- the Debug build passes;
- the app starts from the built `.app` and through F5 in VS Code.

Not checked: the Release build and the packages of [Publish.md](Publish.md) (`.dmg`, `.pkg`, `.zip`). A
later release of .NET for macOS may need more of the newer SDK, and the workaround may stop working.

## Undoing it

The environment variables act on every .NET for macOS project on the Mac. Remove both lines from
`~/.zshrc` once the app is built on a Mac that runs the Xcode the workload asks for, then quit and start
VS Code again.

## References

- [.NET 11 release notes of .NET for iOS, tvOS, Mac Catalyst and macOS](https://github.com/dotnet/macios/wiki/.NET-11-release-notes)
- [The Xcode requirement of .NET for iOS and macOS](https://learn.microsoft.com/dotnet/ios/troubleshooting/xcode-requirement)
- [Xcode 27 for Intel Macs, Apple Developer Forums](https://developer.apple.com/forums/thread/829619)
