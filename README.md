# external-dns-namesilo-webhook

A C# [.NET 10](https://dotnet.microsoft.com/) [ExternalDNS webhook provider](https://kubernetes-sigs.github.io/external-dns/v0.14.2/tutorials/webhook-provider/) for the [NameSilo DNS API](https://www.namesilo.com/api-reference#dns/dns-list-records). Runs as a sidecar alongside ExternalDNS in Kubernetes.

## What it does

ExternalDNS calls this webhook over HTTP. The webhook translates those requests into NameSilo `dnsListRecords`, `dnsAddRecord`, `dnsUpdateRecord`, and `dnsDeleteRecord` API calls.

ExternalDNS typically manages apex **A** and **AAAA** records (for example a load-balancer IP) and **TXT** ownership records (`--registry=txt`) for zones listed in `Namesilo:DomainFilter`. Static CNAMEs and other records outside that filter are left unchanged when they are not selected by ExternalDNS.

### DNS record types

NameSilo list responses can include types the webhook does not manage. The `DnsRecordType` enum covers all known NameSilo wire values (`A`, `AAAA`, `CAA`, `CNAME`, `MX`, `NS`, `PTR`, `SOA`, `SRV`, `TXT`) so deserialization fails loudly if NameSilo adds an unknown type.

The service only **creates, updates, and deletes** records whose type is in the supported set: **A, AAAA, CNAME, TXT, MX, NS, SRV**. Other enum values (e.g. **CAA, PTR, SOA**) are listed but skipped.

## Architecture

```text
┌─────────────────────┐     localhost:8888     ┌──────────────────────────┐
│  external-dns       │ ─────────────────────► │  namesilo-webhook        │
│  (--provider=webhook)│                        │  (this project)          │
└─────────┬───────────┘                        └────────────┬─────────────┘
          │ watches Traefik Service                         │ HTTPS GET
          ▼                                                 ▼
   example.com A record                           NameSilo DNS API
```

## Project layout

All application code lives under `src/`:

```text
external-dns-namesilo-webhook/
  Directory.Build.props                 # ImplicitUsings disabled solution-wide
  ExternalDnsNamesiloWebhook.sln
  src/
    ExternalDnsNamesiloWebhook/           # ASP.NET Core host (Kestrel, DI wiring)
      Controllers/                        # Thin HTTP adapters
      DependencyInjection/
      Filters/                            # MVC exception mapping
    ExternalDnsNamesiloWebhook.Core/      # NameSilo client, DNS service, config, DTOs
      Configuration/                      # Options, SECRETS_PATH / key-per-file
      Constants/                          # API defaults, webhook paths, media types
      DependencyInjection/
      Enums/                              # DnsRecordType and extensions
      Logging/                            # Log redaction helpers
      Namesilo/                           # API client, DNS service, hostname mapping
        Models/                           # NameSilo API JSON wire types
      Webhook/                            # ExternalDNS webhook models
    ExternalDnsNamesiloWebhook.Tests/
      Configuration/
      Constants/
      Fixtures/                           # TestData, options/changes builders
      Logging/
      Namesilo/
      Webhook/
  Dockerfile
  .github/workflows/publish.yaml
```

The split between **Core** (DNS service logic, testable without HTTP) and **Web** (Kestrel + controllers) is intentional.

## Configuration

| Setting | Source | Default | Description |
|---------|--------|---------|-------------|
| `Namesilo:ApiKey` | appsettings / env | — | NameSilo API key |
| `namesilo-api-key` | file at `SECRETS_PATH` | — | Flat key-per-file override (matches cert-manager secret key name) |
| `SECRETS_PATH` | env | `/run/secrets` | Directory for Kubernetes secret volume mounts |
| `Namesilo:DomainFilter` | appsettings | `["example.com"]` | Zones this service serves |
| `Namesilo:DefaultTtl` | appsettings | `300` | TTL when ExternalDNS sends `0` |
| `Namesilo:DryRun` | appsettings | `false` | Log mutations without calling NameSilo |
| `Namesilo:ApiBaseUrl` | appsettings | `https://www.namesilo.com/api` | API base URL |

Secrets use ASP.NET Core [key-per-file configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/#key-per-file-configuration): each filename is a config key; use `__` for nesting (e.g. `Namesilo__ApiKey`).

Logging goes to **stdout** (container logs). Default level is **Information** for application code; **Debug** in Development. TXT record targets are redacted in logs; the API key and request URLs are never logged.

### Kubernetes sidecar example

```yaml
containers:
  - name: namesilo-webhook
    image: ghcr.io/t012m3n7or/external-dns-namesilo-webhook:main
    env:
      - name: SECRETS_PATH
        value: /run/secrets
    volumeMounts:
      - name: namesilo-api-key
        mountPath: /run/secrets
        readOnly: true
    ports:
      - containerPort: 8888
        name: http-webhook
    livenessProbe:
      httpGet: { path: /healthz, port: http-webhook }
volumes:
  - name: namesilo-api-key
    secret:
      secretName: namesilo-api-key
```

## Webhook API

Implements the [ExternalDNS webhook OpenAPI spec](https://kubernetes-sigs.github.io/external-dns/latest/api/webhook.yaml):

| Route | Method | Response | Purpose |
|-------|--------|----------|---------|
| `/` | GET | 200 + domain filter JSON | Negotiation |
| `/records` | GET | 200 + endpoint array | List current records |
| `/records` | POST | 204 | Apply create/update/delete |
| `/adjustendpoints` | POST | 200 + adjusted endpoints | TTL normalization |
| `/healthz` | GET | 200 | Kubernetes probe |
| `/metrics` | GET | 200 | Prometheus (OpenMetrics) |

- Webhook port: **8888**
- Metrics port: **8080**
- Content-Type: `application/external.dns.webhook+json;version=1`

Contract reference: [ExternalDNS webhook OpenAPI](https://kubernetes-sigs.github.io/external-dns/latest/api/webhook.yaml).

## Container image

Published to GitHub Container Registry on push to `main` and version tags:

```text
ghcr.io/t012m3n7or/external-dns-namesilo-webhook:latest      # main push and weekly schedule
ghcr.io/t012m3n7or/external-dns-namesilo-webhook:main        # main push and weekly schedule
ghcr.io/t012m3n7or/external-dns-namesilo-webhook:<git-sha>   # every build (PR builds locally; not pushed)
ghcr.io/t012m3n7or/external-dns-namesilo-webhook:<semver>    # version tags (v*)
```

## Development

```bash
dotnet restore
dotnet build ExternalDnsNamesiloWebhook.sln
dotnet test ExternalDnsNamesiloWebhook.sln
dotnet format ExternalDnsNamesiloWebhook.sln          # apply formatting
dotnet format ExternalDnsNamesiloWebhook.sln --verify-no-changes   # CI check
dotnet run --project src/ExternalDnsNamesiloWebhook
```

Run `dotnet format` on the solution before every commit.

For local testing against NameSilo, set the API key via user secrets, environment variable, or a temp directory:

```bash
export SECRETS_PATH=/tmp/namesilo-secrets
mkdir -p "$SECRETS_PATH"
printf '%s' 'YOUR_API_KEY' > "$SECRETS_PATH/namesilo-api-key"
dotnet run --project src/ExternalDnsNamesiloWebhook
```

## Tests

| Area | Project path | Covers |
|------|--------------|--------|
| Hostname mapping | `Tests/Namesilo/DnsNameMapperTests` | Apex `@`, subdomains, trailing dots, domain filter, record-type support |
| NameSilo client | `Tests/Namesilo/NamesiloApiClientTests` | JSON (de)serialization, error codes, dry-run, MockHttp query matching |
| DNS service | `Tests/Namesilo/NamesiloDnsServiceTests` | Records, create/update/delete, TTL adjustment, change batch logging |
| HTTP endpoints | `Tests/Webhook/WebhookEndpointTests` | `/`, `/records`, `/adjustendpoints`, `/healthz` |
| Secrets config | `Tests/Configuration/` | `SECRETS_PATH` / key-per-file loading |
| Log redaction | `Tests/Logging/DnsLogRedactionTests` | TXT target redaction |

CI runs build, format verification, and tests on every push and pull request. Tests use **xUnit.net v3**, [AutoFixture](https://github.com/AutoFixture/AutoFixture), explicit `using` directives (no global usings), and shared fixtures in `Tests/Fixtures/` (`TestData`, `NamesiloOptionsBuilder`, `DnsChangesBuilder`). A **weekly scheduled workflow** rebuilds the container with `--pull`, applies OS package upgrades in the Dockerfile, and fails on vulnerable NuGet packages.

## References

- [ExternalDNS webhook provider](https://kubernetes-sigs.github.io/external-dns/v0.14.2/tutorials/webhook-provider/)
- [ExternalDNS webhook OpenAPI](https://kubernetes-sigs.github.io/external-dns/latest/api/webhook.yaml)
- [NameSilo DNS API](https://www.namesilo.com/api-reference#dns/dns-list-records)
- [external-dns-digitalocean-webhook](https://github.com/amoniacou/external-dns-digitalocean-webhook) (Go reference)

## License

Apache License 2.0 — see [LICENSE](LICENSE).
