namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

/// <summary>
/// An abstraction of serialization functionalities. This allows various implementations based on different technologies like
/// Newtonsoft JSON.NET or System.Text.Json.
/// For consistent output, the serializer provider shall:
/// * Use camelCase, non-indentation, for serialization. Enum will be serialized as string.
/// * Be case-insensitive for deserialization.
/// </summary>

public interface ISerializationProvider
{
    /// <summary>
    /// Tries to serialize an object and output the serialized object.
    /// </summary>
    /// <param name="obj">Object to be serialized.</param>
    /// <param name="serialized">Serialized result.</param>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <returns>Returns true when the serialization succeeded. Otherwise, false.</returns>
    bool TrySerialize<T>(T obj, out string? serialized);

    /// <summary>
    /// Tries to deserialize a string to an object.
    /// </summary>
    /// <param name="serialized">Serialized object.</param>
    /// <param name="obj">The deserialized object.</param>
    /// <typeparam name="T">The type of the deserialized result.</typeparam>
    /// <returns>Returns true when the deserialization succeeded. Otherwise, false.</returns>
    bool TryDeserialize<T>(string serialized, out T? obj);
}
