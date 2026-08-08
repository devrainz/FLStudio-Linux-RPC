<h1 align="center">
  <br>
  <a href="https://github.com/devrainz/FLStudio-Linux-RPC"><img src="https://raw.githubusercontent.com/devrainz/FLStudio-Linux-RPC/refs/heads/main/Icons/hicolor/128x128/apps/flstudio.png" alt="FL Studio Discord RPC" width="200"></a>
  <br>
  FL Studio Linux RPC
  <br>
</h1>

<h4 align="center">A Linux port of FL Studio Discord RPC for showing your FL Studio projects and activity on Discord.</h4>

<p align="center">
  <img alt="Platform" src="https://img.shields.io/badge/platform-Linux-success">
  <img alt="Architecture" src="https://img.shields.io/badge/architecture-x86--64-blue">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-8.0-512BD4">
  <img alt="Runtime" src="https://img.shields.io/badge/runtime-self--contained-brightgreen">
  <img alt="Status" src="https://img.shields.io/badge/status-v1.0.0-brightgreen">
  <img alt="License" src="https://img.shields.io/badge/license-MIT-green">
</p>

<p align="center">
  <a href="#key-features">Key Features</a> •
  <a href="#requirements">Requirements</a> •
  <a href="#installation">Installation</a> •
  <a href="#usage">Usage</a> •
  <a href="#configuration">Configuration</a> •
  <a href="#building-from-source">Building From Source</a> •
  <a href="#uninstallation">Uninstallation</a> •
  <a href="#packages-used">Packages Used</a> •
  <a href="#feedback">Feedback</a> •
  <a href="#about-this-fork">About This Fork</a> •
  <a href="#license">License</a>
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/devrainz/FLStudio-Linux-RPC/refs/heads/main/assets/preview.png" alt="RPC Preview">
</p>

## Key Features

* **Linux Native** - Designed specifically for Linux instead of relying on Windows APIs.
* **Wine/Proton FL Studio Detection** - Detects FL Studio running through Wine or Proton.
* **Discord Rich Presence** - Displays your current FL Studio activity on Discord.
* **System Tray Integration** - Runs quietly in the background with a tray icon.
* **Secret Mode** - Hide your FL Studio project name from your Discord activity.
* **Conditional RPC** - Discord activity is only displayed while FL Studio is running.
* **Single Instance** - Prevents multiple copies of FL Studio Discord RPC from running simultaneously.
* **Accurate FL Studio Version Detection** - Displays the detected FL Studio version when available.
* **JSON Configuration** - Settings are stored in an easy-to-manage configuration file.
* **Lightweight** - Runs in the background with minimal resource usage.
* **Self-Contained Releases** - Official releases include everything required to run the application without installing the .NET runtime separately.

## Requirements

### Supported Systems

Currently supported:

* **Linux x86-64**
* **FL Studio running through Wine, Proton, etc.**
* **Discord**

The application is currently distributed as a Linux x86-64 build.

### Runtime

Official releases are published as **self-contained .NET 8 applications**, so installing the .NET runtime separately is not required.

For building from source, you will need:

* .NET 8 SDK
* A Linux x86-64 system
* Git

## Installation

### From a Release

Download the latest Linux release from the [Releases](https://github.com/devrainz/FLStudio-Linux-RPC/releases) page.

Download the archive:

```text
FLStudioRPC-linux-x64-vX.X.X.tar.gz
```

Extract it:

```bash
tar -xzf FLStudioRPC-linux-x64-vX.X.X.tar.gz
cd FLStudioRPC-linux-x64
```

Run the installer:

```bash
sudo ./setup.sh
```

The installer will:

1. Install the application to `/opt/flstudio-rpc`
2. Install the application icon
3. Create a desktop entry
4. Update the desktop application database when available

After installation, **FL Studio Discord RPC** will appear in your desktop environment's application menu.

You can launch it from there, or run:

```bash
/opt/flstudio-rpc/FLStudioRPC
```

## Usage

Once started, FL Studio Discord RPC runs in the background and monitors your system for FL Studio.

When FL Studio is detected, the application updates your Discord Rich Presence with information about the current FL Studio session.

When FL Studio is closed, the Discord activity is cleared automatically.

### System Tray

The application provides a system tray icon when supported by your desktop environment.

Right-click the tray icon to access the available options, including:

* **Secret Mode** - Hide your current project name
* **About** - Open information about the application
* **Exit** - Close FL Studio Discord RPC

## Configuration

Configuration is stored as JSON and can be customized without modifying the application itself.

The configuration controls application behavior such as Discord Rich Presence settings and privacy options.

> **Note**
> The exact configuration location and available options may change as the Linux port develops. Check the current configuration implementation in `Config/Config.cs` if you are building from source.

## Uninstallation

If you installed the application using `setup.sh`, use the included uninstaller.

From the extracted release directory:

```bash
sudo ./uninstall.sh
```

The uninstaller removes:

* `/opt/flstudio-rpc`
* `/usr/share/applications/flstudiorpc.desktop`
* Installed application resources and icons

The desktop application database is also updated when the required utility is available.

## Building From Source

This project uses **.NET 8** and the modern SDK-style project format.

### Prerequisites

Install the .NET 8 SDK for your Linux distribution.

Then clone the repository:

```bash
git clone https://github.com/devrainz/FLStudio-Linux-RPC.git
cd FLStudio-Linux-RPC
```

Restore the project dependencies:

```bash
dotnet restore
```

Build the project:

```bash
dotnet build
```

Run it:

```bash
dotnet run
```

### Publishing a Linux x86-64 Build

To create a self-contained Linux x86-64 build similar to the official releases:

```bash
dotnet publish FLStudioRPC.csproj \
  --configuration Release \
  --runtime linux-x64 \
  --self-contained true \
  --output publish/linux-x64 \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true
```

The resulting executable will be:

```text
publish/linux-x64/FLStudioRPC
```

### Creating a Release Package

Official releases contain:

```text
FLStudioRPC-linux-x64/
├── FLStudioRPC
├── setup.sh
├── uninstall.sh
└── Icons/
    └── hicolor/
        ├── 128x128/
        │   └── apps/
        │       └── flstudio.png
        └── index.theme
```

The GitHub Actions release workflow automatically builds and packages the application.

Releases can be created through:

**GitHub → Actions → Create Linux Release → Run workflow**

The workflow supports:

* Patch releases
* Minor releases
* Major releases
* Custom release notes

It automatically creates a versioned `.tar.gz` archive and publishes it as a GitHub Release.

## Project Structure

```text
FLStudio-Linux-RPC/
├── Config/
│   └── Config.cs
├── Events/
│   └── Events.cs
├── Icons/
│   └── hicolor/
├── Memory/
│   └── Utils.cs
├── Tray/
│   └── LinuxTray.cs
├── installer/
│   ├── setup.sh
│   └── uninstall.sh
├── .github/
│   └── workflows/
│       └── release.yml
├── FLStudioRPC.csproj
├── Program.cs
└── README.md
```

## Packages Used

[DiscordRichPresence](https://github.com/Lachee/discord-rpc-csharp) - Discord Rich Presence library.

[Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) - JSON serialization and configuration handling.

[Colorful.Console](https://github.com/tomakita/Colorful.Console) - Console output formatting.

[Tmds.DBus](https://github.com/tmds/Tmds.DBus) - D-Bus integration for Linux desktop functionality.

[Tmds.DBus.Protocol](https://github.com/tmds/Tmds.DBus.Protocol) - D-Bus protocol support.

[SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp) - Image processing used by the Linux application and tray integration.

## Feedback

If you encounter a bug, have a feature request, or have an issue with FL Studio running through Wine or Proton, please open an issue on the [GitHub repository](https://github.com/devrainz/FLStudio-Linux-RPC/issues).

Feedback and contributions are welcome.

## About This Fork

**FL Studio Discord RPC for Linux** is a Linux-focused fork and substantial port of the original [FL Studio Discord RPC](https://github.com/zfi2/FL-Studio-Discord-RPC) by [@zfi2](https://github.com/zfi2).

The original project provided the foundation for this Linux version, and portions of its original code and overall functionality were retained where appropriate. However, the original application was heavily dependent on **Windows-specific APIs and technologies**, making a direct Linux port impractical.

Because of this, a significant portion of the application had to be **rewritten and replaced with Linux-native implementations**. Windows-specific functionality was removed and replaced with Linux-compatible approaches for things such as:

* FL Studio process and Wine/Proton detection
* System tray integration
* Application and desktop integration
* Configuration and application behavior
* Single-instance handling
* Linux application icons and desktop entries
* Installation and uninstallation
* Other functionality that previously depended on Windows APIs

The goal of this project is therefore not simply to run the original Windows application on Linux, but to provide a **proper Linux-native implementation** while preserving the core idea and functionality of the original project.

### Credits

Huge thanks to **[@zfi2](https://github.com/zfi2)** for creating the original FL Studio Discord RPC and providing the foundation that made this project possible.

**Original project:**
https://github.com/zfi2/FL-Studio-Discord-RPC

This project is independently maintained and focused exclusively on Linux.

## License

This project is licensed under the [MIT License](https://opensource.org/license/mit/) - see the [LICENSE](LICENSE) file for details.

---

> GitHub [@devrainz](https://github.com/devrainz)
