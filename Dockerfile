FROM ghcr.io/thosch1800/dotnet-sdk:2105.22.0 AS build-env

WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ./src/LogoMqttBinding/*.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY ./src/LogoMqttBinding/. ./
#RUN dotnet publish -c Release -o out 
RUN dotnet publish -c Release -r linux-arm --self-contained -o out



# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:5.0-alpine
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "LogoMqttBinding.dll"]

