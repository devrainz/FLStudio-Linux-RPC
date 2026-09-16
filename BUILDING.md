# Building FL Studio Linux RPC from Source

Official native packages are the recommended way to install FL Studio Linux RPC:

- Debian / Ubuntu / Linux Mint: `.deb`
- Fedora / RHEL-family distributions: `.rpm`
- Arch Linux / CachyOS / EndeavourOS: `.pkg.tar.zst`

If none of those packages fit your distribution, you need a custom build, or you want to work on the project itself, you can compile it directly from source.

## Requirements

You will need:

- .NET 8 SDK
- GNU Make
- Git
- The native GTK/libadwaita libraries required by the application

Package names differ between distributions.

Official pre-built releases currently target **Linux x86-64**. Other architectures may be buildable with a different .NET runtime identifier, but they are not officially tested unless stated otherwise.

## Clone the Repository

```bash
git clone https://github.com/devrainz/FLStudio-Linux-RPC.git
cd FLStudio-Linux-RPC
```

## Build

Build the project:

```bash
make
```

or:

```bash
make build
```

During development you can also run the application directly through the .NET CLI:

```bash
dotnet run
```

## Create a Self-Contained Build

For the same general style of self-contained build used by the official Linux releases:

```bash
make publish
```

By default this uses:

```text
RID=linux-x64
CONFIG=Release
```

The resulting executable will be placed at:

```text
build/publish/linux-x64/FLStudioRPC
```

Run it directly:

```bash
./build/publish/linux-x64/FLStudioRPC
```

## Build for Another Runtime

You can override the .NET runtime identifier:

```bash
make publish RID=linux-arm64
```

This does not automatically mean the target is officially supported. Native libraries and desktop integration still need to exist for that architecture and distribution.

## Build Native Packages

Maintainers can build all officially supported native package formats with:

```bash
make package VERSION=1.3.0
```

This calls:

```text
packaging/build-packages.sh
```

and produces the Debian, RPM, Arch Linux packages and checksum file under `dist/`.

The same packaging script is used by GitHub Actions, which helps keep local and release builds consistent.

## Useful Targets

```text
make
make help
make restore
make build
make publish
make package VERSION=X.Y.Z
make clean
```

## Clean Generated Files

```bash
make clean
```

This removes the local `build/` and `dist/` output directories and runs `dotnet clean`.

## Installation After a Manual Source Build

A manual source build is intentionally not the same as installing a native package.

If your distribution supports one of the official packages, prefer that package so the distro package manager can manage:

- the executable
- the application-menu entry
- icons
- upgrades
- uninstallation

For unsupported distributions, the self-contained executable can be run directly from the publish directory or integrated manually according to that distribution's conventions.
