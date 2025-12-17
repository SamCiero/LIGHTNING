param(
  [ValidateSet("win-x64","win-arm64")]
  [string]$Rid = "win-x64",

  [ValidateSet("Release","Debug")]
  [string]$Config = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$appProj  = Join-Path $repoRoot "src\LIGHTNING.App\LIGHTNING.App.csproj"
$outDir   = Join-Path $repoRoot "artifacts\publish\$Rid\self-contained-folder"

Write-Host "Publishing LIGHTNING.App..."
Write-Host "  Config: $Config"
Write-Host "  RID:    $Rid"
Write-Host "  Out:    $outDir"

dotnet publish $appProj `
  -c $Config `
  -r $Rid `
  --self-contained true `
  -o $outDir `
  -p:PublishSingleFile=false `
  -p:PublishTrimmed=false

Write-Host "Done."
