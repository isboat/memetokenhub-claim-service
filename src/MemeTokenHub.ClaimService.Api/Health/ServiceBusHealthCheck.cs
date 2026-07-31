using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MemeTokenHub.ClaimService.Api.Health;

public sealed class ServiceBusHealthCheck(ServiceBusClient serviceBusClient, ServiceBusSender serviceBusSender) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (serviceBusClient.IsClosed || serviceBusSender.IsClosed)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "The Azure Service Bus client is closed.");
        }

        try
        {
            using ServiceBusMessageBatch messageBatch = await serviceBusSender.CreateMessageBatchAsync(cancellationToken);
            return HealthCheckResult.Healthy("Azure Service Bus is reachable.");
        }
        catch (Exception exception) when (exception is ServiceBusException or TimeoutException or TaskCanceledException)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "Azure Service Bus is unavailable.",
                exception);
        }
    }
}
