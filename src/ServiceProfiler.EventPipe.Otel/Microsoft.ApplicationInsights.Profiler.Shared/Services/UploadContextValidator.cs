
using System;
using System.Text;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;

namespace Microsoft.ApplicationInsights.Profiler.Core.Utilities;

internal class UploadContextValidator(IFile file) : IUploadContextValidator
{
    private readonly IFile _file = file ?? throw new ArgumentNullException(nameof(file));

    /// <summary>
    /// Validate the context is ready for uploading.
    /// </summary>
    /// <returns>Returns error message when it is not ready. Returns null if it is ready.</returns>
    public string? Validate(UploadContextModel uploadContext)
    {
        if (uploadContext == null)
        {
            return $"{nameof(uploadContext)} is required.";
        }

        StringBuilder errorMessageBuilder = new StringBuilder();

        if (uploadContext.AIInstrumentationKey == Guid.Empty)
        {
            errorMessageBuilder.AppendLine($"{nameof(uploadContext.AIInstrumentationKey)} is required.");
        }

        if (uploadContext.HostUrl == default)
        {
            errorMessageBuilder.AppendLine($"{nameof(uploadContext.HostUrl)} is required.");
        }

        if (uploadContext.SessionId == default(DateTimeOffset))
        {
            errorMessageBuilder.AppendLine($"{nameof(uploadContext.SessionId)} is required.");
        }

        if (string.IsNullOrEmpty(uploadContext.SerializedSampleFilePath) && string.IsNullOrEmpty(uploadContext.PipeName))
        {
            errorMessageBuilder.AppendLine($"{nameof(uploadContext.SerializedSampleFilePath)} and {nameof(uploadContext.PipeName)} can't be null at the same time.");
        }

        if (!string.IsNullOrEmpty(uploadContext.SerializedSampleFilePath) && !_file.Exists(uploadContext.SerializedSampleFilePath!))
        {
            errorMessageBuilder.AppendLine($"Serialized sample file doesn't exist. File path: {uploadContext.SerializedSampleFilePath}.");
        }

        if (errorMessageBuilder.Length == 0)
        {
            return null;
        }

        return errorMessageBuilder.ToString();
    }
}
