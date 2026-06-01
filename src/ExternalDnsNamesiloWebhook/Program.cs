using ExternalDnsNamesiloWebhook.Core.Configuration;
using ExternalDnsNamesiloWebhook.Core.DependencyInjection;
using ExternalDnsNamesiloWebhook.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;

namespace ExternalDnsNamesiloWebhook;

/// <summary>ASP.NET Core host entry point for the ExternalDNS NameSilo webhook.</summary>
public partial class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        ConfigureConfiguration(builder);
        ConfigureServices(builder);
        ConfigureKestrel(builder);

        WebApplication app = builder.Build();
        LogStartup(app);
        app.UseHttpMetrics();
        app.MapControllers();
        app.MapMetrics();
        app.Run();
    }

    private static void ConfigureConfiguration(WebApplicationBuilder builder)
    {
        builder.Configuration.AddKeyPerFileSecrets();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddNamesiloWebhookCore(builder.Configuration);
        builder.AddWebhookControllers();
    }

    private static void ConfigureKestrel(WebApplicationBuilder builder)
    {
        if (builder.Environment.IsEnvironment("Testing"))
        {
            return;
        }

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(8888);
            options.ListenAnyIP(8080);
        });
    }

    private static void LogStartup(WebApplication app)
    {
        ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
        NamesiloOptions options = app.Services.GetRequiredService<IOptions<NamesiloOptions>>().Value;

        logger.LogInformation(
            "ExternalDNS NameSilo webhook started. DryRun={DryRun}, DomainCount={DomainCount}",
            options.DryRun,
            options.DomainFilter.Length);

        if (options.DryRun)
        {
            logger.LogWarning("NameSilo DryRun is enabled; DNS mutations will not be sent to the API");
        }
    }
}
