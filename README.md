# external-dns-namesilo-webhook

A [ExternalDNS webhook provider](https://kubernetes-sigs.github.io/external-dns/v0.14.2/tutorials/webhook-provider/) for [NameSilo](https://www.namesilo.com/api-reference) DNS. Written in C# (.NET) and designed to run as a sidecar alongside ExternalDNS in Kubernetes.

## Purpose

Manages DNS records in NameSilo via the ExternalDNS webhook HTTP API. In the homelab deployment, ExternalDNS maintains only the apex **`tormentz.com` A record** (Traefik LoadBalancer IP). Subdomains are static CNAMEs to the apex and are not managed by this webhook.

## Planned layout

```
external-dns-namesilo-webhook/
  ExternalDnsNamesiloWebhook.sln
  src/
    ExternalDnsNamesiloWebhook/          # ASP.NET Core web host
    ExternalDnsNamesiloWebhook.Core/      # NameSilo client, provider, configuration
    ExternalDnsNamesiloWebhook.Tests/     # xUnit tests
  Dockerfile
  .github/workflows/publish.yaml
```

## Configuration

| Setting | Environment / file | Description |
|---------|-------------------|-------------|
| API key | `/run/secrets/namesilo-api-key` or `Namesilo:ApiKey` | NameSilo API key |
| Secrets path | `SECRETS_PATH` (default `/run/secrets`) | Kubernetes secret volume mount |
| Domain filter | `Namesilo:DomainFilter` | Zones to manage (e.g. `tormentz.com`) |
| Default TTL | `Namesilo:DefaultTtl` | TTL when ExternalDNS sends 0 (default 300) |
| Dry run | `Namesilo:DryRun` | Log changes without calling NameSilo API |

Secrets use ASP.NET Core [key-per-file configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/#key-per-file-configuration): each secret key is a filename; use `__` for nested config keys.

## Container image

Published to **ghcr.io**:

```
ghcr.io/t012m3n7or/external-dns-namesilo-webhook
```

## Webhook API

Implements the [ExternalDNS webhook OpenAPI spec](https://kubernetes-sigs.github.io/external-dns/latest/api/webhook.yaml):

| Route | Method | Purpose |
|-------|--------|---------|
| `/` | GET | Domain filter negotiation |
| `/records` | GET | List current DNS records |
| `/records` | POST | Apply create/update/delete changes |
| `/adjustendpoints` | POST | Adjust endpoints (TTL normalization) |
| `/healthz` | GET | Kubernetes probe |
| `/metrics` | GET | Prometheus metrics |

Default webhook port: **8888**. Metrics port: **8080**.

## Local development

```bash
dotnet restore
dotnet build
dotnet test
dotnet format ExternalDnsNamesiloWebhook.sln
dotnet run --project src/ExternalDnsNamesiloWebhook
```

Set `Namesilo:ApiKey` in user secrets or environment for local testing against NameSilo.

## References

- [ExternalDNS webhook provider](https://kubernetes-sigs.github.io/external-dns/v0.14.2/tutorials/webhook-provider/)
- [NameSilo DNS API](https://www.namesilo.com/api-reference#dns/dns-list-records)
- [external-dns-digitalocean-webhook](https://github.com/amoniacou/external-dns-digitalocean-webhook) (Go reference implementation)

## License

Apache-2.0
