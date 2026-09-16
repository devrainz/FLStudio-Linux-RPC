#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

VERSION="${1:-}"
VERSION="${VERSION#v}"

if [[ -z "$VERSION" ]]; then
    echo "Usage: $0 <version>" >&2
    echo "Example: $0 1.2.0" >&2
    exit 1
fi

if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: .NET 8 SDK is required to build FLStudioRPC." >&2
    exit 1
fi

rm -rf publish/linux-x64 dist
mkdir -p dist

echo "==> Restoring .NET dependencies"
dotnet restore FLStudioRPC.csproj

echo "==> Publishing self-contained Linux x86-64 binary"
dotnet publish FLStudioRPC.csproj \
    --configuration Release \
    --runtime linux-x64 \
    --self-contained true \
    --output publish/linux-x64 \
    /p:PublishSingleFile=true \
    /p:IncludeNativeLibrariesForSelfExtract=true \
    /p:DebugType=None \
    /p:DebugSymbols=false

chmod 0755 publish/linux-x64/FLStudioRPC
test -x publish/linux-x64/FLStudioRPC

DEB="dist/flstudiorpc_${VERSION}_amd64.deb"
RPM="dist/flstudiorpc-${VERSION}-1.x86_64.rpm"
ARCH="dist/flstudiorpc-${VERSION}-1-x86_64.pkg.tar.zst"

run_nfpm() {
    local packager="$1"
    local target="$2"

    if command -v nfpm >/dev/null 2>&1; then
        VERSION="$VERSION" nfpm package \
            --config packaging/nfpm.yaml \
            --packager "$packager" \
            --target "$target"
        return
    fi

    if ! command -v docker >/dev/null 2>&1; then
        echo "ERROR: nFPM or Docker is required to create distro packages." >&2
        exit 1
    fi

    local nfpm_image="ghcr.io/goreleaser/nfpm:v2.47.0"

    docker run --rm \
        -e VERSION="$VERSION" \
        -v "$ROOT_DIR:/tmp" \
        -w /tmp \
        "$nfpm_image" \
        package \
        --config packaging/nfpm.yaml \
        --packager "$packager" \
        --target "/tmp/$target"
}

echo "==> Building Debian package"
run_nfpm deb "$DEB"

echo "==> Building RPM package"
run_nfpm rpm "$RPM"

echo "==> Building Arch Linux package"
run_nfpm archlinux "$ARCH"

test -s "$DEB"
test -s "$RPM"
test -s "$ARCH"

sha256sum "$DEB" "$RPM" "$ARCH" > dist/SHA256SUMS

echo
echo "Packages created successfully:"
ls -lh dist
