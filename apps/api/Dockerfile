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

ENTRYPOINT ["dotnet", "Frota360.Api.dll"]
