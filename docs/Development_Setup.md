# Development Setup

What a Linux or macOS machine needs to build and run the app from source, F5 in VS Code included. Set it up
once per machine and again after each move to a new .NET.

## Linux

Install the SDK that `global.json` of the repository names, with Microsoft's install script: a distro's feed can
lag behind a new .NET. The script installs no dependencies; those of each distro, ICU among them, are listed on
[Install .NET on Linux](https://learn.microsoft.com/dotnet/core/install/linux). `~/.dotnet` goes first on `PATH`,
as a .NET installed from the distro's packages (`/usr/bin/dotnet`) would win otherwise. Run from the repository
root, then verify (expect the SDK version of `global.json`):

```bash
curl -fsSL -o /tmp/dotnet-install.sh https://dot.net/v1/dotnet-install.sh && \
bash /tmp/dotnet-install.sh --jsonfile global.json && \
rm /tmp/dotnet-install.sh
echo 'export DOTNET_ROOT="$HOME/.dotnet"' >> ~/.bashrc
echo 'export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"' >> ~/.bashrc
source ~/.bashrc
dotnet --version
```

Then quit VS Code, all its windows, and start it again: it keeps the environment it was started with.

## macOS

Install the SDK that `global.json` of the repository names, with Microsoft's install script, and the `macos`
workload, which belongs to one SDK. `~/.dotnet` goes first on `PATH`, as a .NET installed from a `.pkg` sits in
`/usr/local/share/dotnet` and would win otherwise. The workload may ask for a newer Xcode; its error names the
version. On an Intel Mac, which cannot run that Xcode, see [MacOS_Intel_Build.md](MacOS_Intel_Build.md). Run from
the repository root, then verify (expect the SDK version of `global.json`):

```bash
curl -fsSL -o /tmp/dotnet-install.sh https://dot.net/v1/dotnet-install.sh && \
bash /tmp/dotnet-install.sh --jsonfile global.json && \
rm /tmp/dotnet-install.sh
echo 'export DOTNET_ROOT="$HOME/.dotnet"' >> ~/.zshrc
echo 'export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"' >> ~/.zshrc
source ~/.zshrc
dotnet workload install macos
dotnet --version
```

Then quit VS Code with Cmd+Q and start it again: it keeps the environment it was started with.
