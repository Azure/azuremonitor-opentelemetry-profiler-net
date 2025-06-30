using Microsoft.ApplicationInsights.Profiler.Core.Contracts;

namespace Microsoft.ApplicationInsights.Profiler.Core.Utilities
{
    internal interface IUploadContextValidator
    {
        /// <summary>
        /// Validate the upload context.
        /// </summary>
        /// <param name="uploadContext">Validation target.</param>
        /// <returns>Returns null when there is no error. Otherwise, returns the error message.</returns>
        string Validate(UploadContext uploadContext);
    }
}