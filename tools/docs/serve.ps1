# SPDX-License-Identifier: MIT
# Serve the narrative documentation locally with live reload.
$ErrorActionPreference = "Stop"

$Root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
Set-Location $Root

if (-not (Get-Command zensical -ErrorAction SilentlyContinue)) {
    throw "zensical is not installed; see tools/docs/requirements.txt"
}

& zensical serve @args
exit $LASTEXITCODE
