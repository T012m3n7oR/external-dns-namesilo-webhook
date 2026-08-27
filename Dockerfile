FROM mcr.microsoft.com/dotnet/sdk:10.0-noble@sha256:e1ffd2a92ae84c1291bc1b6887501f8af98e6331e7af6d4c8d37168c5e87a64c AS build
WORKDIR /src

RUN apt-get update \
    && apt-get upgrade -y \
    && rm -rf /var/lib/apt/lists/*

COPY .editorconfig ./
COPY src/ExternalDnsNamesiloWebhook.Core/ExternalDnsNamesiloWebhook.Core.csproj src/ExternalDnsNamesiloWebhook.Core/
COPY src/ExternalDnsNamesiloWebhook/ExternalDnsNamesiloWebhook.csproj src/ExternalDnsNamesiloWebhook/
RUN dotnet restore src/ExternalDnsNamesiloWebhook/ExternalDnsNamesiloWebhook.csproj

COPY . .
RUN dotnet publish src/ExternalDnsNamesiloWebhook/ExternalDnsNamesiloWebhook.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:RunAnalyzers=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble@sha256:a4556ed033fa96f984bb7a8d348851cb2d36b1281dd2420070045f664fbb5f94 AS runtime
WORKDIR /app

ARG DEBIAN_FRONTEND=noninteractive
RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
    --mount=type=cache,target=/var/lib/apt,sharing=locked \
    apt-get update \
    && apt-get upgrade -y --no-install-recommends

EXPOSE 8888
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8888;http://+:8080
ENV SECRETS_PATH=/run/secrets

COPY --from=build /app/publish .
RUN chown -R app:app /app

USER $APP_UID

ENTRYPOINT ["dotnet", "ExternalDnsNamesiloWebhook.dll"]
