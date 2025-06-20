using Microsoft.ApplicationInsights.Profiler.Core.EventListeners;
using Microsoft.ApplicationInsights.Profiler.Shared.Services;
using Microsoft.ApplicationInsights.Profiler.Shared.Services.Abstractions;
using System;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests
{
    public class JsonSerializationProviderTests
    {
        [Fact]
        public void ShouldSerializeObjectInExpectedWay()
        {
            JsonSerializationTestDataContract payload = new JsonSerializationTestDataContract()
            {
                StringValue = "Hello XUniT!"
            };
            ISerializationProvider target = CreateTestTarget();
            bool canSerialize = target.TrySerialize(payload, out string actual);
            Assert.True(canSerialize);

            // Expectations:
            // 1. Property names are camel cased;
            // 2. Case in values are persisted;
            // 3. No indentation or newline;
            // 4. Enum is serialized to string;
            string expected = @"{""stringValue"":""Hello XUniT!"",""dataType"":""Unknown""}";
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ShouldDeserializePropertyCaseInSensitive()
        {
            string serialized = @"{""STRINGVALUE"":""Hello XUniT!"",""dataType"":""Unknown""}";
            ISerializationProvider target = CreateTestTarget();
            bool canDeserialize = target.TryDeserialize<JsonSerializationTestDataContract>(serialized, out JsonSerializationTestDataContract actual);
            Assert.True(canDeserialize);

            Assert.Equal("Hello XUniT!", actual.StringValue);
        }

        [Fact]
        public void ShouldDeserializeEnumFromString()
        {
            string serialized = @"{""stringValue"":""Hello XUniT!"",""dataType"":""Type1""}";
            ISerializationProvider target = CreateTestTarget();
            bool canDeserialize = target.TryDeserialize<JsonSerializationTestDataContract>(serialized, out JsonSerializationTestDataContract actual);
            Assert.True(canDeserialize);

            Assert.Equal(JsonSerializationTestDataType.Type1, actual.DataType);
        }

        [Fact]
        public void ShouldDeserializeEnumFromStringCaseInsensitive()
        {
            string serialized = @"{""stringValue"":""Hello XUniT!"",""dataType"":""type1""}";
            ISerializationProvider target = CreateTestTarget();
            bool canDeserialize = target.TryDeserialize<JsonSerializationTestDataContract>(serialized, out JsonSerializationTestDataContract actual);
            Assert.True(canDeserialize);

            Assert.Equal(JsonSerializationTestDataType.Type1, actual.DataType);
        }

        [Fact]
        public void ShouldNotDeserializeNull()
        {
            string serialized = null;
            ISerializationProvider target = CreateTestTarget();
            bool canDeserialize = target.TryDeserialize<JsonSerializationTestDataContract>(serialized, out JsonSerializationTestDataContract actual);
            Assert.False(canDeserialize);
            Assert.Null(actual);
        }

        [Fact]
        public void ShouldNotSerializeNull()
        {
            JsonSerializationTestDataContract payload = null;
            ISerializationProvider target = CreateTestTarget();
            bool canSerialize = target.TrySerialize<JsonSerializationTestDataContract>(payload, out string actual);
            Assert.False(canSerialize);
            Assert.Null(actual);
        }

        [Fact]
        public void ShouldSerializeDateTimeOffsetInExpectedWay()
        {
            string compare = "\"2013-01-20T00:00:00+00:00\"";
            DateTime startingDate = new DateTime(2013, 1, 20, 0, 0, 0, DateTimeKind.Utc);
            DateTimeOffset testOffset = new DateTimeOffset(startingDate);

            ISerializationProvider target = CreateTestTarget();
            bool canSerialize = target.TrySerialize<DateTimeOffset>(testOffset, out string actual);

            Assert.True(canSerialize);
            Assert.Equal(actual, compare);
        }

        [Fact]
        public void ShouldIgnoreParams()
        {
            string compare = "{\"TimeStamp\":\"0001-01-01T00:00:00+00:00\",\"EventName\":\"FakeEvent\",\"EventId\":0,\"Payload\":[{\"SomeData\":\"SomeDataHere\",\"BooleanData\":false}]}";
            ISerializationProvider target = CreateTestTarget();

            // Generate object from string
            target.TryDeserialize<object[]>("[{\"SomeData\":\"SomeDataHere\",\"BooleanData\":false}]", out object[] payload);
            // FakePayload payload = new FakePayload();
            ApplicationInsightsOperationEvent classWithIgnore = new ApplicationInsightsOperationEvent();
            classWithIgnore.EventName = "FakeEvent";
            classWithIgnore.Payload = payload;

            bool canSerialize = target.TrySerialize<ApplicationInsightsOperationEvent>(classWithIgnore, out string actual);

            Assert.True(canSerialize);
            Assert.Equal(actual, compare);
        }

        [Theory]
        [InlineData(@"{""stringValue"":256,""dataType"":""type1""}")]       // 256 is a number, allowed in value
        [InlineData(@"{""stringValue"":256,""dataType"":1}")]               // 1 is a enum value, but it isn't in string format.
        public void ShouldAllowDeserializeCompatibleConverts(string serialized)
        {
            ISerializationProvider target = CreateTestTarget();
            bool canDeserialize = target.TryDeserialize<JsonSerializationTestDataContract>(serialized, out JsonSerializationTestDataContract actual);
            Assert.True(canDeserialize);
        }

        [Theory]
        [InlineData("abc")] // Not a json
        [InlineData(@"{""stringValue"":""Hello XUniT!""")] // Malformat json
        public void ShouldNotThrowDeserializing(string serialized)
        {
            ISerializationProvider target = CreateTestTarget();
            bool canDeserialize = target.TryDeserialize<JsonSerializationTestDataContract>(serialized, out JsonSerializationTestDataContract actual);
            Assert.False(canDeserialize);
        }

        private ISerializationProvider CreateTestTarget()
            => new HighPerfJsonSerializationProvider();
    }
}
