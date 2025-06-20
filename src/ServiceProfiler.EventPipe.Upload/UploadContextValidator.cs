using Microsoft.ApplicationInsights.Profiler.Core.Contracts;
using System;
using System.IO;
using System.Text;

namespace Microsoft.ApplicationInsights.Profiler.Core.Utilities
{
    internal class UploadContextValidator : IUploadContextValidator
    {
        private readonly Func<string, bool> _fileExists;

        public UploadContextValidator(
            Func<string, bool>? fileExists = null)
        {
            _fileExists = fileExists ?? ((filePath) => File.Exists(filePath));
        }

        /// <summary>
        /// Validate the context is ready for uploading.
        /// </summary>
        /// <returns>Returns error message when it is not ready. Returns null if it is ready.</returns>
        public string Validate(UploadContext uploadContext)
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

            if (!string.IsNullOrEmpty(uploadContext.SerializedSampleFilePath) && !_fileExists(uploadContext.SerializedSampleFilePath))
            {
                errorMessageBuilder.AppendLine($"Serialized sample file doesn't exist. File path: {uploadContext.SerializedSampleFilePath}.");
            }

            if (errorMessageBuilder.Length == 0)
            {
                return string.Empty;
            }

            return errorMessageBuilder.ToString();
        }
    }
}
