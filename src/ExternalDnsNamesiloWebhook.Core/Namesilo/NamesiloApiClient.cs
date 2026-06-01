using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ExternalDnsNamesiloWebhook.Core.Configuration;
using ExternalDnsNamesiloWebhook.Core.Constants;
using ExternalDnsNamesiloWebhook.Core.Contracts.NameSilo;
using ExternalDnsNamesiloWebhook.Core.Enums;
using ExternalDnsNamesiloWebhook.Core.Logging;
using ExternalDnsNamesiloWebhook.Core.Namesilo.Models;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExternalDnsNamesiloWebhook.Core.Namesilo;

public sealed class NamesiloApiClient : INamesiloApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<NamesiloOptions> _options;
    private readonly ILogger<NamesiloApiClient> _logger;

    public NamesiloApiClient(
        IHttpClientFactory httpClientFactory,
        IOptions<NamesiloOptions> options,
        ILogger<NamesiloApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NamesiloDnsRecord>> ListRecordsAsync(
        ListRecordsRequest request,
        CancellationToken cancellationToken)
    {
        NamesiloApiResponse apiResponse = await SendAndReadAsync(
            NamesiloApiOperations.ListRecords,
            request,
            cancellationToken).ConfigureAwait(false);

        IReadOnlyList<NamesiloDnsRecord> records = ParseRecords(apiResponse);
        _logger.LogDebug("Listed {RecordCount} NameSilo records for {Domain}", records.Count, request.Domain);
        return records;
    }

    public async Task<string> AddRecordAsync(
        AddRecordRequest request,
        CancellationToken cancellationToken)
    {
        if (TryDryRunAdd(request, out string dryRunRecordId))
        {
            return dryRunRecordId;
        }

        NamesiloApiResponse apiResponse = await SendAndReadAsync(
            NamesiloApiOperations.AddRecord,
            request,
            cancellationToken).ConfigureAwait(false);

        return apiResponse.Reply?.RecordId ?? string.Empty;
    }

    public async Task UpdateRecordAsync(
        UpdateRecordRequest request,
        CancellationToken cancellationToken)
    {
        if (TryDryRunUpdate(request))
        {
            return;
        }

        await SendAndReadAsync(
            NamesiloApiOperations.UpdateRecord,
            request,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteRecordAsync(
        DeleteRecordRequest request,
        CancellationToken cancellationToken)
    {
        if (TryDryRunDelete(request))
        {
            return;
        }

        await SendAndReadAsync(
            NamesiloApiOperations.DeleteRecord,
            request,
            cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<NamesiloDnsRecord> ParseRecords(NamesiloApiResponse response)
    {
        IReadOnlyList<NamesiloDnsRecord>? resourceRecords = response.Reply?.ResourceRecords;
        if (resourceRecords is null || resourceRecords.Count == 0)
        {
            return Array.Empty<NamesiloDnsRecord>();
        }

        return resourceRecords;
    }

    private static string Truncate(string value)
    {
        if (value.Length <= NamesiloApiDefaults.ErrorBodyMaxLength)
        {
            return value;
        }

        return value[..NamesiloApiDefaults.ErrorBodyMaxLength];
    }

    private bool TryDryRunAdd(AddRecordRequest request, out string recordId)
    {
        if (!_options.Value.DryRun)
        {
            recordId = string.Empty;
            return false;
        }

        _logger.LogInformation(
            "Dry run: add {Type} {Host}.{Domain} -> {Target} ttl={Ttl}",
            request.RecordType,
            request.RecordHost,
            request.Domain,
            DnsLogRedaction.FormatRecordTarget(request.RecordType, request.RecordValue),
            request.Ttl);
        recordId = NamesiloDns.DryRunRecordId;
        return true;
    }

    private bool TryDryRunUpdate(UpdateRecordRequest request)
    {
        if (!_options.Value.DryRun)
        {
            return false;
        }

        _logger.LogInformation(
            "Dry run: update {RecordId} {Host}.{Domain} ttl={Ttl}",
            request.RecordId,
            request.RecordHost,
            request.Domain,
            request.Ttl);
        return true;
    }

    private bool TryDryRunDelete(DeleteRecordRequest request)
    {
        if (!_options.Value.DryRun)
        {
            return false;
        }

        _logger.LogInformation("Dry run: delete {RecordId} from {Domain}", request.RecordId, request.Domain);
        return true;
    }

    private async Task<NamesiloApiResponse> SendAndReadAsync(
        string operation,
        INamesiloApiRequest request,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> parameters = request.ToQueryParameters();
        using HttpResponseMessage response = await SendAsync(operation, parameters, cancellationToken)
            .ConfigureAwait(false);

        return await ReadSuccessResponseAsync(
            response,
            operation,
            parameters.GetValueOrDefault("domain"),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<HttpResponseMessage> SendAsync(
        string operation,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken)
    {
        NamesiloOptions options = _options.Value;
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new NamesiloServiceException("NameSilo API key is not configured.");
        }

        Dictionary<string, string> queryParameters = new()
        {
            ["version"] = NamesiloApiDefaults.ApiVersion,
            ["type"] = NamesiloApiDefaults.JsonFormat,
            ["key"] = options.ApiKey,
        };

        foreach (KeyValuePair<string, string> pair in parameters)
        {
            queryParameters[pair.Key] = pair.Value;
        }

        using FormUrlEncodedContent encodedQuery = new(queryParameters);
        string query = await encodedQuery.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        string requestUri = operation + "?" + query;
        string? domain = parameters.GetValueOrDefault("domain");
        _logger.LogDebug("NameSilo API request {Operation} domain={Domain}", operation, domain);

        try
        {
            using HttpClient httpClient = _httpClientFactory.CreateClient(HttpClientNames.NameSilo);
            return await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "NameSilo API request failed for {Operation} domain={Domain}", operation, domain);
            throw new NamesiloServiceException($"NameSilo API request failed for {operation}.", ex);
        }
    }

    private async Task<NamesiloApiResponse> ReadSuccessResponseAsync(
        HttpResponseMessage response,
        string operation,
        string? domain,
        CancellationToken cancellationToken)
    {
        string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "NameSilo API HTTP {StatusCode} for {Operation} domain={Domain}",
                (int)response.StatusCode,
                operation,
                domain);
            _logger.LogDebug("NameSilo API error response preview: {BodyPreview}", Truncate(body));
            throw new NamesiloServiceException($"NameSilo API HTTP {(int)response.StatusCode}.");
        }

        NamesiloApiResponse apiResponse = NamesiloApiJson.DeserializeResponse(body);
        int code = NamesiloApiJson.ReadReplyCode(apiResponse);
        if (code != NamesiloDns.SuccessReplyCode)
        {
            _logger.LogWarning(
                "NameSilo API returned code {ReplyCode} for {Operation} domain={Domain}",
                code,
                operation,
                domain);
            _logger.LogDebug("NameSilo API error response preview: {BodyPreview}", Truncate(body));
            throw new NamesiloServiceException($"NameSilo API returned code {code}.");
        }

        return apiResponse;
    }
}
