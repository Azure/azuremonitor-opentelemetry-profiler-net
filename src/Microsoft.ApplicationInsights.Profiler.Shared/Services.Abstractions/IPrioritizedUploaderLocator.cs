//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface IPrioritizedUploaderLocator
{
    /// <summary>
    /// Sets the priority of the upload locator.
    /// Multiple upload locators will register to the service collector to form up an IEnumerable<IUploaderLocator>,
    /// be injected and be executed in the order of the priorities. In this project, we assumes smaller value takes high priority and the locator will executes first.
    /// Any success call on Locate, which returns non-null value, will terminate the execution.
    /// Technically, priority could be set as any valid integer. It is suggested the implementations takes 0 as the highest priority
    /// and leave some gaps between priorities in case new locator needed in between the existing ones.
    /// As a simple implementation, there's no logic to handle same priority situations and the sequence of execution of those locators are undetermined.
    /// </summary>
    int Priority { get;}

    /// <summary>
    /// Locate the target file or directory.
    /// </summary>
    /// <returns>Returns the location of the found file or directory. Returns null when the target doesn't exist.</returns>
    string? Locate();
}