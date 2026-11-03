# Build the combined MOCASSIN documentation site.
#
# Zensical owns the narrative site. DocFX and rustdoc output is copied into
# site/api/dotnet and site/api/rust when the corresponding projects/crates
# exist. Generated API HTML and site/ are not committed.
$ErrorActionPreference = "Stop"

$Root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
Set-Location $Root

if (-not (Get-Command zensical -ErrorAction SilentlyContinue)) {
    throw "zensical is not installed; see tools/docs/requirements.txt"
}

Write-Host "==> Zensical (strict)"
& zensical build --strict
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ((Test-Path "Mocassin.slnx") -and (Test-Path "tools/docfx/docfx.json") -and
    (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "==> DocFX"
    & dotnet tool restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    if (Test-Path "artifacts/docfx") { Remove-Item -Recurse -Force "artifacts/docfx" }
    & dotnet docfx "tools/docfx/docfx.json" -o "artifacts/docfx"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    New-Item -ItemType Directory -Force "site/api" | Out-Null
    if (Test-Path "site/api/dotnet") { Remove-Item -Recurse -Force "site/api/dotnet" }
    Copy-Item -Recurse "artifacts/docfx" "site/api/dotnet"
}
else {
    Write-Host "==> DocFX skipped (solution or tools/docfx/docfx.json not present yet)"
}

if ((Test-Path "src/rust/Cargo.toml") -and (Get-Command cargo -ErrorAction SilentlyContinue)) {
    Write-Host "==> rustdoc"
    & cargo doc --manifest-path "src/rust/Cargo.toml" --workspace --no-deps --target-dir "target/doc"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    New-Item -ItemType Directory -Force "site/api" | Out-Null
    if (Test-Path "site/api/rust") { Remove-Item -Recurse -Force "site/api/rust" }
    Copy-Item -Recurse "target/doc" "site/api/rust"
}
else {
    Write-Host "==> rustdoc skipped (Rust workspace not present yet)"
}

Write-Host "Documentation built in $Root/site"
