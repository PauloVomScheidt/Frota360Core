# Etapa 1 — Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Os .csproj vêm antes do resto para que a camada do restore seja cacheada: mudança em
# código-fonte não invalida o download dos pacotes.
COPY ["src/Api/Frota360.Api.csproj", "src/Api/"]
COPY ["src/Application/Frota360.Application.csproj", "src/Application/"]
COPY ["src/Domain/Frota360.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Frota360.Infrastructure.csproj", "src/Infrastructure/"]

RUN dotnet restore "src/Api/Frota360.Api.csproj"

# O que este COPY leva é filtrado pelo .dockerignore — que exclui bin/, obj/ e os
# appsettings de ambiente. Sem ele, o obj/ da máquina host contaminaria o restore.
COPY . .

RUN dotnet publish "src/Api/Frota360.Api.csproj" -c Release -o /app/publish --no-restore

# Etapa 2 — Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# A imagem aspnet não traz wget nem curl (verificado), e sem um cliente HTTP o HEALTHCHECK
# abaixo falharia sempre — deixando o container eternamente unhealthy e travando o
# depends_on: service_healthy do compose. O curl entra só por isso.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# O sistema grava hora local em todas as datas persistidas (ver DataSemFusoConverter), então
# o fuso do container faz parte da semântica dos dados. Sem esta linha o Linux assume UTC e a
# mesma build gravaria 3 h adiantado em produção, sem erro nenhum. A imagem aspnet:10.0 já
# traz o tzdata, então basta a variável. O fuso efetivo sai no log de inicialização.
ENV TZ=America/Sao_Paulo

# /health verifica o DbContext, então "saudável" aqui significa "API de pé E banco
# alcançável" — que é exatamente o que o compose precisa saber para liberar os dependentes.
# O start-period cobre a janela em que as migrations estão rodando no boot.
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1

# A imagem já traz o usuário "app" sem privilégio. Só dá para usá-lo porque em produção o
# Serilog escreve apenas no console: não há diretório logs/ para conceder permissão. A porta
# 8080 está acima de 1024, então o bind não exige root.
USER app

ENTRYPOINT ["dotnet", "Frota360.Api.dll"]
