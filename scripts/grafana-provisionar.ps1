<#
.SYNOPSIS
    Cria ou atualiza no Grafana o painel e as regras de alerta do Frota360, a partir de infra/grafana.

.DESCRIPTION
    Monitoramento como codigo: o repositorio e a fonte, e o Grafana so recebe. Roda quantas vezes
    quiser - cada execucao sobrescreve painel, contato e regras com o que esta nos arquivos.

    1. Descobre os data sources Prometheus e Loki da stack (grafanacloud-prom/-logs no Grafana
       Cloud, prometheus/loki no grafana/otel-lgtm local).
    2. Garante a pasta "Frota360".
    3. Publica infra/grafana/painel-frota360.json.
    4. Garante o contato de e-mail "frota360-email" (com -EmailAlertas).
    5. Substitui o grupo de regras "frota360-api" por infra/grafana/alertas-frota360.json.

    As regras ficam com proveniencia de API: a interface do Grafana nao deixa edita-las, para o
    repositorio continuar sendo a unica fonte. Mudou um limite? Altere o JSON e rode de novo.

    Autenticacao: em localhost usa admin/admin (o padrao do grafana/otel-lgtm). Fora dele, um
    token de service account com papel Admin - a API de provisionamento de alertas nao aceita
    Editor - na variavel GRAFANA_TOKEN (do processo ou do
    usuario do Windows) - o token nunca e aceito como parametro, para nao ficar no historico.

.PARAMETER Url
    Endereco do Grafana. Padrao: GRAFANA_URL ou http://localhost:3000.

.PARAMETER EmailAlertas
    Destino dos alertas. Obrigatorio na primeira execucao numa stack; depois, se omitido, o
    contato existente e mantido. Nao e versionado de proposito: e dado pessoal.

.EXAMPLE
    ./scripts/grafana-provisionar.ps1 -EmailAlertas voce@exemplo.com
    # Grafana local (docker compose --profile observabilidade up -d)

.EXAMPLE
    ./scripts/grafana-provisionar.ps1 -Url https://<stack>.grafana.net -EmailAlertas voce@exemplo.com
#>
[CmdletBinding()]
param(
    [string] $Url = $(if ($env:GRAFANA_URL) { $env:GRAFANA_URL } else { 'http://localhost:3000' }),
    [string] $EmailAlertas
)

$ErrorActionPreference = 'Stop'
$raiz = Split-Path $PSScriptRoot -Parent
$pastaUid = 'frota360'
$grupo = 'frota360-api'
$contato = 'frota360-email'
$Url = $Url.TrimEnd('/')

function Obter-Cabecalhos {
    if ($Url -match '^https?://(localhost|127\.0\.0\.1)(:|/|$)') {
        return @{ Authorization = 'Basic ' + [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes('admin:admin')) }
    }

    $token = $env:GRAFANA_TOKEN
    if (-not $token) { $token = [Environment]::GetEnvironmentVariable('GRAFANA_TOKEN', 'User') }
    if (-not $token) { throw 'Defina GRAFANA_TOKEN com um token de service account (papel Admin) da stack.' }
    if ($token -match '[<>\s"'']') { throw 'GRAFANA_TOKEN contem <, >, espaco ou aspas - sobras do exemplo. Grave so o valor que comeca com glsa_.' }

    return @{ Authorization = "Bearer $token" }
}

$cabecalhos = Obter-Cabecalhos

function Chamar([string] $metodo, [string] $caminho, $corpo = $null) {
    $parametros = @{ Method = $metodo; Uri = "$Url$caminho"; Headers = $cabecalhos; UseBasicParsing = $true }
    if ($null -ne $corpo) {
        $json = if ($corpo -is [string]) { $corpo } else { $corpo | ConvertTo-Json -Depth 50 -Compress }
        # Bytes em UTF-8: o PowerShell 5.1 codificaria a string em ISO-8859-1 e estragaria os acentos.
        $parametros.Body = [Text.Encoding]::UTF8.GetBytes($json)
        $parametros.ContentType = 'application/json; charset=utf-8'
    }
    try { $resposta = Invoke-WebRequest @parametros }
    catch {
        $status = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        $dica = if ($status -eq 403 -and $caminho -like '/api/v1/provisioning/*') { ' A API de provisionamento de alertas exige uma service account com papel Admin.' } else { '' }
        throw "$metodo $caminho respondeu $status. $($_.ErrorDetails.Message)$dica"
    }
    if ($resposta.Content) { return [Text.Encoding]::UTF8.GetString($resposta.RawContentStream.ToArray()) | ConvertFrom-Json }
}

function Escolher-DataSource($lista, [string] $tipo, [string[]] $preferidos) {
    foreach ($uid in $preferidos) {
        $ds = $lista | Where-Object { $_.uid -eq $uid -and $_.type -eq $tipo }
        if ($ds) { return $ds.uid }
    }
    $ds = $lista | Where-Object { $_.type -eq $tipo } | Select-Object -First 1
    if (-not $ds) { throw "Nenhum data source do tipo $tipo em $Url." }
    return $ds.uid
}

function Ler-Modelo([string] $arquivo, [hashtable] $trocas) {
    $texto = Get-Content (Join-Path $raiz "infra/grafana/$arquivo") -Raw -Encoding UTF8
    foreach ($chave in $trocas.Keys) { $texto = $texto.Replace('${' + $chave + '}', $trocas[$chave]) }
    return $texto
}

Write-Host "==> Grafana em $Url"
$dataSources = @(Chamar GET '/api/datasources') | ForEach-Object { $_ }
$prometheus = Escolher-DataSource $dataSources 'prometheus' @('grafanacloud-prom', 'prometheus')
$loki = Escolher-DataSource $dataSources 'loki' @('grafanacloud-logs', 'loki')
Write-Host "    Prometheus: $prometheus | Loki: $loki"

Write-Host '==> Pasta Frota360'
# Pela listagem, e nao por GET /api/folders/<uid>: o Grafana Cloud responde 403 (e nao 404) para
# pasta inexistente, para nao revelar quais existem.
$pastas = @(Chamar GET '/api/folders') | ForEach-Object { $_ }
if (-not ($pastas | Where-Object { $_.uid -eq $pastaUid })) {
    Chamar POST '/api/folders' @{ uid = $pastaUid; title = 'Frota360' } | Out-Null
}

Write-Host '==> Painel'
$painel = Ler-Modelo 'painel-frota360.json' @{ DS_PROMETHEUS = $prometheus; DS_LOKI = $loki }
$publicacao = '{"dashboard":' + $painel + ',"folderUid":"' + $pastaUid + '","overwrite":true,"message":"scripts/grafana-provisionar.ps1"}'
$resultado = Chamar POST '/api/dashboards/db' $publicacao
Write-Host "    $Url$($resultado.url)"

Write-Host '==> Contato de alerta'
$existentes = @(Chamar GET '/api/v1/provisioning/contact-points') | ForEach-Object { $_ } | Where-Object { $_.name -eq $contato }
if ($EmailAlertas) {
    $ponto = @{ name = $contato; type = 'email'; settings = @{ addresses = $EmailAlertas; singleEmail = $true }; disableResolveMessage = $false }
    if ($existentes) { Chamar PUT "/api/v1/provisioning/contact-points/$($existentes[0].uid)" $ponto | Out-Null }
    else { Chamar POST '/api/v1/provisioning/contact-points' $ponto | Out-Null }
    Write-Host "    $contato -> e-mail atualizado"
}
elseif (-not $existentes) {
    throw "O contato $contato ainda nao existe nesta stack: rode de novo com -EmailAlertas <endereco>."
}
else { Write-Host "    $contato mantido" }

Write-Host '==> Regras de alerta'
$regras = Ler-Modelo 'alertas-frota360.json' @{ DS_PROMETHEUS = $prometheus; CONTATO = $contato } | ConvertFrom-Json
foreach ($regra in $regras.rules) {
    $regra | Add-Member -NotePropertyName folderUID -NotePropertyValue $pastaUid -Force
    $regra | Add-Member -NotePropertyName ruleGroup -NotePropertyValue $grupo -Force
}
Chamar PUT "/api/v1/provisioning/folder/$pastaUid/rule-groups/$grupo" $regras | Out-Null
$regras.rules | ForEach-Object { Write-Host "    $($_.title)" }

Write-Host 'Pronto.'
