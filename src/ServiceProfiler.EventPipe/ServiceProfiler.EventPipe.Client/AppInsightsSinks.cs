using System.Collections.Generic;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Profiler.Core.Logging;

namespace Microsoft.ApplicationInsights.Profiler.Core.Orchestration
{
    internal sealed class AppInsightsSinks : IAppInsightsSinks
    {
        public AppInsightsSinks(IEnumerable<IAppInsightsLogger> loggers)
        {
            _loggers = loggers;
        }

        public void LogInformation(string message)
        {
            foreach (IAppInsightsLogger logger in _loggers)
            {
                logger.TrackTrace(message, SeverityLevel.Information);
            }
        }

        private readonly IEnumerable<IAppInsightsLogger> _loggers;
    }
}
