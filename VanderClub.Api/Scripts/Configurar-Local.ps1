# Salva configuracoes somente nos User Secrets do seu usuario Windows.
# Execute a partir da pasta do projeto; nenhuma aplicacao cliente e alterada.
[CmdletBinding()]
param([int]$PortaMySql = 3307)
$ErrorActionPreference = 'Stop'
$previousOutputEncoding = $OutputEncoding
$OutputEncoding = New-Object System.Text.UTF8Encoding($false)
$project = Join-Path $PSScriptRoot '..\VanderClub.Api.csproj'
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'Instale o SDK .NET 10 antes de continuar.' }
if ($PortaMySql -lt 1 -or $PortaMySql -gt 65535) { throw 'Porta MySQL invalida.' }

$portText = Read-Host "Porta do MySQL do VanderClub [Enter = $PortaMySql]"
if ($portText) {
    $PortaMySql = [int]$portText
    if ($PortaMySql -lt 1 -or $PortaMySql -gt 65535) { throw 'Porta MySQL invalida.' }
}
$dbUser = Read-Host 'Usuario do MySQL [Enter = vanderclub_api]'
if (-not $dbUser) { $dbUser = 'vanderclub_api' }
$secure = Read-Host "Senha MySQL de $dbUser (nao e a senha do Presidente)" -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try {
    $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr)
    if (-not $plain) { throw 'Informe a senha da conta MySQL.' }
    # Evita a conversao PSObject -> String do indexador do builder no PowerShell.
    # Valores sao sempre delimitados; aspas internas sao duplicadas (sintaxe ADO.NET).
    $quotedUser = ([string]$dbUser).Replace('"', '""')
    $quotedPassword = ([string]$plain).Replace('"', '""')
    $connectionString = 'Server=127.0.0.1;Port={0};Database=vanderclub;User ID="{1}";Password="{2}";SslMode=Required;' -f $PortaMySql, $quotedUser, $quotedPassword

    # Preserva a chave ja existente para manter as repeticoes idempotentes validas.
    $output = & dotnet user-secrets list --json --project $project
    if ($LASTEXITCODE -ne 0) { throw 'Nao foi possivel ler User Secrets.' }
    $json = ($output | Where-Object { $_ -notmatch '^\s*//' }) -join "`n"
    $old = $json | ConvertFrom-Json
    $key = $old.'Security:IdempotencySecret'
    if (-not $key -or $key.Length -lt 32) {
        $bytes = New-Object byte[] 32
        $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
        try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
        $key = [Convert]::ToBase64String($bytes)
    }
    $settings = @{
        'ConnectionStrings:VanderClub' = [string]$connectionString
        'Security:IdempotencySecret' = $key
    }
    # Envia o JSON pela entrada padrao; senhas nao sao argumentos do processo.
    $settings | ConvertTo-Json -Compress | & dotnet user-secrets set --project $project
    if ($LASTEXITCODE -ne 0) { throw 'Nao foi possivel salvar User Secrets.' }
    Write-Host 'Configuracao salva. Execute dotnet run -- --verify para conferir o banco.'
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
    $plain = $null; $settings = $null; $connectionString = $null; $quotedUser = $null; $quotedPassword = $null; $key = $null; $old = $null; $output = $null; $json = $null
    $secure.Dispose()
    $OutputEncoding = $previousOutputEncoding
}
