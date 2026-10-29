# Real Windows apphost test: self-contained old install + lean update with a new DLL.
$ErrorActionPreference = 'Stop'
$root = Join-Path $env:RUNNER_TEMP ('clinicpro-update-test-' + [Guid]::NewGuid().ToString('N'))
$old = Join-Path $root 'old'
$newFull = Join-Path $root 'new-full'
$newLean = Join-Path $root 'new-lean'
try {
    dotnet publish tests/UpdateHostFixture/Old/Old.csproj -c Release -r win-x64 --self-contained true -o $old
    if ($LASTEXITCODE -ne 0) { throw 'Old self-contained fixture publish failed' }
    dotnet publish tests/UpdateHostFixture/New/New.csproj -c Release -r win-x64 --self-contained true -o $newFull
    if ($LASTEXITCODE -ne 0) { throw 'New self-contained fixture publish failed' }
    dotnet publish tests/UpdateHostFixture/New/New.csproj -c Release -r win-x64 --self-contained false -o $newLean
    if ($LASTEXITCODE -ne 0) { throw 'New lean fixture publish failed' }
    $exe = Join-Path $old 'ClinicPro.UpdateHost.exe'
    $before = & $exe
    if ($LASTEXITCODE -ne 0 -or $before -ne 'old-version') { throw 'Old self-contained host failed' }
    $runtimeConfig = Join-Path $old 'ClinicPro.UpdateHost.runtimeconfig.json'
    $originalRuntimeHash = (Get-FileHash $runtimeConfig).Hash
    # Same overlay rule as installer: retain old runtimeconfig and runtime files,
    # install the lean payload and the current self-contained dependency manifest.
    Get-ChildItem $newLean -File | Where-Object { $_.Name -notin @('ClinicPro.UpdateHost.runtimeconfig.json', 'ClinicPro.UpdateHost.deps.json') } |
        Copy-Item -Destination $old -Force
    Copy-Item (Join-Path $newFull 'ClinicPro.UpdateHost.deps.json') $old -Force
    if ((Get-FileHash $runtimeConfig).Hash -ne $originalRuntimeHash) { throw 'Runtime config was overwritten' }
    $after = & $exe
    if ($LASTEXITCODE -ne 0 -or $after -ne 'new-dependency-loaded') { throw 'Updated self-contained host did not load the new dependency' }
    Write-Host 'Self-contained update loaded the new dependency successfully.'
} finally {
    Remove-Item $root -Recurse -Force -ErrorAction SilentlyContinue
}
