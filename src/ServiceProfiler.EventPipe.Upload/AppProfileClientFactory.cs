using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ServiceProfiler.Agent;

namespace Microsoft.ApplicationInsights.Profiler.Uploader
{
    /// <inheritdoc />
    internal class AppProfileClientFactory : IAppProfileClientFactory
    {
        private readonly IngestionClientOptions _options;
        private readonly ILoggerFactory _loggerFactory;

        public AppProfileClientFactory(
            IOptions<IngestionClientOptions> options,
            ILoggerFactory loggerFactory
            )
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <inheritdoc />
        public IAppProfileClient Create(UploadContextExtension uploadContext)
        {
            if (uploadContext is null)
            {
                throw new ArgumentNullException(nameof(uploadContext));
            }

            return new IngestionClient(CustomizeIngestionClientOptions(uploadContext), _loggerFactory.CreateLogger<IngestionClient>());
        }

        internal IOptions<IngestionClientOptions> CustomizeIngestionClientOptions(UploadContextExtension uploadContext)
        {
            _options.TokenCredential = uploadContext.TokenCredential;
            return Options.Create(_options);
        }
    }
}
