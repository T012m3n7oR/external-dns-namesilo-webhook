using ExternalDnsNamesiloWebhook.Core.Constants;
using Microsoft.AspNetCore.Mvc;

namespace ExternalDnsNamesiloWebhook.Controllers;

[ApiController]
public sealed class HealthController : ControllerBase
{
    [HttpGet(WebhookPaths.Healthz)]
    [Produces(HttpMediaTypes.TextPlain)]
    public ContentResult Get()
    {
        return Content(HealthConstants.OkBody, HttpMediaTypes.TextPlain);
    }
}
