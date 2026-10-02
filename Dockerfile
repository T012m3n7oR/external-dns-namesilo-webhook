FROM mcr.microsoft.com/dotnet/sdk:10.0-noble@sha256:83e0db97c45d2e39b80123fe42940a23c423405a17f80b608a4b8768033d6392 AS build
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

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble@sha256:57460add89e2b3dd1950c41d8b7dc96eeb7a24d13d98e3656ce9997a8b746bd6 AS runtime
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
