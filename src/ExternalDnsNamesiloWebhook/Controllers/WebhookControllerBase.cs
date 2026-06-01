using ExternalDnsNamesiloWebhook.Core.Constants;
using Microsoft.AspNetCore.Mvc;

namespace ExternalDnsNamesiloWebhook.Controllers;

public abstract class WebhookControllerBase : ControllerBase
{
    protected IActionResult WebhookOk(object? value)
    {
        return new JsonResult(value)
        {
            ContentType = WebhookMediaTypes.Version1,
        };
    }
}
