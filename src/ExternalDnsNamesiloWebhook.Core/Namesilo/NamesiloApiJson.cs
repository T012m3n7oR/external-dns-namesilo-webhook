using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExternalDnsNamesiloWebhook.Core.Constants;
using ExternalDnsNamesiloWebhook.Core.Namesilo.Models;

namespace ExternalDnsNamesiloWebhook.Core.Namesilo;

public static class NamesiloApiJson
{
    public static JsonSerializerOptions SerializerOptions { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new JsonStringEnumConverter(),
            new NamesiloResourceRecordListConverter(),
        },
    };

    public static NamesiloApiResponse DeserializeResponse(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<NamesiloApiResponse>(body, SerializerOptions)
                ?? throw new NamesiloServiceException("NameSilo API response was empty.");
        }
        catch (JsonException ex)
        {
            throw new NamesiloServiceException("NameSilo API response was not valid JSON.", ex);
        }
    }

    public static string SerializeResponse(NamesiloApiResponse response)
    {
        return JsonSerializer.Serialize(response, SerializerOptions);
    }

    public static string SerializeReply(int code, string detail = NamesiloApiDefaults.SuccessDetail)
    {
        return SerializeResponse(new NamesiloApiResponse
        {
            Reply = new NamesiloApiReply
            {
                Code = code,
                Detail = detail,
            },
        });
    }

    public static string SerializeSuccessReply(
        int code = NamesiloDnsConstants.SuccessReplyCode,
        string detail = NamesiloApiDefaults.SuccessDetail)
    {
        return SerializeReply(code, detail);
    }

    public static string SerializeListRecordsReply(params NamesiloDnsRecord[] records)
    {
        return SerializeResponse(new NamesiloApiResponse
        {
            Reply = new NamesiloApiReply
            {
                Code = NamesiloDnsConstants.SuccessReplyCode,
                Detail = NamesiloApiDefaults.SuccessDetail,
                ResourceRecords = records,
            },
        });
    }

    public static string SerializeSingleObjectListRecordsReply(NamesiloDnsRecord record)
    {
        return JsonSerializer.Serialize(
            new NamesiloSingleResourceRecordResponse
            {
                Reply = new NamesiloSingleResourceRecordReply
                {
                    Code = NamesiloDnsConstants.SuccessReplyCode,
                    Detail = NamesiloApiDefaults.SuccessDetail,
                    ResourceRecord = record,
                },
            },
            SerializerOptions);
    }

    public static int ReadReplyCode(NamesiloApiResponse response)
    {
        if (response.Reply is not null)
        {
            return response.Reply.Code;
        }

        if (response.Code is int code)
        {
            return code;
        }

        throw new NamesiloServiceException("NameSilo API response did not include a status code.");
    }
}
