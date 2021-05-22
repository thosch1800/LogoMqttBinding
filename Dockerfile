FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim-arm32v7 AS build-env
RUN dotnet nuget add source https://nuget.pkg.github.com/thosch1800/index.json -n github --store-password-in-clear-text -u thosch1800 -p 972b76d3dae1b53b2fae1e463e79a3a37b409ed1
RUN dotnet nuget list source
#FROM ghcr.io/thosch1800/dotnet-sdk:latest AS build-env

WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ./src/LogoMqttBinding/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY ./src/LogoMqttBinding/. ./
RUN dotnet publish -c Release -o out 

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:5.0-buster-slim-arm32v7
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "LogoMqttBinding.dll"]
