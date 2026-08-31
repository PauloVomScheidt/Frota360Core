<#
.SYNOPSIS
    Provisiona uma empresa de desenvolvimento com Admin e Motorista prontos.

.DESCRIPTION
    Num banco zerado nao existe usuario, e POST /convite exige estar autenticado.
    O unico jeito de entrar e o endpoint de backoffice, protegido por X-Backoffice-Key.
    Este script faz o bootstrap inteiro:

      1. POST /backoffice/empresa   -> cria a empresa, semeia os 10 TiposManutencaoPadrao
                                       e emite o convite do primeiro Admin
      2. POST /convite/aceitar      -> cria o Admin e devolve o JWT
      3. POST /convite (como Admin) -> convida um Motorista
      4. POST /convite/aceitar      -> cria o Motorista

    E re-executavel: se a empresa de dev ja existir, o passo 1 cai para um login com as
    mesmas credenciais em vez de falhar, e o passo 3 pula o motorista que ja existe.
    Nao toca em nenhuma outra empresa do banco - cada uma e um tenant isolado.

.PARAMETER ApiUrl
    Base da API. Padrao https://localhost:7271/api/v1 (perfil 'https' do launchSettings).

.PARAMETER BackofficeKey
    Valor de Backoffice:ApiKey. Se omitido, e lido de 'dotnet user-secrets'.

.PARAMETER Senha
    Senha dos dois usuarios. Minimo 6 caracteres, ao menos uma maiuscula e um numero.

.PARAMETER Recriar
    DESTRUTIVO: dropa e recria o banco 'frota360' inteiro antes de provisionar, apagando
    TODAS as empresas. Pede confirmacao explicita.

.EXAMPLE
    ./scripts/seed-dev.ps1
    Provisiona (ou reaproveita) a empresa de dev no banco atual.

.EXAMPLE
    ./scripts/seed-dev.ps1 -ApiUrl http://localhost:5062/api/v1
    Quando a API roda no perfil 'http'.
#>
[CmdletBinding()]
param(
    # O perfil 'https' publica 7271 e 5062, mas 5062 responde 307 (UseHttpsRedirection).
    [string] $ApiUrl         = 'https://localhost:7271/api/v1',
    [string] $BackofficeKey,
    [string] $NomeEmpresa    = 'Transportadora Dev',
    [string] $Cnpj           = '12345678000199',
    [string] $EmailAdmin     = 'admin@dev.com',
    [string] $EmailMotorista = 'motorista@dev.com',
    [string] $Senha          = 'SenhaForte123',
    [string] $Container      = 'pg-frota360',
    [switch] $Recriar
)

$ErrorActionPreference = 'Stop'
$raiz = Split-Path $PSScriptRoot -Parent

# Este arquivo e ASCII puro de proposito: o Windows PowerShell 5.1 le .ps1 sem BOM como
# ANSI, e qualquer acento ou travessao vira lixo no meio da execucao.

function Passo($n, $texto) { Write-Host "`n[$n] $texto" -ForegroundColor Cyan }
function Ok($texto)        { Write-Host "    $texto"     -ForegroundColor Green }
function Nota($texto)      { Write-Host "    $texto"     -ForegroundColor DarkGray }

# O ASP.NET usa certificado de desenvolvimento autoassinado, que o Invoke-RestMethod do
# PowerShell 5.1 recusa (nao existe -SkipCertificateCheck nesta versao). Confiar nele so
# faz sentido porque este script e exclusivo de desenvolvimento local.
if ($ApiUrl -like 'https://localhost*' -and -not ('TrustLocalDevCert' -as [type])) {
    Add-Type -TypeDefinition @'
using System.Net;
public static class TrustLocalDevCert {
    public static void Ativar() {
        ServicePointManager.ServerCertificateValidationCallback += (s, c, ch, e) => true;
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
    }
}
'@
    [TrustLocalDevCert]::Ativar()
}

Add-Type -AssemblyName System.Web

# O token vem URL-encoded dentro do linkConvite - precisa decodificar antes de reenviar.
function Get-TokenDoLink([string] $link) {
    [System.Web.HttpUtility]::UrlDecode(($link -split 'token=', 2)[1])
}

# Devolve a mensagem de erro da API (envelope ApiResponse) em vez do stack do PS.
# No PowerShell 5.1 o corpo da resposta de erro quase nunca chega em ErrorDetails: e
# preciso ler o stream da WebException na mao. Sem isto, um 422 aparece como o inutil
# "O servidor remoto retornou um erro: (422) Unprocessable Entity."
function Get-MensagemDeErro($erro) {
    $bruto = $erro.ErrorDetails.Message

    if (-not $bruto -and $erro.Exception.Response) {
        try {
            $stream = $erro.Exception.Response.GetResponseStream()
            $stream.Position = 0
            $leitor = New-Object System.IO.StreamReader($stream)
            $bruto = $leitor.ReadToEnd()
            $leitor.Dispose()
        } catch { }
    }

    if (-not $bruto) { return $erro.Exception.Message }

    try {
        $j = $bruto | ConvertFrom-Json
        $msg = $j.mensagem
        if ($j.erros) { $msg = $msg + ' (' + ($j.erros -join '; ') + ')' }
        return $msg
    } catch { return $bruto }
}

function Aceitar-Convite([string] $link, [string] $nome) {
    $corpo = @{ token = (Get-TokenDoLink $link); nome = $nome; senha = $Senha } | ConvertTo-Json
    (Invoke-RestMethod -Method Post -Uri "$ApiUrl/convite/aceitar" `
        -ContentType 'application/json' -Body $corpo).dados
}

# --- opcional: zerar o banco -------------------------------------------------
if ($Recriar) {
    Write-Host "ATENCAO: isto apaga TODAS as empresas e usuarios do banco 'frota360'." -ForegroundColor Yellow
    if ((Read-Host "Digite 'sim' para confirmar") -ne 'sim') { Write-Host 'Cancelado.'; exit 1 }

    Passo 0 'Recriando o banco e aplicando as migrations'
    docker exec $Container psql -U postgres -d postgres -c 'DROP DATABASE IF EXISTS frota360;' | Out-Null
    docker exec $Container psql -U postgres -d postgres -c 'CREATE DATABASE frota360;'          | Out-Null
    Push-Location (Join-Path $raiz 'apps/api')
    try { dotnet ef database update --project src/Infrastructure --startup-project src/Api | Out-Null }
    finally { Pop-Location }
    Ok 'banco recriado'
    Read-Host 'REINICIE a API e pressione Enter'
}

# --- a chave do backoffice ---------------------------------------------------
if (-not $BackofficeKey) {
    Push-Location (Join-Path $raiz 'apps/api')
    try {
        $linha = dotnet user-secrets list --project src/Api |
                 Where-Object { $_ -like 'Backoffice:ApiKey =*' }
    } finally { Pop-Location }

    if (-not $linha) {
        throw "Backoffice:ApiKey nao configurada. Rode: dotnet user-secrets set 'Backoffice:ApiKey' '<valor>' --project src/Api"
    }
    $BackofficeKey = ($linha -split ' = ', 2)[1]
}

# --- 1 e 2. empresa + Admin (ou login, se ja existir) ------------------------
Passo 1 "Provisionando a empresa '$NomeEmpresa'"
$corpo = @{ nomeEmpresa = $NomeEmpresa; cnpj = $Cnpj; emailAdmin = $EmailAdmin } | ConvertTo-Json
$empresa = $null
try {
    $empresa = (Invoke-RestMethod -Method Post -Uri "$ApiUrl/backoffice/empresa" `
        -ContentType 'application/json' -Headers @{ 'X-Backoffice-Key' = $BackofficeKey } `
        -Body $corpo).dados
    Ok "empresa #$($empresa.empresaId) criada, 10 tipos de manutencao semeados"
} catch {
    $msg = Get-MensagemDeErro $_
    if ($_.Exception.Response.StatusCode.value__ -ne 422) { throw "Falha ao provisionar: $msg" }
    Nota "empresa de dev ja existe ($msg) - reaproveitando"
}

Passo 2 "Entrando como Admin ($EmailAdmin)"
if ($empresa) {
    $admin = Aceitar-Convite $empresa.linkConvite 'Admin Dev'
    Ok "$($admin.email) criado - role $($admin.role)"
} else {
    $corpo = @{ email = $EmailAdmin; senha = $Senha } | ConvertTo-Json
    try {
        $admin = (Invoke-RestMethod -Method Post -Uri "$ApiUrl/auth/login" `
            -ContentType 'application/json' -Body $corpo).dados
    } catch {
        throw "O Admin '$EmailAdmin' existe mas a senha nao confere. Use -Recriar, ou passe -EmailAdmin/-Senha diferentes. Detalhe: $(Get-MensagemDeErro $_)"
    }
    Ok "$($admin.email) - login ok, role $($admin.role)"
}

# --- 3 e 4. Motorista --------------------------------------------------------
Passo 3 "Convidando o Motorista ($EmailMotorista)"
$corpo = @{ email = $EmailMotorista; role = 'Motorista' } | ConvertTo-Json
$convite = $null
try {
    $convite = (Invoke-RestMethod -Method Post -Uri "$ApiUrl/convite" `
        -ContentType 'application/json' -Headers @{ Authorization = "Bearer $($admin.token)" } `
        -Body $corpo).dados
    Ok 'convite emitido'
} catch {
    $msg = Get-MensagemDeErro $_
    if ($_.Exception.Response.StatusCode.value__ -ne 422) { throw "Falha ao convidar: $msg" }
    Nota "motorista ja existe ($msg) - pulando"
}

if ($convite) {
    Passo 4 'Criando o Motorista'
    $motorista = Aceitar-Convite $convite.linkConvite 'Motorista Dev'
    Ok "$($motorista.email) criado - role $($motorista.role)"
}

# --- resumo ------------------------------------------------------------------
Write-Host "`nPronto. Entre no front com:" -ForegroundColor Green
Write-Host ("  {0,-22} {1}  (Admin)"     -f $EmailAdmin,     $Senha)
Write-Host ("  {0,-22} {1}  (Motorista)" -f $EmailMotorista, $Senha)
Write-Host "`nO e-mail e normalizado para minusculas: qualquer caixa funciona no login."
