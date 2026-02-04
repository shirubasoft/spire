#Requires -Version 5.1
$ErrorActionPreference = 'Stop'

$ToolName = "spire.cli"
$ScriptDir = $PSScriptRoot

function Show-Usage {
    Write-Host "Usage: .\use-spire.ps1 <local|nuget>"
    Write-Host ""
    Write-Host "  local  - Build and install the CLI globally from the local project"
    Write-Host "  nuget  - Install the CLI globally from NuGet"
    exit 1
}

if ($args.Count -ne 1) { Show-Usage }

switch ($args[0]) {
    "local" {
        $ArtifactsDir = Join-Path $ScriptDir "artifacts"

        Write-Host "Building solution in Release..."
        dotnet build (Join-Path $ScriptDir "Spire.slnx") -c Release
        if ($LASTEXITCODE -ne 0) { throw "Build failed" }

        Write-Host "Packing NuGet packages..."
        if (Test-Path $ArtifactsDir) { Remove-Item $ArtifactsDir -Recurse -Force }
        New-Item -ItemType Directory -Path $ArtifactsDir -Force | Out-Null
        dotnet pack (Join-Path $ScriptDir "Spire.slnx") -c Release -o $ArtifactsDir --no-build
        if ($LASTEXITCODE -ne 0) { throw "Pack failed" }

        $Version = nbgv get-version -v NuGetPackageVersion
        if ($LASTEXITCODE -ne 0) { throw "Failed to get version" }
        Write-Host "Package version: $Version"

        Write-Host "Clearing NuGet cache for spire packages..."
        $NuGetPackagesDir = (dotnet nuget locals global-packages -l) -replace '.*:\s*', ''
        Get-ChildItem -Path $NuGetPackagesDir -Directory -Filter "spire.*" -ErrorAction SilentlyContinue |
            ForEach-Object { Remove-Item $_.FullName -Recurse -Force }

        Write-Host "Installing local CLI globally (v$Version)..."
        dotnet tool update $ToolName `
            --global `
            --allow-downgrade `
            --add-source $ArtifactsDir `
            --version $Version
        if ($LASTEXITCODE -ne 0) { throw "Tool install failed" }
    }
    "nuget" {
        Write-Host "Installing NuGet CLI globally..."
        dotnet tool update $ToolName `
            --global `
            --allow-downgrade
        if ($LASTEXITCODE -ne 0) { throw "Tool install failed" }
    }
    default { Show-Usage }
}

Write-Host "Done."
