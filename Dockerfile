FROM ghcr.io/thosch1800/dotnet-core-sdk:latest AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ./.github-ci/nuget.config ./
COPY ./src/LogoMqttBinding/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY ./src/LogoMqttBinding/. ./
RUN dotnet publish -c Release -o out 

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "LogoMqttBinding.dll"]