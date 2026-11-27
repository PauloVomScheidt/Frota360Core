<#
.SYNOPSIS
    Popula uma empresa vazia com uma massa de dados de demonstração, pela API pública.

.DESCRIPTION
    Entra como o Admin de uma empresa já provisionada e simula alguns meses de operação de
    uma transportadora da Grande Florianópolis: frota de 10 veículos, 7 motoristas e 1
    supervisor, rotas diárias, abastecimentos tanque a tanque, manutenções preventivas e
    despesas avulsas. Termina com duas rotas abertas, manutenções atrasadas e vencendo,
    para o dashboard ter o que mostrar.

    Tudo passa pelos endpoints, então as regras de negócio valem como em qualquer
    lançamento: o odômetro só avança, o valor do abastecimento é calculado no servidor e
    cada escrita fica na auditoria em nome do Admin.

    Os convites disparam e-mail de verdade. Por isso os usuários são derivados de um
    e-mail que você controla, com subendereçamento: fulano@gmail.com vira
    fulano+motorista1@gmail.com, e todos chegam na sua caixa.

    A simulação usa semente fixa: a mesma semente gera sempre os mesmos dados.
    Use -Simular antes para ver o volume sem tocar na API.

.PARAMETER ApiUrl
    Base da API. Produção: https://api.frota360app.com.br/api/v1

.PARAMETER EmailAdmin
    Login do Admin da empresa a popular. A senha é pedida na hora.

.PARAMETER EmailBase
    E-mail que você controla, base dos usuários criados (fulano+motorista1@gmail.com).

.PARAMETER Meses
    Meses de histórico até ontem. Padrão 4.

.PARAMETER SenhaAdmin
    Opcional. Sem ela, a senha é pedida na hora.

.PARAMETER SenhaUsuarios
    Opcional. Senha dos usuários criados; sem ela, é pedida na hora.

.PARAMETER Confirmar
    Pula a confirmação final (para execução sem terminal interativo).

.PARAMETER Simular
    Só calcula e mostra quantos lançamentos seriam feitos, sem chamar a API.

.EXAMPLE
    ./scripts/seed-demo.ps1 -Simular

.EXAMPLE
    ./scripts/seed-demo.ps1 -EmailAdmin admin@dev.com -EmailBase fulano@gmail.com

.EXAMPLE
    ./scripts/seed-demo.ps1 -ApiUrl https://api.frota360app.com.br/api/v1 -EmailAdmin voce@empresa.com -EmailBase fulano@gmail.com
#>
[CmdletBinding()]
param(
    [string] $ApiUrl = 'https://localhost:7271/api/v1',
    [string] $EmailAdmin,
    [string] $EmailBase,
    [ValidateRange(1, 12)] [int] $Meses = 4,
    [int]    $Semente = 360,
    [Security.SecureString] $SenhaAdmin,
    [Security.SecureString] $SenhaUsuarios,
    [switch] $Simular,
    [switch] $Confirmar
)

$ErrorActionPreference = 'Stop'

# Arquivo salvo em UTF-8 com BOM: sem o BOM o Windows PowerShell 5.1 lê como ANSI e os
# acentos dos nomes viram lixo no banco.

function Passo($texto) { Write-Host "`n$texto" -ForegroundColor Cyan }
function Ok($texto)    { Write-Host "    $texto" -ForegroundColor Green }
function Nota($texto)  { Write-Host "    $texto" -ForegroundColor DarkGray }

if (-not $Simular) {
    if (-not $EmailAdmin) { throw 'Informe -EmailAdmin (ou use -Simular).' }
    if ($EmailBase -notmatch '^[^@\s]+@[^@\s]+$') { throw 'Informe -EmailBase com um e-mail válido que você controla.' }
}

if ($ApiUrl -like 'https://localhost*' -and -not ('TrustLocalDevCert' -as [type])) {
    Add-Type -TypeDefinition @'
using System.Net;
public static class TrustLocalDevCert {
    public static void Ativar() {
        ServicePointManager.ServerCertificateValidationCallback += (s, c, ch, e) => true;
    }
}
'@
    [TrustLocalDevCert]::Ativar()
}
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Add-Type -AssemblyName System.Web

# ============================================================================
# Cliente HTTP
# ============================================================================

$script:token = $null
$script:ultimaChamada = [DateTime]::MinValue
$script:chamadas = 0
$script:idFalso = 0

function Get-MensagemDeErro($erro) {
    $bruto = $erro.ErrorDetails.Message
    if (-not $bruto -and $erro.Exception.Response) {
        try {
            $stream = $erro.Exception.Response.GetResponseStream()
            $stream.Position = 0
            $leitor = New-Object System.IO.StreamReader($stream, [Text.Encoding]::UTF8)
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

function Get-Status($erro) {
    if ($erro.Exception.Response) { return [int]$erro.Exception.Response.StatusCode }
    return 0
}

function Ler-Corpo($resposta) {
    $texto = [Text.Encoding]::UTF8.GetString($resposta.RawContentStream.ToArray())
    if (-not $texto) { return $null }
    return $texto | ConvertFrom-Json
}

# O limite global da API é 200 req/min por IP; 320 ms entre chamadas fica abaixo dele.
# Login e aceite de convite caem no limite "auth" (5/min): o 429 espera a janela virar.
function Enviar([string] $metodo, [string] $caminho, $corpo, [bool] $autenticado) {
    for ($tentativa = 1; ; $tentativa++) {
        $espera = 320 - ((Get-Date) - $script:ultimaChamada).TotalMilliseconds
        if ($espera -gt 0) { Start-Sleep -Milliseconds ([int]$espera) }
        $script:ultimaChamada = Get-Date

        $p = @{ Method = $metodo; Uri = "$ApiUrl$caminho"; UseBasicParsing = $true }
        if ($autenticado) { $p.Headers = @{ Authorization = "Bearer $script:token" } }
        if ($null -ne $corpo) {
            $p.ContentType = 'application/json; charset=utf-8'
            $p.Body = [Text.Encoding]::UTF8.GetBytes((ConvertTo-Json $corpo -Depth 6 -Compress))
        }

        try {
            $resposta = Invoke-WebRequest @p
            $script:chamadas++
            return $resposta
        } catch {
            $status = Get-Status $_
            if ($status -eq 429 -and $tentativa -le 6) {
                Nota 'limite de requisições da API atingido - aguardando 61s'
                Start-Sleep -Seconds 61
                continue
            }
            if ($status -eq 401 -and $autenticado -and $tentativa -eq 1) {
                Entrar | Out-Null
                continue
            }
            $erro = New-Object Exception("$metodo $caminho -> $status`: $(Get-MensagemDeErro $_)")
            $erro.Data['Status'] = $status
            throw $erro
        }
    }
}

function Api([string] $metodo, [string] $caminho, $corpo = $null) {
    if ($Simular) {
        $script:chamadas++
        $script:idFalso++
        return [pscustomobject]@{ id = $script:idFalso }
    }
    (Ler-Corpo (Enviar $metodo $caminho $corpo $true)).dados
}

function Entrar {
    $corpo = @{ email = $EmailAdmin; senha = $script:senhaAdminTexto }
    try { $resposta = Enviar 'Post' '/auth/login' $corpo $false }
    catch { throw "Login como $EmailAdmin falhou: $($_.Exception.Message)" }

    $cookies = $resposta.Headers['Set-Cookie'] -join ';'
    if ($cookies -notmatch 'frota360_token=([^;,\s]+)') { throw 'A API não devolveu o cookie de sessão no login.' }
    $script:token = $Matches[1]
    (Ler-Corpo $resposta).dados
}

function Texto-Plano([Security.SecureString] $s) {
    [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($s))
}

# ============================================================================
# Aleatoriedade determinística e utilidades
# ============================================================================

$rng = New-Object Random($Semente)
function Sortear([double] $min, [double] $max) { $min + $rng.NextDouble() * ($max - $min) }
function SortearInt([int] $min, [int] $max)     { $rng.Next($min, $max + 1) }
function Chance([double] $p)                    { $rng.NextDouble() -lt $p }
function Escolher($lista)                       { $lista[$rng.Next($lista.Count)] }
function Embaralhar($lista)                     { $lista | Sort-Object { $rng.Next() } }
function Dinheiro([double] $v)                  { [decimal][Math]::Round($v, 2) }
function Data([DateTime] $d)                    { $d.ToString('yyyy-MM-ddTHH:mm:ss', [Globalization.CultureInfo]::InvariantCulture) }

function Distancia-Km($a, $b) {
    $r = 6371
    $dLat = ($b.Lat - $a.Lat) * [Math]::PI / 180
    $dLon = ($b.Lon - $a.Lon) * [Math]::PI / 180
    $h = [Math]::Sin($dLat / 2) * [Math]::Sin($dLat / 2) +
         [Math]::Cos($a.Lat * [Math]::PI / 180) * [Math]::Cos($b.Lat * [Math]::PI / 180) *
         [Math]::Sin($dLon / 2) * [Math]::Sin($dLon / 2)
    2 * $r * [Math]::Asin([Math]::Sqrt($h))
}

function Email-Derivado([string] $sufixo) {
    $local, $dominio = $EmailBase -split '@', 2
    "$local+$sufixo@$dominio".ToLowerInvariant()
}

function Nova-Placa {
    $letras = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'
    -join @(
        $letras[$rng.Next(26)], $letras[$rng.Next(26)], $letras[$rng.Next(26)],
        $rng.Next(10), $letras[$rng.Next(26)], $rng.Next(10), $rng.Next(10)
    )
}

# ============================================================================
# Cenário
# ============================================================================

$deposito = @{ Endereco = 'Rua Gerônimo José Dias, 1200 - Barreiros, São José/SC'; Lat = -27.5765; Lon = -48.6105 }

$locaisUrbanos = @(
    @{ Endereco = 'Av. Beira-Mar Norte, 2500 - Centro, Florianópolis/SC';      Lat = -27.5872; Lon = -48.5470 }
    @{ Endereco = 'Rod. SC-401, 4100 - Saco Grande, Florianópolis/SC';         Lat = -27.5420; Lon = -48.5030 }
    @{ Endereco = 'Av. Madre Benvenuta, 1500 - Trindade, Florianópolis/SC';    Lat = -27.5880; Lon = -48.5150 }
    @{ Endereco = 'Rua Koesa, 300 - Kobrasol, São José/SC';                    Lat = -27.5960; Lon = -48.6160 }
    @{ Endereco = 'Av. Barão do Rio Branco, 900 - Centro, Palhoça/SC';         Lat = -27.6455; Lon = -48.6697 }
    @{ Endereco = 'Rua Getúlio Vargas, 200 - Centro, Biguaçu/SC';              Lat = -27.4940; Lon = -48.6550 }
    @{ Endereco = 'Rod. Admar Gonzaga, 1800 - Itacorubi, Florianópolis/SC';    Lat = -27.5800; Lon = -48.5000 }
    @{ Endereco = 'Av. Pequeno Príncipe, 600 - Campeche, Florianópolis/SC';    Lat = -27.6780; Lon = -48.4870 }
)

# Pracas = praças de pedágio só de ida.
$locaisRegionais = @(
    @{ Endereco = 'Rod. BR-101, km 118 - Cordeiros, Itajaí/SC';               Lat = -26.9100; Lon = -48.6900; Pracas = 1 }
    @{ Endereco = 'Rua XV de Novembro, 800 - Centro, Blumenau/SC';            Lat = -26.9194; Lon = -49.0661; Pracas = 1 }
    @{ Endereco = 'Av. Santos Dumont, 3000 - Zona Industrial, Joinville/SC';  Lat = -26.2650; Lon = -48.8440; Pracas = 2 }
    @{ Endereco = 'Av. Centenário, 1500 - Centro, Criciúma/SC';               Lat = -28.6800; Lon = -49.3700; Pracas = 1 }
    @{ Endereco = 'Rua Coronel Córdova, 400 - Centro, Lages/SC';              Lat = -27.8150; Lon = -50.3260; Pracas = 0 }
    @{ Endereco = 'Rod. BR-277, km 80 - Cidade Industrial, Curitiba/PR';      Lat = -25.4370; Lon = -49.2700; Pracas = 3 }
    @{ Endereco = 'Av. Assis Brasil, 5000 - Sarandi, Porto Alegre/RS';        Lat = -29.9900; Lon = -51.1200; Pracas = 3 }
    @{ Endereco = 'Rua Marechal Deodoro, 300 - Centro, Chapecó/SC';           Lat = -27.1000; Lon = -52.6150; Pracas = 0 }
)

# Kml = consumo médio real; Mult escala os custos de manutenção e despesa.
$frota = @(
    @{ Nome = 'Fiorino Furgão';  Marca = 'Fiat';          Classe = 'leve';   Comb = 'Gasolina comum'; Kml = 11.5; Tanque = 50;  KmIni = 38000;  Eixos = 2; Mult = 1.0 }
    @{ Nome = 'Strada Freedom';  Marca = 'Fiat';          Classe = 'leve';   Comb = 'Etanol';         Kml = 8.6;  Tanque = 55;  KmIni = 21000;  Eixos = 2; Mult = 1.0 }
    @{ Nome = 'Saveiro Robust';  Marca = 'Volkswagen';    Classe = 'leve';   Comb = 'Gasolina comum'; Kml = 11.0; Tanque = 55;  KmIni = 64000;  Eixos = 2; Mult = 1.0 }
    @{ Nome = 'Master Furgão';   Marca = 'Renault';       Classe = 'van';    Comb = 'Diesel S10';     Kml = 9.6;  Tanque = 80;  KmIni = 87000;  Eixos = 2; Mult = 1.5 }
    @{ Nome = 'Sprinter 417';    Marca = 'Mercedes-Benz'; Classe = 'van';    Comb = 'Diesel S10';     Kml = 9.0;  Tanque = 75;  KmIni = 112000; Eixos = 2; Mult = 1.5 }
    @{ Nome = 'Daily 35-160';    Marca = 'Iveco';         Classe = 'van';    Comb = 'Diesel S10';     Kml = 7.8;  Tanque = 90;  KmIni = 54000;  Eixos = 2; Mult = 1.6 }
    @{ Nome = 'Accelo 1017';     Marca = 'Mercedes-Benz'; Classe = 'medio';  Comb = 'Diesel S10';     Kml = 5.6;  Tanque = 150; KmIni = 143000; Eixos = 2; Mult = 2.6 }
    @{ Nome = 'Delivery 11.180'; Marca = 'Volkswagen';    Classe = 'medio';  Comb = 'Diesel S10';     Kml = 5.2;  Tanque = 150; KmIni = 98000;  Eixos = 2; Mult = 2.6 }
    @{ Nome = 'Atego 2430';      Marca = 'Mercedes-Benz'; Classe = 'pesado'; Comb = 'Diesel S10';     Kml = 3.3;  Tanque = 300; KmIni = 231000; Eixos = 3; Mult = 4.0 }
    @{ Nome = 'FH 540';          Marca = 'Volvo';         Classe = 'pesado'; Comb = 'Diesel S10';     Kml = 2.6;  Tanque = 600; KmIni = 318000; Eixos = 6; Mult = 4.5 }
)

$velocidade = @{ leve = 28; van = 35; medio = 55; pesado = 60 }

$nomesMotoristas = @('Carlos Eduardo Souza', 'Mariana Lopes', 'João Pedro Almeida', 'Rafael Costa',
                     'Fernanda Ribeiro', 'Lucas Martins', 'Anderson da Silva')
$nomeSupervisor  = 'Patrícia Moreira'
$frentistas      = @('Jorge', 'Luana', 'Diego', 'Sabrina', 'Marcos', 'Vitória')

$postos = @(
    @{ Nome = 'Auto Posto Kobrasol';     Cidade = 'São José/SC'; Urbano = $true;  Ajuste = 0.00 }
    @{ Nome = 'Posto Via Expressa';      Cidade = 'São José/SC'; Urbano = $true;  Ajuste = 0.06 }
    @{ Nome = 'Posto Trevo de Palhoça';  Cidade = 'Palhoça/SC';  Urbano = $true;  Ajuste = -0.04 }
    @{ Nome = 'Posto Litoral Norte';     Cidade = 'Itajaí/SC';   Urbano = $false; Ajuste = -0.07 }
    @{ Nome = 'Posto Serra Catarinense'; Cidade = 'Lages/SC';    Urbano = $false; Ajuste = 0.09 }
)

$precoBase = @{ 'Diesel S10' = 6.09; 'Gasolina comum' = 6.29; 'Etanol' = 4.19 }

# Intervalo do catálogo padrão; caminhão roda mais entre trocas.
$preventivas = @(
    @{ Tipo = 'Troca de óleo';               Intervalo = 10000; Custo = @(260, 380) }
    @{ Tipo = 'Alinhamento e balanceamento'; Intervalo = 10000; Custo = @(140, 220) }
    @{ Tipo = 'Revisão geral';               Intervalo = 20000; Custo = @(900, 1500) }
    @{ Tipo = 'Troca de pastilhas de freio'; Intervalo = 30000; Custo = @(320, 480) }
)
$fatorIntervalo = @{ leve = 1.0; van = 1.0; medio = 1.5; pesado = 2.0 }

$multas = @(
    @{ Valor = 130.16; Obs = 'Excesso de velocidade em até 20%' }
    @{ Valor = 195.23; Obs = 'Estacionamento em local proibido' }
    @{ Valor = 293.47; Obs = 'Avanço de sinal vermelho' }
    @{ Valor = 88.38;  Obs = 'Parada em local proibido' }
)

$hoje   = (Get-Date).Date
$inicio = $hoje.AddMonths(-$Meses)
# Os dois veículos que ficam com a preventiva atrasada no fim da simulação.
$veiculosQueAtrasam = @('Saveiro Robust', 'Delivery 11.180')

# ============================================================================
# 1. Sessão e verificação
# ============================================================================

if ($Simular) {
    Passo "SIMULAÇÃO - nenhuma chamada à API será feita ($Meses meses, semente $Semente)"
} else {
    Passo "Entrando como $EmailAdmin em $ApiUrl"
    if (-not $SenhaAdmin) { $SenhaAdmin = Read-Host "Senha de $EmailAdmin" -AsSecureString }
    $script:senhaAdminTexto = Texto-Plano $SenhaAdmin
    $sessao = Entrar
    if ($sessao.role -ne 'Admin') { throw "O usuário $EmailAdmin é $($sessao.role); o script precisa de um Admin." }
    Ok "empresa: $($sessao.nomeEmpresa)"

    $existentes = @(Api 'Get' '/veiculo' | Where-Object { $_ })
    if ($existentes.Count -gt 0) {
        throw "A empresa '$($sessao.nomeEmpresa)' já tem $($existentes.Count) veículo(s). Este script é para empresa vazia."
    }

    if (-not $SenhaUsuarios) { $SenhaUsuarios = Read-Host 'Senha para os motoristas e o supervisor (mín. 6, com maiúscula e número)' -AsSecureString }
    $script:senhaUsuariosTexto = Texto-Plano $SenhaUsuarios
    if ($script:senhaUsuariosTexto -notmatch '^(?=.*[A-Z])(?=.*\d).{6,}$') { throw 'A senha precisa de 6+ caracteres, uma maiúscula e um número.' }

    Write-Host "`nIsto vai gravar alguns milhares de registros em '$($sessao.nomeEmpresa)'." -ForegroundColor Yellow
    if (-not $Confirmar -and (Read-Host "Digite 'sim' para continuar") -ne 'sim') { Write-Host 'Cancelado.'; exit 1 }
}

# ============================================================================
# 2. Catálogos
# ============================================================================

function Mapa-Por-Nome([string] $caminho) {
    $mapa = @{}
    if ($Simular) { return $mapa }
    foreach ($item in @(Api 'Get' $caminho)) { $mapa[$item.nome] = $item.id }
    $mapa
}

function Id-Do-Catalogo($mapa, [string] $nome, [string] $caminho) {
    if (-not $mapa.ContainsKey($nome)) {
        $mapa[$nome] = (Api 'Post' $caminho @{ nome = $nome }).id
    }
    $mapa[$nome]
}

Passo 'Catálogos'
$tiposCombustivel = Mapa-Por-Nome '/tipocombustivel'
$tiposDespesa     = Mapa-Por-Nome '/tipodespesa'
$tiposManutencao  = Mapa-Por-Nome '/tipomanutencao'

foreach ($c in $precoBase.Keys)  { Id-Do-Catalogo $tiposCombustivel $c '/tipocombustivel' | Out-Null }
foreach ($d in @('Pedágio', 'Multa de trânsito', 'Seguro', 'Lavagem', 'Estacionamento', 'IPVA')) {
    Id-Do-Catalogo $tiposDespesa $d '/tipodespesa' | Out-Null
}
foreach ($m in ($preventivas.Tipo + 'Troca de filtro de ar', 'Troca de pneus', 'Troca de bateria')) {
    if (-not $tiposManutencao.ContainsKey($m)) {
        $tiposManutencao[$m] = (Api 'Post' '/tipomanutencao' @{ nome = $m }).id
    }
}

foreach ($p in $postos) {
    $p.Id = (Api 'Post' '/posto' @{ nome = $p.Nome; cidade = $p.Cidade }).id
}
Ok "$($postos.Count) postos credenciados"

# ============================================================================
# 3. Usuários
# ============================================================================

function Convidar([string] $email, [string] $role, [string] $nome) {
    if ($Simular) { $script:chamadas += 2; return }
    try {
        $convite = Api 'Post' '/convite' @{ email = $email; role = $role }
    } catch {
        if ($_.Exception.Data['Status'] -eq 422) { Nota "$email já existe - mantido"; return }
        throw
    }
    $token = [System.Web.HttpUtility]::UrlDecode(($convite.linkConvite -split 'token=', 2)[1])
    Enviar 'Post' '/convite/aceitar' @{ token = $token; nome = $nome; senha = $script:senhaUsuariosTexto } $false | Out-Null
    Ok "$nome ($role) - $email"
}

Passo 'Usuários (o limite de 5 aceites por minuto pode pausar esta etapa)'
for ($i = 0; $i -lt $nomesMotoristas.Count; $i++) {
    Convidar (Email-Derivado "motorista$($i + 1)") 'Motorista' $nomesMotoristas[$i]
}
Convidar (Email-Derivado 'supervisor') 'Supervisor' $nomeSupervisor

$motoristas = @()
if ($Simular) {
    for ($i = 0; $i -lt $nomesMotoristas.Count; $i++) {
        $motoristas += @{ Id = $i + 1; Nome = $nomesMotoristas[$i]; Livre = $inicio }
    }
} else {
    $emails = 1..$nomesMotoristas.Count | ForEach-Object { Email-Derivado "motorista$_" }
    foreach ($m in @(Api 'Get' '/motorista')) {
        if ($emails -contains $m.email.ToLowerInvariant()) {
            $motoristas += @{ Id = $m.id; Nome = $m.nome; Livre = $inicio }
        }
    }
    if ($motoristas.Count -eq 0) { throw 'Nenhum motorista encontrado depois dos convites.' }
}

# ============================================================================
# 4. Frota
# ============================================================================

Passo 'Frota'
foreach ($v in $frota) {
    $v.Placa = Nova-Placa
    $v.Km = $v.KmIni + (SortearInt -3000 3000)
    $v.Id = (Api 'Post' '/veiculo' @{
        nomeVeiculo = $v.Nome; marcaVeiculo = $v.Marca; placa = $v.Placa; quilometragem = $v.Km
    }).id
    $v.Livre = $inicio
    $v.Litros = $v.Tanque * (Sortear 0.4 0.9)
    $v.UltimaLavagem = $inicio.AddDays((SortearInt 0 25))
    $v.Pendentes = @()

    foreach ($prev in $preventivas) {
        $intervalo = [int]($prev.Intervalo * $fatorIntervalo[$v.Classe])
        $prevista = [int]([Math]::Round(($v.Km + $intervalo * (Sortear 0.08 1.0)) / 100) * 100)
        $id = (Api 'Post' '/manutencao' @{
            veiculoId = $v.Id; tipoManutencaoId = $tiposManutencao[$prev.Tipo]; quilometragemPrevista = $prevista
        }).id
        $v.Pendentes += @{ Id = $id; Prev = $prev; Intervalo = $intervalo; Prevista = $prevista; Folga = (SortearInt 0 400) }
    }
    Ok "$($v.Placa)  $($v.Marca) $($v.Nome)  $($v.Km) km"
}

# ============================================================================
# 5. Operação dia a dia
# ============================================================================

$contagem = [ordered]@{ Rotas = 0; Abastecimentos = 0; Manutencoes = 0; Despesas = 0 }

function Preco-Do-Litro([string] $comb, $posto, [DateTime] $dia) {
    $tendencia = ($dia - $inicio).TotalDays * 0.0018
    [decimal][Math]::Round($precoBase[$comb] + $posto.Ajuste + $tendencia + (Sortear -0.04 0.04), 2)
}

function Lancar-Despesa($v, [string] $tipo, [double] $valor, [DateTime] $data, $motoristaId, [string] $obs) {
    $corpo = @{ veiculoId = $v.Id; tipoDespesaId = $tiposDespesa[$tipo]; valor = (Dinheiro $valor); dataDespesa = (Data $data) }
    if ($motoristaId) { $corpo.motoristaId = $motoristaId }
    if ($obs) { $corpo.observacao = $obs }
    Api 'Post' '/despesa' $corpo | Out-Null
    $contagem.Despesas++
}

function Abrir-Rota($v, $m, [DateTime] $saida, $destino) {
    $rota = Api 'Post' '/rota' @{
        enderecoPartida = $deposito.Endereco; enderecoChegada = $destino.Endereco
        latitudePartida = $deposito.Lat;      longitudePartida = $deposito.Lon
        latitudeChegada = $destino.Lat;       longitudeChegada = $destino.Lon
        codigoMotorista = $m.Id; codigoVeiculo = $v.Id
        dataInicio = (Data $saida); kmInicial = $v.Km
    }
    $contagem.Rotas++
    $rota.id
}

# Consome o percurso e reabastece quando o tanque chega à reserva, gravando o odômetro
# do ponto do reabastecimento: é isso que deixa o km/l tanque a tanque coerente.
function Rodar($v, $m, [int] $km, [DateTime] $saida, [double] $horas) {
    $kml = $v.Kml * (Sortear 0.93 1.07)
    $percorrido = 0
    while ($true) {
        $restante = $km - $percorrido
        if ($v.Litros - $restante / $kml -ge 0.22 * $v.Tanque) {
            $v.Litros -= $restante / $kml
            break
        }
        $ate = [Math]::Max(5, [int](($v.Litros - (Sortear 0.12 0.22) * $v.Tanque) * $kml))
        $ate = [Math]::Min($ate, $restante - 1)
        $percorrido += $ate
        $v.Litros -= $ate / $kml

        $postosPossiveis = @($postos | Where-Object { $_.Urbano -or $v.Classe -in 'medio', 'pesado' })
        $posto = Escolher $postosPossiveis
        $litros = [decimal][Math]::Round($v.Tanque - $v.Litros, 2)
        $quando = $saida.AddHours($horas * $percorrido / $km)
        $corpo = @{
            veiculoId = $v.Id; motoristaId = $m.Id
            tipoCombustivelId = $tiposCombustivel[$v.Comb]; postoId = $posto.Id
            litros = $litros; valorLitro = (Preco-Do-Litro $v.Comb $posto $quando)
            odometro = $v.Km + $percorrido
            notaFiscal = "NFC-e $(SortearInt 100000 999999)"
            dataAbastecimento = (Data $quando)
        }
        if (Chance 0.6) { $corpo.frentista = Escolher $frentistas }
        Api 'Post' '/abastecimento' $corpo | Out-Null
        $contagem.Abastecimentos++
        $v.Litros = $v.Tanque
    }
}

function Concluir-Preventivas($v, [DateTime] $quando, [bool] $deixarAtrasar) {
    foreach ($p in @($v.Pendentes)) {
        if ($v.Km -lt $p.Prevista - $p.Folga -or $deixarAtrasar) { continue }
        $custo = (Sortear $p.Prev.Custo[0] $p.Prev.Custo[1]) * $v.Mult
        Api 'Post' "/manutencao/$($p.Id)/concluir" @{
            quilometragemRealizada = $v.Km; dataRealizacao = (Data $quando); custo = (Dinheiro $custo)
        } | Out-Null
        $contagem.Manutencoes++

        $p.Prevista = [int]([Math]::Round(($v.Km + $p.Intervalo) / 100) * 100)
        $p.Folga = SortearInt 0 400
        $p.Id = (Api 'Post' '/manutencao' @{
            veiculoId = $v.Id; tipoManutencaoId = $tiposManutencao[$p.Prev.Tipo]; quilometragemPrevista = $p.Prevista
        }).id
    }
}

Passo "Operação de $($inicio.ToString('dd/MM/yyyy')) a $($hoje.AddDays(-1).ToString('dd/MM/yyyy'))"
$multasPendentes = @($multas | ForEach-Object { $inicio.AddDays((SortearInt 5 ((($hoje - $inicio).Days) - 5))) } | Sort-Object)
$limiteFim = (Get-Date).AddHours(-1)
$mesAtual = -1

for ($dia = $inicio; $dia -lt $hoje; $dia = $dia.AddDays(1)) {
    if ($dia.Month -ne $mesAtual) {
        $mesAtual = $dia.Month
        Nota ("{0:MM/yyyy}  ({1} chamadas até aqui)" -f $dia, $script:chamadas)

        # Parcela do seguro no dia 10; o IPVA dos dois pesados cai no segundo mês.
        foreach ($v in $frota) {
            $vencimento = (Get-Date -Year $dia.Year -Month $dia.Month -Day 10).Date
            if ($vencimento -lt $inicio -or $vencimento -ge $hoje) { continue }
            Lancar-Despesa $v 'Seguro' ((Sortear 170 230) * $v.Mult) $vencimento $null "Parcela mensal do seguro - $($vencimento.ToString('MM/yyyy'))"
            if ($v.Classe -eq 'pesado' -and $dia.Month -eq $inicio.AddMonths(1).Month) {
                Lancar-Despesa $v 'IPVA' ($v.Mult * 980) $vencimento $null "IPVA $($dia.Year) - parcela única"
            }
        }
    }

    if ($dia.DayOfWeek -eq 'Sunday') { continue }
    $probabilidade = if ($dia.DayOfWeek -eq 'Saturday') { 0.25 } else { 0.72 }
    $fimDaJanela = $dia -ge $hoje.AddDays(-25)

    foreach ($v in (Embaralhar $frota)) {
        if ($v.Livre -gt $dia -or -not (Chance $probabilidade)) { continue }
        $livres = @($motoristas | Where-Object { $_.Livre -le $dia })
        if ($livres.Count -eq 0) { break }
        $m = Escolher $livres

        if ($v.Classe -in 'leve', 'van') {
            $destino = Escolher $locaisUrbanos
            $km = if ($v.Classe -eq 'leve') { SortearInt 45 170 } else { SortearInt 80 250 }
        } else {
            $opcoes = @($locaisRegionais | Where-Object { $v.Classe -eq 'pesado' -or (Distancia-Km $deposito $_) -le 260 })
            $destino = Escolher $opcoes
            $km = [int]((Distancia-Km $deposito $destino) * 1.28 * 2 + (SortearInt 8 40))
        }

        $saida = $dia.AddMinutes((SortearInt 360 520))
        $horas = $km / $velocidade[$v.Classe] + (Sortear 0.5 2.0)
        if ($horas -gt 11) { $horas += 10 }
        $chegada = $saida.AddHours($horas)
        if ($chegada -gt $limiteFim) { continue }

        $rotaId = Abrir-Rota $v $m $saida $destino
        Rodar $v $m $km $saida $horas
        $v.Km += $km
        Api 'Post' "/rota/$rotaId/encerrar" @{ kmFinal = $v.Km; dataFim = (Data $chegada) } | Out-Null

        $v.Livre = $chegada.Date.AddDays(1)
        $m.Livre = $chegada.Date.AddDays(1)

        if ($destino.Pracas -gt 0 -and (Chance 0.85)) {
            $valor = $destino.Pracas * 2 * 4.10 * $v.Eixos
            Lancar-Despesa $v 'Pedágio' $valor $chegada $m.Id "Ida e volta - $($destino.Endereco.Split(',')[-1].Trim())"
        }
        if ($v.Classe -in 'leve', 'van' -and (Chance 0.06)) {
            Lancar-Despesa $v 'Estacionamento' (Sortear 12 35) $saida.AddHours(2) $m.Id $null
        }
        if ($multasPendentes.Count -gt 0 -and $dia -ge $multasPendentes[0]) {
            $multa = $multas[4 - $multasPendentes.Count]
            Lancar-Despesa $v 'Multa de trânsito' $multa.Valor $saida.AddHours(1) $m.Id $multa.Obs
            $multasPendentes = @($multasPendentes | Select-Object -Skip 1)
        }

        $atrasa = $fimDaJanela -and ($veiculosQueAtrasam -contains $v.Nome)
        Concluir-Preventivas $v $chegada.AddHours(1) $atrasa
    }

    foreach ($v in $frota) {
        if (($dia - $v.UltimaLavagem).Days -ge 28 -and $dia.DayOfWeek -eq 'Saturday') {
            Lancar-Despesa $v 'Lavagem' ((Sortear 55 80) * [Math]::Sqrt($v.Mult)) $dia.AddHours(10) $null $null
            $v.UltimaLavagem = $dia
        }
    }
}

# ============================================================================
# 6. Retrato de hoje: alertas no dashboard e duas rotas em andamento
# ============================================================================

Passo 'Situação atual'

# Atrasadas: os veículos de $veiculosQueAtrasam passaram da prevista sem concluir.
# Se a sorte não os levou até lá, força uma preventiva já vencida.
foreach ($nome in $veiculosQueAtrasam) {
    $v = $frota | Where-Object { $_.Nome -eq $nome }
    if (-not ($v.Pendentes | Where-Object { $_.Prevista -le $v.Km })) {
        Api 'Post' '/manutencao' @{
            veiculoId = $v.Id; tipoManutencaoId = $tiposManutencao['Troca de filtro de ar']
            quilometragemPrevista = $v.Km - (SortearInt 300 900); observacao = 'Ficou para a próxima parada'
        } | Out-Null
    }
}

# Vencendo: dentro da faixa de aviso de 500 km.
foreach ($par in @(@('Fiorino Furgão', 'Troca de filtro de ar'), @('Master Furgão', 'Troca de filtro de ar'), @('Atego 2430', 'Troca de pneus'))) {
    $v = $frota | Where-Object { $_.Nome -eq $par[0] }
    Api 'Post' '/manutencao' @{
        veiculoId = $v.Id; tipoManutencaoId = $tiposManutencao[$par[1]]; quilometragemPrevista = $v.Km + (SortearInt 150 450)
    } | Out-Null
}

$strada = $frota | Where-Object { $_.Nome -eq 'Strada Freedom' }
Api 'Post' '/manutencao' @{
    veiculoId = $strada.Id; tipoManutencaoId = $tiposManutencao['Troca de bateria']
    quilometragemPrevista = $strada.Km + 8000; dataPrevista = (Data $hoje.AddDays(12))
    observacao = 'Bateria falhando na partida a frio'
} | Out-Null
Ok 'manutenções atrasadas e vencendo para o dashboard'

$emRota = @(
    @{ Veiculo = 'Sprinter 417'; Destino = $locaisUrbanos[1] }
    @{ Veiculo = 'Atego 2430';   Destino = $locaisRegionais[2] }
)
$saidaHoje = (Get-Date).AddMinutes(-40)
for ($i = 0; $i -lt $emRota.Count; $i++) {
    $v = $frota | Where-Object { $_.Nome -eq $emRota[$i].Veiculo }
    $m = $motoristas[$i]
    Abrir-Rota $v $m $saidaHoje $emRota[$i].Destino | Out-Null
    Ok "$($m.Nome) em rota com $($v.Placa) ($($v.Nome))"
}

# ============================================================================
# Resumo
# ============================================================================

Passo 'Pronto'
foreach ($k in $contagem.Keys) { Write-Host ('    {0,-16} {1}' -f $k, $contagem[$k]) }
Write-Host ('    {0,-16} {1}' -f 'Chamadas à API', $script:chamadas)

if (-not $Simular) {
    Write-Host "`nLogins criados (senha informada no início):" -ForegroundColor Green
    for ($i = 0; $i -lt $nomesMotoristas.Count; $i++) {
        Write-Host ('    {0,-40} Motorista  {1}' -f (Email-Derivado "motorista$($i + 1)"), $nomesMotoristas[$i])
    }
    Write-Host ('    {0,-40} Supervisor {1}' -f (Email-Derivado 'supervisor'), $nomeSupervisor)
}
