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
    if (Test-Path "target/doc") { Remove-Item -Recurse -Force "target/doc" }
    & cargo doc --manifest-path "src/rust/Cargo.toml" --workspace --no-deps --target-dir "target"
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    New-Item -ItemType Directory -Force "site/api" | Out-Null
    if (Test-Path "site/api/rust") { Remove-Item -Recurse -Force "site/api/rust" }
    Copy-Item -Recurse "target/doc" "site/api/rust"

    # cargo doc has no workspace landing page, so generate a small crate index.
    $crateLinks = Get-ChildItem -Directory "target/doc" |
        Where-Object {
            $_.Name -notin @("src", "static.files") -and
            (Test-Path (Join-Path $_.FullName "index.html"))
        } |
        ForEach-Object { '<li><a href="{0}/index.html">{0}</a></li>' -f $_.Name }

    $html = @"
<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>MOCASSIN Rust API</title></head><body>
<h1>MOCASSIN Rust API</h1>
<ul>
$($crateLinks -join "`n")
</ul>
</body></html>
"@
    Set-Content -Path "site/api/rust/index.html" -Value $html -Encoding utf8
}
else {
    Write-Host "==> rustdoc skipped (Rust workspace not present yet)"
}

Write-Host "Documentation built in $Root/site"
