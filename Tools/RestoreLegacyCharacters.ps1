param(
    [string]$ProjectRoot = "D:\Unity\Ashfall-iOS"
)

$ErrorActionPreference = "Stop"
$bak = Join-Path $ProjectRoot "Assets\ArtSource\LegacyCharactersBackup"
if (!(Test-Path $bak)) {
    throw "LegacyCharactersBackup folder not found at $bak"
}

foreach ($sub in @("Briggs", "Iona", "Raven", "ValeBoss", "ValeProps")) {
    $src = Join-Path $bak "Characters\$sub"
    $dst = Join-Path $ProjectRoot "Assets\Resources\Characters\$sub"
    if (Test-Path $src) {
        New-Item -ItemType Directory -Force $dst | Out-Null
        robocopy $src $dst /MIR /XF *.meta | Out-Null
        Write-Host "Restored Characters/$sub"
    }
}

$ieSrc = Join-Path $bak "ImportedEnemies"
$ieDst = Join-Path $ProjectRoot "Assets\Resources\ImportedEnemies"
if (Test-Path $ieSrc) {
    Get-ChildItem $ieSrc -File | ForEach-Object {
        Copy-Item $_.FullName (Join-Path $ieDst $_.Name) -Force
        Write-Host "Restored ImportedEnemies/$($_.Name)"
    }
}

Write-Host "Done. Next run Unity / verify_parity.py --unity (or re-import assets in Unity)."
