using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace Web.Api.Infrastructure;

internal sealed class IpRateLimiter(IOptions<ApiRateLimitingOptions> options) : IDisposable
{
    private readonly PartitionedRateLimiter<HttpContext> _limiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var ip = context.Connection.RemoteIpAddress;
        if (ip?.IsIPv4MappedToIPv6 == true)
            ip = ip.MapToIPv4();
        return RateLimitPartition.GetFixedWindowLimiter(ip?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.Value.IpPermitLimit,
            Window = TimeSpan.FromSeconds(options.Value.WindowSeconds),
            QueueLimit = 0
        });
    });

    internal RateLimitLease Acquire(HttpContext context) => _limiter.AttemptAcquire(context);

    public void Dispose() => _limiter.Dispose();
}
