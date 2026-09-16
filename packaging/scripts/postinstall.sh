#!/bin/sh
set -e

# Remove the application files created by releases that used the legacy
# setup.sh installer. The current package owns /usr/bin/flstudiorpc instead.
if [ -f /opt/flstudio-rpc/FLStudioRPC ]; then
    rm -rf /opt/flstudio-rpc
fi

# The old installer also overwrote /usr/share/icons/hicolor/index.theme.
# Do not try to modify another package's file here; emit a one-time warning so
# users can restore the distro-owned hicolor theme package if necessary.
if [ -f /usr/share/icons/hicolor/index.theme ] && \
   grep -q '^Comment=FLStudioRPC icons$' /usr/share/icons/hicolor/index.theme 2>/dev/null; then
    echo "WARNING: legacy FLStudioRPC installer damage detected in hicolor/index.theme." >&2
    echo "Reinstall your distro's hicolor-icon-theme package once to restore it." >&2
fi

# Refresh desktop/icon caches when the distro provides the helpers.
# The package itself owns the launcher and icons; these commands only make
# desktop environments notice changes immediately.
if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database /usr/share/applications >/dev/null 2>&1 || true
fi

if command -v gtk-update-icon-cache >/dev/null 2>&1; then
    gtk-update-icon-cache -f -t /usr/share/icons/hicolor >/dev/null 2>&1 || true
fi

exit 0
