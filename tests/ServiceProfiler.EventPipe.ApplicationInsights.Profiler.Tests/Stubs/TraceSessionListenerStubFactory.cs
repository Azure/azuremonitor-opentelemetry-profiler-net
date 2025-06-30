//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using Microsoft.ApplicationInsights.Profiler.Core.EventListeners;
using Microsoft.ApplicationInsights.Profiler.Shared.Samples;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    internal class TraceSessionListenerStubFactory : TraceSessionListenerFactory
    {
        private readonly IOptions<UserConfiguration> _userConfiguration;
        private readonly ISerializationOptionsProvider<JsonSerializerOptions> _jsonSerializerOptionsProvider;

        private ISerializationProvider _serializer { get; }

        public TraceSessionListenerStubFactory(
            SampleActivityContainerFactory sampleActivityContainerFactory,
            IVersionProvider versionProvider,
            IOptions<UserConfiguration> userConfiguration,
            ISerializationProvider serializer,
            ISerializationOptionsProvider<JsonSerializerOptions> jsonSerializerOptionsProvider,
            ILoggerFactory loggerFactory)
            : base(sampleActivityContainerFactory, versionProvider, serializer, jsonSerializerOptionsProvider, loggerFactory)
        {
            _userConfiguration = userConfiguration;
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _jsonSerializerOptionsProvider = jsonSerializerOptionsProvider ?? throw new ArgumentNullException(nameof(jsonSerializerOptionsProvider));
        }

        public override ITraceSessionListener CreateTraceSessionListener()
        {
            return new TraceSessionListenerStub(
                _sampleActivityContainerFactory,
                _userConfiguration,
                _serializer,
                _jsonSerializerOptionsProvider,
                _loggerFactory.CreateLogger<TraceSessionListenerStub>());
        }

        public override IEnumerable<ITraceSessionListener> CreateTraceSessionListeners()
        {
            yield return CreateTraceSessionListener();
        }
    }
}
