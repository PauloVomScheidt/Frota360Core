# Etapa 1 — Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/Api/Frota360.Api.csproj", "src/Api/"]
COPY ["src/Application/Frota360.Application.csproj", "src/Application/"]
COPY ["src/Domain/Frota360.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Frota360.Infrastructure.csproj", "src/Infrastructure/"]

RUN dotnet restore "src/Api/Frota360.Api.csproj"

COPY . .

RUN dotnet publish "src/Api/Frota360.Api.csproj" -c Release -o /app/publish

# Etapa 2 — Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# O sistema grava hora local em todas as datas persistidas (ver DataSemFusoConverter), então
# o fuso do container faz parte da semântica dos dados. Sem esta linha o Linux assume UTC e a
# mesma build gravaria 3 h adiantado em produção, sem erro nenhum. A imagem aspnet:10.0 já
# traz o tzdata, então basta a variável. O fuso efetivo sai no log de inicialização.
ENV TZ=America/Sao_Paulo

ENTRYPOINT ["dotnet", "Frota360.Api.dll"]
