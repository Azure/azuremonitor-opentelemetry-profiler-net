namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

public interface ISerializationOptionsProvider<T>
{
    T Options { get; }
}
