# Quick rebuild script - build only, no install
# Used to check for compile errors

param(
    [string]$Configuration = "Debug"
)

$ProjectPath = "$PSScriptRoot\ProjectOpenerExtension\ProjectOpenerExtension.csproj"

Write-Host "=== Quick build ===" -ForegroundColor Cyan
dotnet build $ProjectPath -c $Configuration -r win-x64 /p:Platform=x64 /p:GenerateAppxPackageOnBuild=false

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✓ Build succeeded! Run .\update-extension.ps1 -SkipBuild for a quick install" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "✗ Build failed, please fix the errors" -ForegroundColor Red
}
