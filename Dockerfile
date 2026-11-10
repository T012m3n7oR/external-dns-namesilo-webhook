FROM mcr.microsoft.com/dotnet/sdk:10.0-noble@sha256:fe0fd4e93bd2fbb47c883b7a6369e5c8d3db8bf01cdc5981f6ee35fbdf3ff4fb AS build
WORKDIR /src

RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
    --mount=type=cache,target=/var/lib/apt,sharing=locked \
    apt-get update \
    && apt-get upgrade -y

COPY .editorconfig ./
COPY src/ExternalDnsNamesiloWebhook.Core/ExternalDnsNamesiloWebhook.Core.csproj src/ExternalDnsNamesiloWebhook.Core/
COPY src/ExternalDnsNamesiloWebhook/ExternalDnsNamesiloWebhook.csproj src/ExternalDnsNamesiloWebhook/
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet restore src/ExternalDnsNamesiloWebhook/ExternalDnsNamesiloWebhook.csproj \
    /p:NuGetAudit=false

COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages,sharing=locked \
    dotnet publish src/ExternalDnsNamesiloWebhook/ExternalDnsNamesiloWebhook.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false \
    /p:NuGetAudit=false \
    /p:RunAnalyzers=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble@sha256:bfd912c83295ce66d4b706ebb8541b9f14259b0e76fbb88b2751f58ce2399d2e AS runtime
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
