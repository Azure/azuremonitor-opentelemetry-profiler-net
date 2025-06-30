using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights.Profiler.Shared.Contracts;
using Microsoft.ApplicationInsights.Profiler.Uploader.TraceValidators;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ServiceProfiler.EventPipe.Upload.Tests
{
    public class TraceValidatorTests : TestsBase, IClassFixture<TraceFileFixture>
    {

        public TraceValidatorTests(TraceFileFixture traceFileFixture)
        {
            _traceFileFixture = traceFileFixture;
        }

        [Fact]
        public void CanValidateV3File()
        {
            List<SampleActivity> sampleActivities = new List<SampleActivity>() {
                new SampleActivity(){ StartActivityIdPath="/#2178/1/1/1/", StopActivityIdPath="/#2178/1/1/1/" },
                new SampleActivity(){ StartActivityIdPath="//1/2/", StopActivityIdPath="//1/2/" },
            };

            string netperfPath = @"TestDeployments\8053A4AA-3A58-4B43-9AC4-E64C3CC6D50B.netperf";
            ITraceValidator validator = new EventPipeTraceValidatorFactory(GetServiceProvider(),
               GetLogger<EventPipeTraceValidatorFactory>()).Create(netperfPath);
            validator.Validate(sampleActivities);
        }

        [Fact]
        public void ActivityListValidatorShallPass()
        {
            List<SampleActivity> sampleActivities = new List<SampleActivity>() {
                new SampleActivity(){ StartActivityIdPath="//1/1/", StopActivityIdPath="//1/1/"},
                new SampleActivity(){ StartActivityIdPath="//1/2/", StopActivityIdPath="//1/2/"},
            };
            string netperfPath = @"TestDeployments\Good.netperf";
            ActivityListValidator target = new ActivityListValidator(netperfPath, GetLogger<ActivityListValidator>(), null);
            IEnumerable<SampleActivity> validSamples = target.Validate(sampleActivities);

            Assert.Equal(sampleActivities.Count, validSamples.Count());
        }

        [Fact]
        public void AcitivtyListValidatorFiltersOutUnpairedSamples()
        {
            List<SampleActivity> sampleActivities = new List<SampleActivity>(){
                new SampleActivity(){ StartActivityIdPath="/#1/1/1/1/", StopActivityIdPath="/#1/1/1/1/",  }, // This activity has stop but no start event.
                new SampleActivity(){ StartActivityIdPath="/#1/1/6/1/", StopActivityIdPath="/#1/1/6/1/",  }, // This activity should match both events.
                new SampleActivity(){ StartActivityIdPath="/#1/1/12/1/", StopActivityIdPath="/#1/1/12/1/", }, // This activity has start but no stop event.
            };
            string netperfPath = @"TestDeployments\NotFullyMatchedActivityPairs.netperf";
            ActivityListValidator target = new ActivityListValidator(netperfPath, GetLogger<ActivityListValidator>(), null);
            IEnumerable<SampleActivity> validSamples = target.Validate(sampleActivities);
            Assert.Single(validSamples);
        }

        [Fact]
        public void ActivityListValidatorReturnOnlyValidActivities()
        {
            List<SampleActivity> sampleActivities = new List<SampleActivity>() {
                new SampleActivity(){ StartActivityIdPath="//1/1/", StopActivityIdPath="//1/1/"},
                new SampleActivity(){ StartActivityIdPath="//1/1233/", StopActivityIdPath="//1/1233/"},
            };
            string netperfPath = @"TestDeployments\Good.netperf";
            ActivityListValidator target = new ActivityListValidator(netperfPath, GetLogger<ActivityListValidator>(), null);
            IEnumerable<SampleActivity> validSamples = target.Validate(sampleActivities);

            Assert.NotNull(validSamples);
            Assert.Single(validSamples);
            Assert.Equal("//1/1/", validSamples.First().StartActivityIdPath);
        }

        [Fact]
        public void ActivityListValidatorShallFailWhenNoHit()
        {
            List<SampleActivity> sampleActivities = new List<SampleActivity>() {
                new SampleActivity(){ StartActivityIdPath="//999/1/", StopActivityIdPath="//999/1/"},
            };
            string netperfPath = @"TestDeployments\Good.netperf";
            ActivityListValidator target = new ActivityListValidator(netperfPath, GetLogger<ActivityListValidator>(), null);

            Exception ex = Assert.Throws<ValidateFailedException>(() => { target.Validate(sampleActivities); });

            Assert.NotNull(ex);
            Assert.Equal("[ActivityListValidator] No sample activity matches the trace.", ex.Message);
        }

        [Fact]
        public void ReaderWriterInConsistentNetperfValidationFail()
        {
            List<SampleActivity> sampleActivities = new List<SampleActivity>() {
                new SampleActivity(){ StartActivityIdPath="//1/1/", StopActivityIdPath="//1/1/"},
                new SampleActivity(){ StartActivityIdPath="//1/2/", StopActivityIdPath="//1/2/"}
            };
            string netperfPath = @"TestDeployments\OnceDeadloop.netperf";
            ConvertTraceToEtlxValidator target = new ConvertTraceToEtlxValidator(
                netperfPath, GetLogger<ConvertTraceToEtlxValidator>(), null);

            Exception ex = Assert.Throws<ValidateFailedException>(() => target.Validate(sampleActivities));
            Assert.NotNull(ex);
            Assert.Equal("[ConvertTraceToEtlxValidator] Read past end of stream.", ex.Message);
        }

        [Fact]
        public void ChainUpAllValidatorShouldPass()
        {
            List<SampleActivity> sampleActivities = new List<SampleActivity>() {
                new SampleActivity(){ StartActivityIdPath="//1/1/", StopActivityIdPath="//1/1/", },
                new SampleActivity(){ StartActivityIdPath="//1/2/", StopActivityIdPath="//1/2/", }
            };
            string netperfPath = @"TestDeployments\Good.netperf";

            ConvertTraceToEtlxValidator target = new ConvertTraceToEtlxValidator(
                netperfPath, GetLogger<ConvertTraceToEtlxValidator>(),
                new ActivityListValidator(netperfPath, GetLogger<ActivityListValidator>(), null)
                );

            target.Validate(sampleActivities);
            // Expect no exception.
        }

        [Fact]
        public void EventPipeTraceValidatorFactoryShouldPassGoodTrace()
        {
            List<SampleActivity> sampleActivities = new List<SampleActivity>() {
                new SampleActivity(){ StartActivityIdPath="//1/1/", StopActivityIdPath="//1/1/",},
                new SampleActivity(){ StartActivityIdPath="//1/2/", StopActivityIdPath="//1/2/",}
            };
            string netperfPath = @"TestDeployments\Good.netperf";

            ITraceValidator validator = new EventPipeTraceValidatorFactory(GetServiceProvider(),
                GetLogger<EventPipeTraceValidatorFactory>()).Create(netperfPath);

            validator.Validate(sampleActivities);
            // Expect no exception.
        }

        protected override ServiceCollection CreateServiceCollection()
        {
            ServiceCollection collection = new ServiceCollection();
            collection.AddLogging();
            return collection;
        }

        private readonly TraceFileFixture _traceFileFixture;
    }
}
