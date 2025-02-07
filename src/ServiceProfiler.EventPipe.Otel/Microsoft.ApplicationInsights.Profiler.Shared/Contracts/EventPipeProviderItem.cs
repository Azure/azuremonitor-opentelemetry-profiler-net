//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using Microsoft.Diagnostics.NETCore.Client;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Tracing;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

/// <summary>
/// A serialization friendly object model of an event pipe provider
/// </summary>
public class EventPipeProviderItem
{
    /// <summary>
    /// Gets or sets the provider name. For example: Microsoft-Windows-DotNETRuntime.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the event level.
    /// </summary>
    public EventLevel EventLevel { get; set; }

    /// <summary>
    /// Gets or sets the keywords. Default to "0xF00000000000".
    /// </summary>
    public long Keywords { get; set; } = 0xF00000000000;

    /// <summary>
    /// Gets or sets the arguments for the event pipe provider.
    /// </summary>
    /// <value></value>
    public IDictionary<string, string> Arguments { get; set; } = ImmutableDictionary<string, string>.Empty;

    /// <summary>
    /// Converts to a <see cref="EventPipeProvider"/> object.
    /// </summary>
    public EventPipeProvider ToProvider() => new(Name, EventLevel, Keywords, Arguments);
}