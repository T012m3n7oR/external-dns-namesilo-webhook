using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ExternalDnsNamesiloWebhook.Core.Constants;
using ExternalDnsNamesiloWebhook.Core.Contracts.NameSilo;
using ExternalDnsNamesiloWebhook.Core.Webhook.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ExternalDnsNamesiloWebhook.Controllers;

[ApiController]
[Produces(WebhookMediaTypes.Version1)]
public sealed class ExternalDnsWebhookController : WebhookControllerBase
{
    private readonly INamesiloDnsService _dnsService;
    private readonly ILogger<ExternalDnsWebhookController> _logger;

    public ExternalDnsWebhookController(
        INamesiloDnsService dnsService,
        ILogger<ExternalDnsWebhookController> logger)
    {
        _dnsService = dnsService;
        _logger = logger;
    }

    [HttpGet(WebhookPaths.Negotiate)]
    [ProducesResponseType(typeof(DomainFilterResponse), StatusCodes.Status200OK)]
    public IActionResult Negotiate()
    {
        _logger.LogDebug("ExternalDNS negotiate (domain filter)");
        return WebhookOk(_dnsService.GetDomainFilter());
    }

    [HttpGet(WebhookPaths.Records)]
    [ProducesResponseType(typeof(IReadOnlyList<DnsEndpoint>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecordsAsync(CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("ExternalDNS list records (NameSilo zone state)");
        IReadOnlyList<DnsEndpoint> records = await _dnsService.GetRecordsAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation(
            "ExternalDNS list records completed in {ElapsedMs}ms",
            stopwatch.ElapsedMilliseconds);
        return WebhookOk(records);
    }

    [HttpPost(WebhookPaths.Records)]
    [Consumes(HttpMediaTypes.ApplicationJson, WebhookMediaTypes.Version1)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ApplyChangesAsync(
        [FromBody] DnsChanges? changes,
        CancellationToken cancellationToken)
    {
        if (changes is null)
        {
            return BadRequest();
        }

        int changeCount = changes.Create.Count + changes.UpdateNew.Count + changes.Delete.Count;
        if (changeCount > 0)
        {
            _logger.LogInformation(
                "ExternalDNS apply DNS changes (create={Create} update={Update} delete={Delete})",
                changes.Create.Count,
                changes.UpdateNew.Count,
                changes.Delete.Count);
        }
        else
        {
            _logger.LogDebug("ExternalDNS apply DNS changes (empty change set)");
        }

        await _dnsService.ApplyChangesAsync(changes, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost(WebhookPaths.AdjustEndpoints)]
    [Consumes(HttpMediaTypes.ApplicationJson, WebhookMediaTypes.Version1)]
    [ProducesResponseType(typeof(IReadOnlyList<DnsEndpoint>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult AdjustEndpoints([FromBody] IReadOnlyList<DnsEndpoint>? endpoints)
    {
        if (endpoints is null)
        {
            return BadRequest();
        }

        _logger.LogInformation(
            "ExternalDNS adjust endpoints ({EndpointCount} from controller)",
            endpoints.Count);
        return WebhookOk(_dnsService.AdjustEndpoints(endpoints));
    }
}
