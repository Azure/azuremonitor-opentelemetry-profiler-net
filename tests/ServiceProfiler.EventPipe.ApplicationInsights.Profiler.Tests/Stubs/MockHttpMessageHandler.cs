//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    [ExcludeFromCodeCoverage]
    internal class MockHttpMessageHandler : DelegatingHandler
    {
        public readonly Dictionary<string, HttpResponseMessage> AvailableResponses = new Dictionary<string, HttpResponseMessage>(StringComparer.InvariantCultureIgnoreCase);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (AvailableResponses.TryGetValue(request.RequestUri.AbsoluteUri, out HttpResponseMessage responseMessage))
            {
                return Task.FromResult(responseMessage);
            }
            else
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request });
            }
        }
    }
}
