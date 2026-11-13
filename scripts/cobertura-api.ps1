<#
.SYNOPSIS
    Mede a cobertura de linhas do backend com as tres suites somadas, num relatorio so.

.DESCRIPTION
    Roda Frota360.Tests (unitarios), Frota360.IntegrationTests (Testcontainers - exige Docker)
    e Frota360.Api.Tests com o coverlet.runsettings, e junta os tres OpenCover com o
    ReportGenerator em apps/api/TestResults/relatorio/index.html.

    E o mesmo recorte do SonarCloud na pasta apps/api: migrations fora (runsettings) e
    Program.cs fora (filtro abaixo). O CI chama este script com -SomenteRelatorio depois dos
    seus proprios passos de teste, entao o numero local e o do CI sao o mesmo calculo.

    Termina com codigo 1 se a cobertura de linhas ficar abaixo de -Minimo.

.PARAMETER Minimo
    Piso de cobertura de linhas, em %. Padrao 75 (meta da prova de autoria).

.PARAMETER Configuracao
    Configuracao do build. Padrao Release, a mesma do CI.

.PARAMETER SomenteRelatorio
    Nao builda nem roda testes: consolida os coverage.opencover.xml que ja estao em TestResults.

.PARAMETER Abrir
    Abre o HTML no navegador ao final.

.EXAMPLE
    ./scripts/cobertura-api.ps1 -Abrir
#>
[CmdletBinding()]
param(
    [double] $Minimo = 75,
    [string] $Configuracao = 'Release',
    [switch] $SomenteRelatorio,
    [switch] $Abrir
)

$ErrorActionPreference = 'Stop'
$api = Join-Path (Split-Path $PSScriptRoot -Parent) 'apps/api'
$resultados = Join-Path $api 'TestResults'
$relatorio = Join-Path $resultados 'relatorio'

# Este arquivo e ASCII puro de proposito: o Windows PowerShell 5.1 le .ps1 sem BOM como
# ANSI, e qualquer acento vira lixo na saida.

function Invocar([string] $descricao, [scriptblock] $comando) {
    Write-Host "==> $descricao" -ForegroundColor Cyan
    & $comando
    if ($LASTEXITCODE -ne 0) { throw "Falhou: $descricao (codigo $LASTEXITCODE)." }
}

Push-Location $api
try {
    if (-not $SomenteRelatorio) {
        $ErrorActionPreference = 'Continue'
        docker info --format '{{.ServerVersion}}' *> $null
        $ErrorActionPreference = 'Stop'
        if ($LASTEXITCODE -ne 0) {
            throw 'Docker fora do ar: a suite de integracao sobe um postgres:17 via Testcontainers.'
        }

        if (Test-Path $resultados) { Remove-Item $resultados -Recurse -Force }

        Invocar 'Build' { dotnet build Frota360.slnx -c $Configuracao }

        foreach ($suite in 'Frota360.Tests', 'Frota360.IntegrationTests', 'Frota360.Api.Tests') {
            Invocar "Testes: $suite" {
                dotnet test "tests/$suite/$suite.csproj" -c $Configuracao --no-build `
                    --settings coverlet.runsettings --collect:'XPlat Code Coverage' `
                    --results-directory $resultados
            }
        }
    }

    $relatorios = @(Get-ChildItem $resultados -Recurse -Filter 'coverage.opencover.xml' -ErrorAction SilentlyContinue)
    if ($relatorios.Count -ne 3) {
        throw "Esperava 3 coverage.opencover.xml em TestResults (um por suite), achei $($relatorios.Count)."
    }

    Invocar 'Restaurar ReportGenerator' { dotnet tool restore }

    # Program.cs: raiz de composicao, cuja ultima linha (await app.RunAsync()) nao e
    # alcancavel por teste. Mesma exclusao do sonar.coverage.exclusions no CI.
    Invocar 'Consolidar relatorios' {
        dotnet tool run reportgenerator `
            "-reports:$resultados/*/coverage.opencover.xml" `
            "-targetdir:$relatorio" `
            '-reporttypes:Html;JsonSummary;MarkdownSummaryGithub;TextSummary' `
            '-filefilters:-*Program.cs' `
            '-title:Frota360 API' `
            '-verbosity:Warning'
    }

    $resumo = (Get-Content (Join-Path $relatorio 'Summary.json') -Raw | ConvertFrom-Json).summary
    $linhas = [double] $resumo.linecoverage

    Write-Host ''
    Get-Content (Join-Path $relatorio 'Summary.txt') -TotalCount 20 | Write-Host
    Write-Host ''
    Write-Host ("Cobertura de linhas do backend: {0}% ({1} de {2} linhas). Minimo: {3}%." -f `
        $linhas, $resumo.coveredlines, $resumo.coverablelines, $Minimo) -ForegroundColor Green
    Write-Host "Relatorio: $(Join-Path $relatorio 'index.html')"

    if ($Abrir) { Invoke-Item (Join-Path $relatorio 'index.html') }

    if ($linhas -lt $Minimo) {
        Write-Host "Abaixo do minimo de $Minimo%." -ForegroundColor Red
        exit 1
    }
}
finally {
    Pop-Location
}
