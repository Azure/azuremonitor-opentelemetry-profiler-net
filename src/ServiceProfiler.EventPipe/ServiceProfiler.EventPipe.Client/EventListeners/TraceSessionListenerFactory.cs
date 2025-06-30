//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.ApplicationInsights.Profiler.Shared.Samples;

namespace Microsoft.ApplicationInsights.Profiler.Core.EventListeners
{
    internal class TraceSessionListenerFactory : ITraceSessionListenerFactory
    {
        protected readonly ILoggerFactory _loggerFactory;
        protected readonly SampleActivityContainerFactory _sampleActivityContainerFactory;
        private readonly IVersionProvider _versionProvider;
        private readonly ISerializationOptionsProvider<JsonSerializerOptions> _serializerOptions;
        private ISerializationProvider _serializer;

        public TraceSessionListenerFactory(
            SampleActivityContainerFactory sampleActivityContainerFactory,
            IVersionProvider versionProvider,
            ISerializationProvider serializer,
            ISerializationOptionsProvider<JsonSerializerOptions> serializerOptions,
            ILoggerFactory loggerFactory)
        {
            _sampleActivityContainerFactory = sampleActivityContainerFactory ?? throw new ArgumentNullException(nameof(sampleActivityContainerFactory));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _versionProvider = versionProvider ?? throw new ArgumentNullException(nameof(versionProvider));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _serializerOptions = serializerOptions ?? throw new ArgumentNullException(nameof(serializerOptions));
        }

        public virtual ITraceSessionListener CreateTraceSessionListener()
        {
            // Refer to: https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-core-3-0#improved-net-core-version-apis
            switch (_versionProvider.RuntimeVersion?.Major)
            {
                case 2:
                case 4:
                    return new TraceSessionListener(_sampleActivityContainerFactory, _serializer, _serializerOptions, _loggerFactory.CreateLogger<TraceSessionListener>());
                case 3:
                    return new TraceSessionListener30(_sampleActivityContainerFactory, _serializer, _serializerOptions, _loggerFactory.CreateLogger<TraceSessionListener30>());
                default:
                    throw new ArgumentException("Unknown version of .NET Core Runtime", nameof(_versionProvider.RuntimeVersion.Major));
            }
        }

        public virtual IEnumerable<ITraceSessionListener> CreateTraceSessionListeners()
        {
            yield return new TraceSessionListener30(_sampleActivityContainerFactory, _serializer, _serializerOptions, _loggerFactory.CreateLogger<TraceSessionListener30>());
            yield return new TraceSessionListener(_sampleActivityContainerFactory, _serializer, _serializerOptions, _loggerFactory.CreateLogger<TraceSessionListener>());
        }
    }
}
