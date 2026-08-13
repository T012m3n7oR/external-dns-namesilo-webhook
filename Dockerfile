FROM mcr.microsoft.com/dotnet/sdk:10.0-noble@sha256:ed034a8bf0b24ded0cbbac07e17825d8e9ebfe21e308191d0f7421eaf5ad4664 AS build
WORKDIR /src

COPY Directory.Build.props StyleCopAnalyzers.ruleset ./
COPY src/ExternalDnsNamesiloWebhook.Core/ExternalDnsNamesiloWebhook.Core.csproj src/ExternalDnsNamesiloWebhook.Core/
COPY src/ExternalDnsNamesiloWebhook/ExternalDnsNamesiloWebhook.csproj src/ExternalDnsNamesiloWebhook/
RUN dotnet restore src/ExternalDnsNamesiloWebhook/ExternalDnsNamesiloWebhook.csproj

COPY . .
RUN dotnet publish src/ExternalDnsNamesiloWebhook/ExternalDnsNamesiloWebhook.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble@sha256:1fa23fc4872d95fd71c2833ebe65d7e84a43b2d51a31d119516852f13d9505a7 AS runtime
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
