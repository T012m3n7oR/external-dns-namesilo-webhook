FROM mcr.microsoft.com/dotnet/sdk:10.0-noble@sha256:35d40304542c8689331f8cab17c65926cdf48fe711e289321d71924b230a7d29 AS build
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

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble@sha256:2d584d8147faddb0d678c5748d47953e5b8e18621ed4fb7049a91381d9d7746f AS runtime
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
