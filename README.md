<h1 align="center">
  <br>
  <a href="https://github.com/devrainz/FLStudio-Linux-RPC">
    <img src="https://raw.githubusercontent.com/devrainz/FLStudio-Linux-RPC/refs/heads/main/Icons/hicolor/128x128/apps/flstudio.png" alt="FL Studio Discord RPC" width="200">
  </a>
  <br>
  FL Studio Linux RPC
  <br>
</h1>

<h4 align="center">
  A Linux port of FL Studio Discord RPC for showing your FL Studio projects and activity on Discord.
</h4>

<p align="center">
  <img alt="Platform" src="https://img.shields.io/badge/platform-Linux-success">
  <img alt="Architecture" src="https://img.shields.io/badge/architecture-x86--64-blue">
  <img alt=".NET" src="https://img.shields.io/badge/.NET-8.0-512BD4">
  <img alt="Runtime" src="https://img.shields.io/badge/runtime-self--contained-brightgreen">
  <img alt="Status" src="https://img.shields.io/badge/status-v1.0.2-brightgreen">
  <img alt="License" src="https://img.shields.io/badge/license-MIT-green">
</p>

<p align="center">
  <a href="#key-features">Key Features</a> •
  <a href="#requirements">Requirements</a> •
  <a href="#installation">Installation</a> •
  <a href="#usage">Usage</a> •
  <a href="#start-with-linux">Start with Linux</a> •
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
* **Wine/Proton FL Studio Detection** - Detects FL Studio running through Wine, Proton, and similar compatibility layers.
* **Discord Rich Presence** - Displays your current FL Studio activity on Discord.
* **System Tray Integration** - Runs quietly in the background with a tray icon.
* **Start with Linux** - Supports automatic startup through the standard Linux XDG Autostart system.
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
* **FL Studio running through Wine, Proton, or another compatible Wine environment**
* **Discord**

The application is currently distributed as a Linux x86-64 build.

### Runtime

Official releases are published as **self-contained .NET 8 applications**, so installing the .NET runtime separately is not required.

For building from source, you will need:

* .NET 8 SDK
* A Linux x86-64 system
* Git

### Desktop Integration

A desktop environment or compatible graphical session is recommended for:

* Application menu integration
* System tray support
* XDG autostart support

System tray behavior may vary depending on your desktop environment, panel, or status notifier implementation.

## Installation

Official releases are distributed as native Linux packages instead of a custom `setup.sh` installer.

Download the package for your distribution from the [Releases](https://github.com/devrainz/FLStudio-Linux-RPC/releases) page.

### Debian / Ubuntu / Linux Mint

```bash
sudo apt install ./flstudiorpc_X.X.X_amd64.deb
```

### Fedora / RHEL-family

```bash
sudo dnf install ./flstudiorpc-X.X.X-1.x86_64.rpm
```

### Arch Linux / CachyOS / EndeavourOS

```bash
sudo pacman -U ./flstudiorpc-X.X.X-1-x86_64.pkg.tar.zst
```

The package manager installs and owns:

* `/usr/bin/flstudiorpc`
* `/usr/share/applications/flstudiorpc.desktop`
* FL Studio RPC icons under `/usr/share/icons/hicolor/`
* The packaged license file

Required GTK4/libadwaita dependencies are declared by the package for each supported package format.

After installation, **FL Studio Discord RPC** should appear in your application menu. You can also launch it directly with:

```bash
flstudiorpc
```

### Migrating From the Old `setup.sh` Installer

Installing one of the new distro packages automatically removes the legacy `/opt/flstudio-rpc` application directory. Existing **Start with Linux** entries that still reference the old `/opt` executable are migrated by the application the next time it starts.

Older versions of `setup.sh` also copied the project's custom `Icons/hicolor/index.theme` over the distro-owned hicolor theme file. The new packages never own or replace that file. If package installation prints a warning about the legacy hicolor theme, restore it once with your distro package manager:

```bash
# Debian / Ubuntu / Linux Mint
sudo apt install --reinstall hicolor-icon-theme

# Fedora
sudo dnf reinstall hicolor-icon-theme

# Arch Linux / CachyOS / EndeavourOS
sudo pacman -S hicolor-icon-theme
```

## Usage

Once started, FL Studio Discord RPC runs in the background and monitors your system for FL Studio.

When FL Studio is detected, the application updates your Discord Rich Presence with information about the current FL Studio session.

When FL Studio is closed, the Discord activity is cleared automatically.

### System Tray

The application provides a system tray icon when supported by your desktop environment or panel.

Right-click the tray icon to access the available options, including:

* **Secret Mode** - Hide your current project name
* **Start with Linux** - Launch FL Studio Discord RPC automatically when your graphical session starts
* **About** - Open information about the application
* **Exit** - Close FL Studio Discord RPC

System tray support depends on your desktop environment or status notifier implementation.

## Start with Linux

The **Start with Linux** option uses the standard Linux **XDG Autostart** system.

When enabled, FL Studio Discord RPC creates an XDG `.desktop` autostart entry for the installed application.

The entry points to:

```text
/usr/bin/flstudiorpc
```

The autostart directory follows `XDG_CONFIG_HOME`.

If `XDG_CONFIG_HOME` is set, the entry is stored under:

```text
$XDG_CONFIG_HOME/autostart/
```

Otherwise, the default location is:

```text
~/.config/autostart/
```

### Desktop Environment Support

Most full Linux desktop environments process XDG autostart entries automatically.

This generally includes:

* GNOME
* Cinnamon
* KDE Plasma
* XFCE
* MATE
* LXQt

In these environments, enabling **Start with Linux** should normally be enough for FL Studio Discord RPC to launch automatically after login.

### Window Managers and Wayland Compositors

Standalone window managers and Wayland compositors may not process XDG autostart entries by themselves.

For example, a plain Hyprland session does not necessarily provide the same XDG autostart handling as a full desktop environment.

For Hyprland users, running the session through **UWSM** is recommended if you want standard XDG autostart applications to launch automatically.

For example, a UWSM-managed Hyprland session may be started using:

```bash
uwsm start hyprland.desktop
```

or through a display manager using the **Hyprland (uwsm-managed)** session when available.

> [!NOTE]
> Enabling **Start with Linux** creates and manages the XDG autostart entry.
>
> Your desktop environment or graphical session is responsible for actually processing that entry and launching the application when you log in.

If **Start with Linux** is enabled but FL Studio Discord RPC does not launch automatically, verify that your current graphical session supports XDG autostart.

## Configuration

Configuration is stored as JSON and can be customized without modifying the application itself.

The configuration controls application behavior such as Discord Rich Presence settings, privacy options, and other application preferences.

> [!NOTE]
> The exact configuration location and available options may change as the Linux port develops.
>
> If you are building from source, check the current configuration implementation in `Config/Config.cs`.

## Uninstallation

Remove the application through the same package manager used to install it.

### Debian / Ubuntu / Linux Mint

```bash
sudo apt remove flstudiorpc
```

### Fedora / RHEL-family

```bash
sudo dnf remove flstudiorpc
```

### Arch Linux / CachyOS / EndeavourOS

```bash
sudo pacman -Rns flstudiorpc
```

The package manager removes the installed executable, application-menu entry, icons, and other package-owned files automatically.

Per-user configuration and preferences under `~/.config` are intentionally preserved. If **Start with Linux** was enabled, its user-owned XDG autostart entry may remain, but it contains `TryExec=/usr/bin/flstudiorpc`, so desktop environments will ignore it once the package is removed. Disable **Start with Linux** before uninstalling if you also want that user file removed automatically by the application.

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

### Creating Distribution Packages

Packaging is handled by [nFPM](https://nfpm.goreleaser.com/) using `packaging/nfpm.yaml`. Both GitHub Actions workflows call the same `packaging/build-packages.sh` script, so local package builds and release builds use the same logic.

The **Build Linux Packages** workflow publishes three x86-64 package formats from the same self-contained .NET build:

```text
flstudiorpc_X.X.X_amd64.deb
flstudiorpc-X.X.X-1.x86_64.rpm
flstudiorpc-X.X.X-1-x86_64.pkg.tar.zst
SHA256SUMS
```

You can run it manually through:

**GitHub → Actions → Build Linux Packages → Run workflow**

The **Create Linux Release** workflow calculates the next semantic version, builds the same three packages, generates release notes, and publishes them in a GitHub Release.

No `setup.sh` or `uninstall.sh` is included. Installation, upgrades, file ownership, and removal are handled by `apt`/`dpkg`, `dnf`/RPM, or `pacman`.

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
├── packaging/
│   ├── build-packages.sh
│   ├── flstudiorpc.desktop
│   ├── nfpm.yaml
│   └── scripts/
│       ├── postinstall.sh
│       └── postremove.sh
├── .github/
│   └── workflows/
│       ├── build-packages.yml
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

If you encounter a bug, have a feature request, or experience an issue with FL Studio running through Wine or Proton, please open an issue on the [GitHub repository](https://github.com/devrainz/FLStudio-Linux-RPC/issues).

When reporting an issue, including information such as your:

* Linux distribution
* Desktop environment or window manager
* Wine or Proton version
* FL Studio version
* Discord installation type

can make troubleshooting easier.

Feedback and contributions are welcome.

## About This Fork

**FL Studio Discord RPC for Linux** is a Linux-focused fork and substantial port of the original [FL Studio Discord RPC](https://github.com/zfi2/FL-Studio-Discord-RPC) by [@zfi2](https://github.com/zfi2).

The original project provided the foundation for this Linux version, and portions of its original code and overall functionality were retained where appropriate.

However, the original application was heavily dependent on **Windows-specific APIs and technologies**, making a direct Linux port impractical.

Because of this, a significant portion of the application had to be **rewritten and replaced with Linux-native implementations**.

Windows-specific functionality was removed or replaced with Linux-compatible approaches for areas such as:

* FL Studio process detection
* Wine and Proton detection
* Discord Rich Presence integration
* System tray integration
* Application and desktop integration
* XDG autostart integration
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
