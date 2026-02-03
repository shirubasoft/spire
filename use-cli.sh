#!/usr/bin/env bash
set -euo pipefail

TOOL_NAME="spire.cli"
PROJECT_PATH="src/cli/Spire.Cli.csproj"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

usage() {
    echo "Usage: $0 <local|nuget>"
    echo ""
    echo "  local  - Build and install the CLI globally from the local project"
    echo "  nuget  - Install the CLI globally from NuGet"
    exit 1
}

[[ $# -eq 1 ]] || usage

case "$1" in
    local)
        ARTIFACTS_DIR="$SCRIPT_DIR/artifacts"

        echo "Building solution in Release..."
        dotnet build "$SCRIPT_DIR/Spire.slnx" -c Release

        echo "Packing CLI NuGet package..."
        rm -rf "$ARTIFACTS_DIR"
        mkdir -p "$ARTIFACTS_DIR"
        dotnet pack "$SCRIPT_DIR/$PROJECT_PATH" -c Release -o "$ARTIFACTS_DIR" --no-build

        VERSION=$(nbgv get-version -v NuGetPackageVersion)
        echo "Package version: $VERSION"

        echo "Clearing NuGet cache for spire.cli package..."
        NUGET_PACKAGES_DIR="$(dotnet nuget locals global-packages -l | sed 's/.*: //')"
        rm -rf "$NUGET_PACKAGES_DIR"/spire.cli

        echo "Installing local CLI globally (v$VERSION)..."
        dotnet tool update "$TOOL_NAME" \
            --global \
            --allow-downgrade \
            --add-source "$ARTIFACTS_DIR" \
            --version "$VERSION"
        ;;
    nuget)
        echo "Installing NuGet CLI globally..."
        dotnet tool update "$TOOL_NAME" \
            --global \
            --allow-downgrade
        ;;
    *)
        usage
        ;;
esac

echo "Done."
