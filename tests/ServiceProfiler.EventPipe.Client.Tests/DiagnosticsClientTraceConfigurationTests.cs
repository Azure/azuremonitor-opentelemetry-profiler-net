//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.ApplicationInsights.Profiler.Core.TraceControls;
using Microsoft.Diagnostics.NETCore.Client;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public class DiagnosticsClientTraceConfigurationTests : TestsBase
    {
        [Fact]
        public void ShouldHaveProperDefaultValues()
        {
            var target = GetRichServiceCollection().BuildServiceProvider().GetRequiredService<DiagnosticsClientTraceConfiguration>();

            Assert.Equal(250, target.CircularBufferMB);
            Assert.True(target.RequestRundown);
            Assert.NotNull(target.BuildEventPipeProviders());

            var expectedProviders = new List<EventPipeProvider>(){
                new EventPipeProvider("Microsoft-Windows-DotNETRuntime", EventLevel.Verbose, 0x4c14fccbd, null),
                // Private provider.
                new EventPipeProvider("Microsoft-Windows-DotNETRuntimePrivate", EventLevel.Verbose, 0x4002000b,null),
                // Sample profiler.
                new EventPipeProvider("Microsoft-DotNETCore-SampleProfiler", EventLevel.Verbose,  0x0, null),
                // TPL.
                new EventPipeProvider("System.Threading.Tasks.TplEventSource", EventLevel.Verbose, 0x1 | 0x2 | 0x4 | 0x40 | 0x80, null),
                // Microsoft-ApplicationInsights-DataRelay
                new EventPipeProvider("Microsoft-ApplicationInsights-DataRelay", EventLevel.Verbose, keywords:0xffffffff, arguments: null),
            };

            Assert.Collection(target.BuildEventPipeProviders(),
                item1 => item1.Equals(expectedProviders[0]),
                item2 => item2.Equals(expectedProviders[1]),
                item3 => item3.Equals(expectedProviders[2]),
                item4 => item4.Equals(expectedProviders[3]),
                item5 => item5.Equals(expectedProviders[4]));
        }
    }
}