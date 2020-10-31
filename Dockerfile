FROM ghcr.io/thosch1800/thosch-dotnet-sdk:3.1 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ./src/LogoMqttBinding/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY ./src/LogoMqttBinding/. ./
RUN dotnet publish -c Release -o out 

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "LogoMqttBinding.dll"]