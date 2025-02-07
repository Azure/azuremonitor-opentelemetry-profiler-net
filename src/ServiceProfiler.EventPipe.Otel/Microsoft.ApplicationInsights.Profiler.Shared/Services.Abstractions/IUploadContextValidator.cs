using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;

namespace Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

internal interface IUploadContextValidator
{
    /// <summary>
    /// Validate the upload context.
    /// </summary>
    /// <param name="uploadContext">Validation target.</param>
    /// <returns>Returns null when there is no error. Otherwise, returns the error message.</returns>
    string? Validate(UploadContextModel uploadContext);
}