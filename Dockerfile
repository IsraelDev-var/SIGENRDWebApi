# syntax=docker/dockerfile:1
# Imagen de produccion de la API de EnergyMap (SIGENRDWebApi).
#   docker build -t energymap-api .

# ---- compilacion ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Primero solo los .csproj: la restauracion queda en cache mientras no cambien las dependencias.
COPY SIGENRD.Core.Domain/SIGENRD.Core.Domain.csproj SIGENRD.Core.Domain/
COPY SIGENRD.Core.Application/SIGENRD.Core.Application.csproj SIGENRD.Core.Application/
COPY SIGENRD.Infrastructure.Persistences/SIGENRD.Infrastructure.Persistences.csproj SIGENRD.Infrastructure.Persistences/
COPY SIGENRD.Infrastructure.Identity/SIGENRD.Infrastructure.Identity.csproj SIGENRD.Infrastructure.Identity/
COPY SIGENRD.Infrastructure.Shared/SIGENRD.Infrastructure.Shared.csproj SIGENRD.Infrastructure.Shared/
COPY SIGENRDWebApi/SIGENRDWebApi.csproj SIGENRDWebApi/
RUN dotnet restore SIGENRDWebApi/SIGENRDWebApi.csproj

COPY SIGENRD.Core.Domain/ SIGENRD.Core.Domain/
COPY SIGENRD.Core.Application/ SIGENRD.Core.Application/
COPY SIGENRD.Infrastructure.Persistences/ SIGENRD.Infrastructure.Persistences/
COPY SIGENRD.Infrastructure.Identity/ SIGENRD.Infrastructure.Identity/
COPY SIGENRD.Infrastructure.Shared/ SIGENRD.Infrastructure.Shared/
COPY SIGENRDWebApi/ SIGENRDWebApi/
RUN dotnet publish SIGENRDWebApi/SIGENRDWebApi.csproj -c Release -o /app --no-restore /p:UseAppHost=false

# ---- ejecucion ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app .

# Usuario sin privilegios (viene en la imagen base) y puerto sin privilegios.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Los secretos (conexiones, JWT, SMTP, semilla) llegan por variables de entorno; nunca van en la imagen.
ENTRYPOINT ["dotnet", "SIGENRDWebApi.dll"]
