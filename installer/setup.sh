 #!/usr/bin/env bash

set -e

APP_NAME="FLStudioRPC"
INSTALL_DIR="/opt/flstudio-rpc"
DESKTOP_FILE="/usr/share/applications/flstudiorpc.desktop"

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
ICON_SOURCE="$SCRIPT_DIR/Icons"

if [[ ! -f "$APP_SOURCE" ]]; then
    echo "ERROR: FLStudioRPC executable was not found."
    echo
    echo "Expected:"
    echo "  $APP_SOURCE"
    echo
    exit 1
fi

if [[ ! -d "$ICON_SOURCE" ]]; then
    echo "ERROR: Icons directory was not found."
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
cp -a "$ICON_SOURCE" "$INSTALL_DIR/Icons"

chmod +x "$INSTALL_DIR/$APP_NAME"

echo "      Installed to:"
echo "      $INSTALL_DIR"

echo
echo "[2/5] Installing application icon..."

ICON_PATH="$INSTALL_DIR/Icons/hicolor/128x128/apps/flstudio.png"

if [[ ! -f "$ICON_PATH" ]]; then
    echo "ERROR: Installed icon was not found."
    exit 1
fi

echo "      $ICON_PATH"

echo
echo "[3/5] Installing desktop entry..."

mkdir -p "$(dirname "$DESKTOP_FILE")"

cat > "$DESKTOP_FILE" <<EOF
[Desktop Entry]
Type=Application
Name=FL Studio Discord RPC
Comment=Discord Rich Presence for FL Studio on Linux
Exec=$INSTALL_DIR/$APP_NAME
Icon=$ICON_PATH
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