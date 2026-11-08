param(
    [Parameter(Mandatory)][string]$Email,
    [Parameter(Mandatory)][string]$DisplayName
)

$ErrorActionPreference = 'Stop'
$workspaceRoot = Split-Path -Parent $PSScriptRoot
$password = Read-Host 'Contraseña del nuevo usuario (mínimo 12 caracteres, mayúscula, minúscula, número y símbolo)' -AsSecureString
$previous = @{}
foreach ($key in @('KIPO_LOCAL_USER_EMAIL', 'KIPO_LOCAL_USER_NAME', 'KIPO_LOCAL_USER_PASSWORD', 'ASPNETCORE_ENVIRONMENT')) {
    $previous[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
}
try {
    $env:KIPO_LOCAL_USER_EMAIL = $Email
    $env:KIPO_LOCAL_USER_NAME = $DisplayName
    $env:KIPO_LOCAL_USER_PASSWORD = [System.Net.NetworkCredential]::new('', $password).Password
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    dotnet run --project (Join-Path $workspaceRoot 'src/Platform.Api') --no-launch-profile -- --LocalUser:Create=true
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear el usuario local.' }
}
finally {
    foreach ($key in $previous.Keys) {
        [Environment]::SetEnvironmentVariable($key, $previous[$key], 'Process')
    }
}
