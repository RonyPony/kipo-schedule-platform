param(
    [Parameter(Mandatory)][string]$Email,
    [string]$DisplayName,
    [ValidateSet('Create', 'ResetMfa')][string]$Mode = 'Create',
    [ValidateSet('Development', 'Staging', 'Production')][string]$Environment = 'Development'
)

$ErrorActionPreference = 'Stop'
if ($Mode -eq 'Create' -and [string]::IsNullOrWhiteSpace($DisplayName)) { throw 'Indica el nombre del administrador con -DisplayName.' }
$workspaceRoot = Split-Path -Parent $PSScriptRoot
$password = Read-Host 'Contraseña administrativa (mínimo 12 caracteres, mayúscula, minúscula, número y símbolo)' -AsSecureString
$previous = @{}
foreach ($key in @('KIPO_ADMIN_EMAIL', 'KIPO_ADMIN_NAME', 'KIPO_ADMIN_PASSWORD', 'ASPNETCORE_ENVIRONMENT')) {
    $previous[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
}
try {
    $env:KIPO_ADMIN_EMAIL = $Email
    $env:KIPO_ADMIN_NAME = $DisplayName
    $env:KIPO_ADMIN_PASSWORD = [System.Net.NetworkCredential]::new('', $password).Password
    $env:ASPNETCORE_ENVIRONMENT = $Environment
    $operation = if ($Mode -eq 'Create') { '--Admin:Create=true' } else { '--Admin:ResetMfa=true' }
    dotnet run --project (Join-Path $workspaceRoot 'src/Platform.Api') --configuration Release --no-launch-profile -- $operation
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo completar la configuración administrativa.' }
}
finally {
    foreach ($key in $previous.Keys) {
        [Environment]::SetEnvironmentVariable($key, $previous[$key], 'Process')
    }
}
