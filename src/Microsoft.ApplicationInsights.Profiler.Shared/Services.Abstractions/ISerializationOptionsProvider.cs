namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface ISerializationOptionsProvider<T>
{
    T Options { get; }
}
