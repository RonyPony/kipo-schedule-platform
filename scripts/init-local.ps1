param(
    [string]$SqlServer = '.\SQLEXPRESS',
    [string]$SqlUser = 'sa',
    [System.Security.SecureString]$SqlPassword
)

$ErrorActionPreference = 'Stop'
$workspaceRoot = Split-Path -Parent $PSScriptRoot
Push-Location $workspaceRoot
try {
    $settingsPath = Join-Path $workspaceRoot 'src/Platform.Api/appsettings.Local.json'
    $settings = @{}
    if (Test-Path -LiteralPath $settingsPath) {
        $settings = [System.IO.File]::ReadAllText($settingsPath, [System.Text.Encoding]::UTF8) | ConvertFrom-Json -AsHashtable
    }
    $connection = [System.Data.Common.DbConnectionStringBuilder]::new()
    if (-not $SqlPassword -and -not $PSBoundParameters.ContainsKey('SqlServer') -and -not $PSBoundParameters.ContainsKey('SqlUser') -and $settings.ConnectionStrings.DefaultConnection) {
        $connection.set_ConnectionString([string]$settings.ConnectionStrings.DefaultConnection)
    }
    else {
        if (-not $SqlPassword) { $SqlPassword = Read-Host 'Contraseña de SQL Server' -AsSecureString }
        $connection['Server'] = $SqlServer
        $connection['Database'] = 'Kipo'
        $connection['User Id'] = $SqlUser
        $connection['Password'] = [System.Net.NetworkCredential]::new('', $SqlPassword).Password
        $connection['Encrypt'] = 'True'
        $connection['TrustServerCertificate'] = 'True'
        $connection['Connect Timeout'] = '5'
    }
    $previousSqlPassword = $env:SQLCMDPASSWORD
    try {
        $env:SQLCMDPASSWORD = [string]$connection['Password']
        sqlcmd -S ([string]$connection['Server']) -U ([string]$connection['User Id']) -d master -C -b -l 8 -Q "IF DB_ID(N'Kipo') IS NULL CREATE DATABASE [Kipo];"
        if ($LASTEXITCODE -ne 0) { throw 'No se pudo preparar la base Kipo en SQL Server local.' }
    }
    finally {
        $env:SQLCMDPASSWORD = $previousSqlPassword
    }
    if (-not $settings.ConnectionStrings) { $settings.ConnectionStrings = @{} }
    $settings.ConnectionStrings.DefaultConnection = $connection.get_ConnectionString()
    [System.IO.File]::WriteAllText($settingsPath, (($settings | ConvertTo-Json -Depth 20) + "`n"), [System.Text.UTF8Encoding]::new($false))
    New-Item -ItemType Directory -Path '.local/certs' -Force | Out-Null
    dotnet dev-certs https --export-path .local/certs/localhost.pem --format PEM --no-password
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo exportar el certificado de desarrollo.' }
    Write-Host 'SQL Server local y certificados preparados. La configuración privada está excluida de Git.'
}
finally {
    Pop-Location
}
