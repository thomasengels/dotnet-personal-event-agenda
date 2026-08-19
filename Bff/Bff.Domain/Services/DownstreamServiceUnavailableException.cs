namespace Bff.Domain.Services;

public sealed class DownstreamServiceUnavailableException(string serviceName, Exception? innerException = null)
    : Exception($"{serviceName} service is unavailable.", innerException)
{
    public string ServiceName { get; } = serviceName;
}
