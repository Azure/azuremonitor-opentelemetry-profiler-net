#nullable enable
//-----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------------------------

using System;

namespace Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators
{
    public class ValidateFailedException : Exception
    {
        /// <summary>
        /// Gets whether the validation exception should interrupt the uploading process.
        /// </summary>
        public bool ShouldStopUploading { get; }

        public ValidateFailedException() : this(message: null) { }
        public ValidateFailedException(string? message) : this(message, innerException: null) { }
        public ValidateFailedException(string? message, Exception? innerException) : this(message, innerException, toStopUploading: false) { }
        public ValidateFailedException(bool toStopUploading) : this(message: null, toStopUploading) { }
        public ValidateFailedException(string? message, bool toStopUploading) : this(message, innerException: null, toStopUploading) { }
        public ValidateFailedException(string? message, Exception? innerException, bool toStopUploading)
            : this(validatorName: null, message, innerException, toStopUploading) { }
        public ValidateFailedException(string? validatorName, string? message, bool toStopUploading) : this(validatorName, message, null, toStopUploading) { }

        public ValidateFailedException(string? validatorName, string? message, Exception? innerException, bool toStopUploading) : base(
            !string.IsNullOrEmpty(validatorName) ? "[" + validatorName + "] " + message : message, innerException)
        {
            ShouldStopUploading = toStopUploading;
        }
    }
}
