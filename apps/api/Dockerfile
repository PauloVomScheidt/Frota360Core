# Etapa 1 — Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Frota360/Frota360.Api.csproj", "Frota360/"]
COPY ["Frota360.Application/Frota360.Application.csproj", "Frota360.Application/"]
COPY ["Frota360.Domain/Frota360.Domain.csproj", "Frota360.Domain/"]
COPY ["Frota360.Infrastructure/Frota360.Infrastructure.csproj", "Frota360.Infrastructure/"]

RUN dotnet restore "Frota360/Frota360.Api.csproj"

COPY . .

RUN dotnet publish "Frota360/Frota360.Api.csproj" -c Release -o /app/publish

# Etapa 2 — Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Frota360.Api.dll"]
