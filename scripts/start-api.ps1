$ErrorActionPreference = 'Stop'
$workspaceRoot = Split-Path -Parent $PSScriptRoot
$settingsPath = Join-Path $workspaceRoot 'src/Platform.Api/appsettings.Local.json'
if (-not (Test-Path -LiteralPath $settingsPath)) { throw 'Ejecuta primero scripts/init-local.ps1 para configurar SQL Server.' }
$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
try {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    dotnet run --project (Join-Path $workspaceRoot 'src/Platform.Api') --no-launch-profile --urls http://127.0.0.1:5080
}
finally {
    $env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
}
