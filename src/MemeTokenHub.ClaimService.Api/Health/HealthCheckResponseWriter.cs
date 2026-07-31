using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MemeTokenHub.ClaimService.Api.Health;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        SortedDictionary<string, HealthCheckEntryResponse> checks = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, HealthReportEntry> reportEntry in report.Entries)
        {
            checks.Add(reportEntry.Key, new HealthCheckEntryResponse(
                reportEntry.Value.Status.ToString().ToLowerInvariant(),
                reportEntry.Value.Duration.TotalMilliseconds,
                reportEntry.Value.Description));
        }

        HealthCheckResponse response = new(
            report.Status.ToString().ToLowerInvariant(),
            report.TotalDuration.TotalMilliseconds,
            DateTimeOffset.UtcNow,
            checks);
        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsJsonAsync(response, SerializerOptions);
    }
}
