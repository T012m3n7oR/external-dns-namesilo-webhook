using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ExternalDnsNamesiloWebhook.Core.Constants;
using ExternalDnsNamesiloWebhook.Core.Contracts.NameSilo;
using ExternalDnsNamesiloWebhook.Core.Webhook.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ExternalDnsNamesiloWebhook.Controllers;

[ApiController]
[Produces(WebhookMediaTypes.Version1)]
public sealed class ExternalDnsWebhookController : WebhookControllerBase
{
    private readonly INamesiloDnsService _dnsService;

    public ExternalDnsWebhookController(INamesiloDnsService dnsService)
    {
        _dnsService = dnsService;
    }

    [HttpGet(WebhookPaths.Negotiate)]
    [ProducesResponseType(typeof(DomainFilterResponse), StatusCodes.Status200OK)]
    public IActionResult Negotiate()
    {
        return WebhookOk(_dnsService.GetDomainFilter());
    }

    [HttpGet(WebhookPaths.Records)]
    [ProducesResponseType(typeof(IReadOnlyList<DnsEndpoint>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecordsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<DnsEndpoint> records = await _dnsService.GetRecordsAsync(cancellationToken).ConfigureAwait(false);
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

        await _dnsService.ApplyChangesAsync(changes, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    [HttpPost(WebhookPaths.AdjustEndpoints)]
    [Consumes(HttpMediaTypes.ApplicationJson, WebhookMediaTypes.Version1)]
    [ProducesResponseType(typeof(IReadOnlyList<DnsEndpoint>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult AdjustEndpoints([FromBody] List<DnsEndpoint>? endpoints)
    {
        if (endpoints is null)
        {
            return BadRequest();
        }

        return WebhookOk(_dnsService.AdjustEndpoints(endpoints));
    }
}
