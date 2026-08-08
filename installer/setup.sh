#!/usr/bin/env bash

set -e

APP_NAME="FLStudioRPC"
INSTALL_DIR="/opt/flstudio-rpc"
DESKTOP_FILE="/usr/share/applications/flstudiorpc.desktop"
ICON_DIR="/usr/share/icons/hicolor/128x128/apps"
ICON_FILE="$ICON_DIR/flstudio.png"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo
echo "======================================"
echo " FL Studio Discord RPC - Linux Setup"
echo "======================================"
echo

if [[ "$EUID" -ne 0 ]]; then
    echo "ERROR: Please run the installer with sudo."
    echo
    echo "Usage:"
    echo "  sudo ./setup.sh"
    echo
    exit 1
fi

if [[ "$(uname -m)" != "x86_64" ]]; then
    echo "ERROR: This installer currently supports x86-64 Linux only."
    echo
    echo "Detected architecture: $(uname -m)"
    echo
    exit 1
fi

APP_SOURCE="$SCRIPT_DIR/FLStudioRPC"
ICON_SOURCE="$SCRIPT_DIR/Icons/hicolor/128x128/apps/flstudio.png"

if [[ ! -f "$APP_SOURCE" ]]; then
    echo "ERROR: FLStudioRPC executable was not found."
    echo
    echo "Expected:"
    echo "  $APP_SOURCE"
    echo
    exit 1
fi

if [[ ! -f "$ICON_SOURCE" ]]; then
    echo "ERROR: FL Studio icon was not found."
    echo
    echo "Expected:"
    echo "  $ICON_SOURCE"
    echo
    exit 1
fi

echo "[1/5] Installing application..."

rm -rf "$INSTALL_DIR"
mkdir -p "$INSTALL_DIR"

cp "$APP_SOURCE" "$INSTALL_DIR/$APP_NAME"
chmod +x "$INSTALL_DIR/$APP_NAME"

echo "      Installed to:"
echo "      $INSTALL_DIR/$APP_NAME"

echo
echo "[2/5] Installing application icon..."

mkdir -p "$ICON_DIR"
cp "$ICON_SOURCE" "$ICON_FILE"
chmod 644 "$ICON_FILE"

echo "      $ICON_FILE"

echo
echo "[3/5] Installing desktop entry..."

mkdir -p "$(dirname "$DESKTOP_FILE")"

cat > "$DESKTOP_FILE" <<EOF
[Desktop Entry]
Type=Application
Name=FL Studio Discord RPC
Comment=Discord Rich Presence for FL Studio on Linux
Exec=$INSTALL_DIR/$APP_NAME
Icon=flstudio
Terminal=false
Categories=Utility;
StartupNotify=false
EOF

chmod 644 "$DESKTOP_FILE"

echo "      $DESKTOP_FILE"

echo
echo "[4/5] Updating desktop database..."

if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database /usr/share/applications >/dev/null 2>&1 || true
fi

echo
echo "[5/5] Installation complete!"
echo
echo "FL Studio Discord RPC has been installed successfully."
echo
echo "You can now launch it from your application menu."
echo