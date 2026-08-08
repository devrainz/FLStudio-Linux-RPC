#!/usr/bin/env bash

set -e

INSTALL_DIR="/opt/flstudio-rpc"
DESKTOP_FILE="/usr/share/applications/flstudiorpc.desktop"
ICON_FILE="/usr/share/icons/hicolor/128x128/apps/flstudio.png"

echo
echo "========================================"
echo " FL Studio Discord RPC - Linux Uninstall"
echo "========================================"
echo

if [[ "$EUID" -ne 0 ]]; then
    echo "ERROR: Please run the uninstaller with sudo."
    echo
    echo "Usage:"
    echo "  sudo ./uninstall.sh"
    echo
    exit 1
fi

echo "[1/4] Removing application..."

if [[ -d "$INSTALL_DIR" ]]; then
    rm -rf "$INSTALL_DIR"
    echo "      Removed $INSTALL_DIR"
else
    echo "      Application directory not found."
fi

echo
echo "[2/4] Removing desktop entry..."

if [[ -f "$DESKTOP_FILE" ]]; then
    rm -f "$DESKTOP_FILE"
    echo "      Removed $DESKTOP_FILE"
else
    echo "      Desktop entry not found."
fi

echo
echo "[3/4] Removing application icon..."

if [[ -f "$ICON_FILE" ]]; then
    rm -f "$ICON_FILE"
    echo "      Removed $ICON_FILE"
else
    echo "      Application icon not found."
fi

echo
echo "[4/4] Updating desktop database..."

if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database /usr/share/applications >/dev/null 2>&1 || true
fi

echo
echo "FL Studio Discord RPC has been uninstalled successfully."
echo