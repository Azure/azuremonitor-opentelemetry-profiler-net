using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.ApplicationInsights.Extensibility;

internal sealed class DisposableSdkInternalOperationsMonitor : IDisposable
{
    private readonly ILogger _logger;

    public DisposableSdkInternalOperationsMonitor(ILogger<DisposableSdkInternalOperationsMonitor> logger)
    {
        SdkInternalOperationsMonitor.Enter();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Dispose()
    {
        if (!SdkInternalOperationsMonitor.IsEntered())
        {
            _logger.LogWarning("Trying to exit application insights internal operation monitor while not in one.");
            return;
        }

        SdkInternalOperationsMonitor.Exit();
    }
}
