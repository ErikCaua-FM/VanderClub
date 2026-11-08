# Cadastro de uma area REAL. Nao cria dados de demonstracao.
[CmdletBinding()]
param([string]$ApiBaseUrl = 'https://localhost:7147/api/v1/')
$ErrorActionPreference = 'Stop'
$api = $ApiBaseUrl.TrimEnd('/') + '/'
$uri = [Uri]$api
if ($uri.Scheme -ne 'https') { throw 'Use HTTPS com certificado confiavel.' }
$origin = $uri.GetLeftPart([UriPartial]::Authority)
$email = Read-Host 'E-mail do Presidente'
$secure = Read-Host 'Senha do Presidente' -AsSecureString
$ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$headers = @{ Origin = $origin }
$loggedIn = $false
try {
    $loginJson = @{ email=$email; password=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) } | ConvertTo-Json -Compress
    $login = Invoke-RestMethod -Method Post -Uri ($api+'auth/admin/login') -WebSession $session -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes($loginJson))
    $loggedIn = $true
    $headers['X-CSRF-Token'] = $login.csrfToken
    if ($login.actor.role -ne 'ADM_GERAL') { throw 'Somente o Presidente pode cadastrar areas.' }
    $bootstrap = Invoke-RestMethod -Method Get -Uri ($api+'admin/bootstrap') -WebSession $session -Headers $headers
    $bootstrap.units | Where-Object active | Format-Table id,name,city,uf
    $unit = Read-Host 'ID da unidade'
    if (-not ($bootstrap.units | Where-Object { $_.id -eq $unit -and $_.active })) { throw 'Unidade nao encontrada/ativa.' }
    $types = Invoke-RestMethod -Method Get -Uri ($api+'admin/areas/types') -WebSession $session -Headers $headers
    $types | Format-Table code,name,maxMinutes
    $code = Read-Host 'Codigo do tipo da area'
    $type = $types | Where-Object code -eq $code
    if (-not $type) { throw 'Tipo nao encontrado.' }
    $name = Read-Host 'Nome da area'
    $description = Read-Host 'Descricao'
    $rules = Read-Host 'Regras de uso'
    $capacity = [int](Read-Host 'Capacidade de pessoas')
    Write-Host 'Horarios iguais nos 7 dias. Minutos desde 00h: 08h=480, 22h=1320, 24h=1440.'
    $openMinute = [int](Read-Host 'Minuto de abertura')
    $closeMinute = [int](Read-Host 'Minuto de fechamento')
    $maxMinutes = 0
    if ($type.maxMinutes -gt 0) {
        $maxMinutes = [int](Read-Host "Duracao maxima em minutos (ate $($type.maxMinutes))")
    }
    Write-Host 'Imagens existentes: academia.svg, futebol.svg, tenis.svg, poli.svg, areia.svg, jogos.svg, festa.svg, social.svg, quiosque.svg.'
    $image = Read-Host 'Nome da imagem [Enter = social.svg]'
    if (-not $image) { $image = 'social.svg' }
    $advance = [int](Read-Host 'Antecedencia minima para reservar, em horas (0 permite imediato)')
    $days = [int](Read-Host 'Antecedencia maxima para reservar, em dias (1 a 365)')
    $buffer = [int](Read-Host 'Intervalo apos cada uso, em minutos (0 permite horarios adjacentes)')
    $cancel = [int](Read-Host 'Antecedencia minima para cancelar, em horas (0 permite ate o inicio)')
    $body = @{ unitId=$unit; typeCode=$type.code; name=$name; description=$description; rules=$rules; image=$image; capacity=$capacity; maxMinutes=$maxMinutes; openMinute=$openMinute; closeMinute=$closeMinute; advanceHours=$advance; advanceDays=$days; bufferMinutes=$buffer; cancelHours=$cancel; active=$true }
    $body | ConvertTo-Json
    $confirm = Read-Host 'Cadastrar esta area? Digite SIM'
    if ($confirm -cne 'SIM') { Write-Host 'Nenhuma area criada.'; return }
    $headers['Idempotency-Key'] = [Guid]::NewGuid().ToString()
    $result = Invoke-RestMethod -Method Post -Uri ($api+'admin/areas') -WebSession $session -Headers $headers -ContentType 'application/json; charset=utf-8' -Body ([Text.Encoding]::UTF8.GetBytes(($body | ConvertTo-Json -Compress)))
    Write-Host "Area cadastrada: $($result.name), ID $($result.id)."
} finally {
    if ($loggedIn) {
        try { Invoke-RestMethod -Method Post -Uri ($api+'auth/admin/logout') -WebSession $session -Headers $headers | Out-Null }
        catch { Write-Warning 'Nao foi possivel encerrar a sessao auxiliar; ela expira automaticamente.' }
    }
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr)
    $loginJson = $null; $login = $null; $headers = $null
    $secure.Dispose()
}
