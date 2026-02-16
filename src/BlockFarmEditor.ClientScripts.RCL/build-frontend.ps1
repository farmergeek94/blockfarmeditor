$ErrorActionPreference = 'Stop'

$rootDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projects = @(
    'block-editor',
    'definitions-workspace',
    'property-editor',
    'settings-dashboard'
)

foreach ($project in $projects) {
    Write-Host "==> $project"
    Set-Location (Join-Path $rootDir $project)
    npm install
    npm run build
}
