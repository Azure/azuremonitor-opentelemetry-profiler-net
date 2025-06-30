using System;
using Microsoft.ApplicationInsights.Profiler.Core.Utilities;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using Moq;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public class UploadContextValidatorTests
    {
        [Fact]
        public void ShouldReturnNullWhenNoError()
        {
            IFile fileUtility = CreateIFileMock().Object;
            IUploadContextValidator target = new UploadContextValidator(fileUtility);
            string actual = target.Validate(new UploadContextModel()
            {
                AIInstrumentationKey = Guid.NewGuid(),
                HostUrl = new Uri("https://endpoint", UriKind.Absolute),
                SessionId = DateTimeOffset.UtcNow,
                StampId = "stampId",
                TraceFilePath = @"c:\tracefilePath.etl.zip",
                MetadataFilePath = @"c:\metadataFilePath.metadata",
                PreserveTraceFile = false,
                SkipEndpointCertificateValidation = false,
                UploadMode = UploadMode.OnSuccess,
                SerializedSampleFilePath = @"c:\sample",
            });

            // Return null upon success.
            Assert.Null(actual);
        }

        [Fact]
        public void ShouldAcceptOptionalMetadataFilePath()
        {
            IFile fileUtility = CreateIFileMock().Object;
            IUploadContextValidator target = new UploadContextValidator(fileUtility);
            string actual = target.Validate(new UploadContextModel()
            {
                AIInstrumentationKey = Guid.NewGuid(),
                HostUrl = new Uri("https://endpoint"),
                SessionId = DateTimeOffset.UtcNow,
                StampId = "stampId",
                TraceFilePath = @"c:\tracefilePath.etl.zip",
                MetadataFilePath = null,
                PreserveTraceFile = false,
                SkipEndpointCertificateValidation = false,
                UploadMode = UploadMode.OnSuccess,
                SerializedSampleFilePath = @"c:\sample",
            });

            // Return null upon success.
            Assert.Null(actual);
        }

        [Fact]
        public void ShouldAcceptOptionalRoleName()
        {
            IFile fileUtility = CreateIFileMock().Object;
            IUploadContextValidator target = new UploadContextValidator(fileUtility);
            string actual = target.Validate(new UploadContextModel()
            {
                AIInstrumentationKey = Guid.NewGuid(),
                HostUrl = new Uri("https://endpoint"),
                SessionId = DateTimeOffset.UtcNow,
                StampId = "stampId",
                TraceFilePath = @"c:\tracefilePath.etl.zip",
                MetadataFilePath = null,
                PreserveTraceFile = false,
                SkipEndpointCertificateValidation = false,
                UploadMode = UploadMode.OnSuccess,
                SerializedSampleFilePath = @"c:\sample",
                RoleName = "RoleName",
                TriggerType = "HighCPU",
            });

            // Return null upon success.
            Assert.Null(actual);
        }

        [Fact]
        public void ShouldAcceptNullRoleName()
        {
            IFile fileUtility = CreateIFileMock().Object;
            IUploadContextValidator target = new UploadContextValidator(fileUtility);
            string actual = target.Validate(new UploadContextModel()
            {
                AIInstrumentationKey = Guid.NewGuid(),
                HostUrl = new Uri("https://endpoint"),
                SessionId = DateTimeOffset.UtcNow,
                StampId = "stampId",
                TraceFilePath = @"c:\tracefilePath.etl.zip",
                MetadataFilePath = null,
                PreserveTraceFile = false,
                SkipEndpointCertificateValidation = false,
                UploadMode = UploadMode.OnSuccess,
                SerializedSampleFilePath = @"c:\sample",
                RoleName = null,
                TriggerType = "HighCPU",
            });

            // Return null upon success.
            Assert.Null(actual);
        }

        [Fact]
        public void ShouldAcceptOptionalTriggerType()
        {
            IFile fileUtility = CreateIFileMock().Object;
            IUploadContextValidator target = new UploadContextValidator(fileUtility);
            string actual = target.Validate(new UploadContextModel()
            {
                AIInstrumentationKey = Guid.NewGuid(),
                HostUrl = new Uri("https://endpoint"),
                SessionId = DateTimeOffset.UtcNow,
                StampId = "stampId",
                TraceFilePath = @"c:\tracefilePath.etl.zip",
                MetadataFilePath = null,
                PreserveTraceFile = false,
                SkipEndpointCertificateValidation = false,
                UploadMode = UploadMode.OnSuccess,
                SerializedSampleFilePath = @"c:\sample",
                RoleName = "RoleName",
                TriggerType = "HighCPU"
            });

            // Return null upon success.
            Assert.Null(actual);
        }

        [Fact]
        public void ShouldAcceptNullTriggerType()
        {
            IFile fileUtility = CreateIFileMock().Object;
            IUploadContextValidator target = new UploadContextValidator(fileUtility);
            string actual = target.Validate(new UploadContextModel()
            {
                AIInstrumentationKey = Guid.NewGuid(),
                HostUrl = new Uri("https://endpoint"),
                SessionId = DateTimeOffset.UtcNow,
                StampId = "stampId",
                TraceFilePath = @"c:\tracefilePath.etl.zip",
                MetadataFilePath = null,
                PreserveTraceFile = false,
                SkipEndpointCertificateValidation = false,
                UploadMode = UploadMode.OnSuccess,
                SerializedSampleFilePath = @"c:\sample",
                RoleName = "MyRole",
                TriggerType = null
            });

            // Return null upon success.
            Assert.Null(actual);
        }

        [Fact]
        public void ShouldFailWhenSampleFileNotExist()
        {
            Mock<IFile> fileMock = CreateIFileMock(exists: () => false);
            string samplePath = @"c:\sample";
            IUploadContextValidator target = new UploadContextValidator(fileMock.Object);
            string actual = target.Validate(new UploadContextModel()
            {
                AIInstrumentationKey = Guid.NewGuid(),
                HostUrl = new Uri("https://endpoint"),
                SessionId = DateTimeOffset.UtcNow,
                StampId = "stampId",
                TraceFilePath = @"c:\tracefilePath.etl.zip",
                MetadataFilePath = null,
                PreserveTraceFile = false,
                SkipEndpointCertificateValidation = false,
                UploadMode = UploadMode.OnSuccess,
                SerializedSampleFilePath = samplePath,
            });

            // Return null upon success.
            Assert.Equal($"Serialized sample file doesn't exist. File path: {samplePath}." + Environment.NewLine, actual);
        }


        [Fact]
        public void ShouldReturnErrorWhenHostUriIsEmpty()
        {
            UploadContextModel context = new()
            {
                AIInstrumentationKey = Guid.NewGuid(),
                HostUrl = null,
                SessionId = DateTimeOffset.UtcNow,
                StampId = "stampId",
                TraceFilePath = @"c:\tracefilePath.etl.zip",
                MetadataFilePath = null,
                PreserveTraceFile = false,
                SkipEndpointCertificateValidation = false,
                UploadMode = UploadMode.OnSuccess,
                SerializedSampleFilePath = @"c:\sample",
            };

            TestImp(createContext: () => context, "HostUrl is required." + Environment.NewLine);
        }

        [Fact]
        public void ShouldAtLeastHaveEitherNamedPipeNameOrSerializedSampleFilePathSet()
        {
            UploadContextModel context = new()
            {
                AIInstrumentationKey = Guid.NewGuid(),
                HostUrl = new Uri("https://endpoint"),
                SessionId = DateTimeOffset.UtcNow,
                StampId = "stampId",
                TraceFilePath = @"c:\tracefilePath.etl.zip",
                MetadataFilePath = null,
                PreserveTraceFile = false,
                SkipEndpointCertificateValidation = false,
                UploadMode = UploadMode.OnSuccess,
                SerializedSampleFilePath = null,
                PipeName = null,
            };

            TestImp(() => context,
            "SerializedSampleFilePath and PipeName can't be null at the same time." + Environment.NewLine);
        }

        private void TestImp(Func<UploadContextModel> createContext, string expectedError)
        {
            Mock<IFile> fileMock = CreateIFileMock();

            IUploadContextValidator target = new UploadContextValidator(fileMock.Object);
            UploadContextModel context = createContext();
            string actual = target.Validate(context);

            Assert.Equal(expectedError, actual);
        }

        private Mock<IFile> CreateIFileMock(Func<bool> exists = null)
        {
            exists ??= () => true; // Default to true if not specified
            Mock<IFile> fileMock = new();
            fileMock.Setup(f => f.Exists(It.IsAny<string>())).Returns(exists());
            return fileMock;
        }
    }
}
