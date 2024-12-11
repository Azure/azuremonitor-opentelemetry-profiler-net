namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions.IPC;

/// <summary>
/// Any serializer implementation that can serialize an object as a payload for named pipe
/// and deserialize the message back to objects.
/// </summary>
public interface IPayloadSerializer
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
