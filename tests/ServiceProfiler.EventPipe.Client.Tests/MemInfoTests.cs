using Microsoft.ApplicationInsights.Profiler.Shared.Orchestrations.MetricsProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.IO;
using System.Text;
using Xunit;

namespace ServiceProfiler.EventPipe.Client.Tests;

public class MemInfoTests 
{
    [Fact]
    public void ShouldGetMemoryMetrics()
    {
        byte[] lines = Encoding.UTF8.GetBytes(@"Buffers:                    421208 kB
MemTotal:                   16249772 kB
MemFree:                         958 kB
MemAvailable:               5771344 kB
");
        Mock<IMemInfoReader> memInfoReader = PrepareMemInfoReaderMock(lines);

        MemInfoFileMemoryMetricsProvider target = new MemInfoFileMemoryMetricsProvider(new MemInfoItemParser(), memInfoReader.Object, NullLogger<MemInfoFileMemoryMetricsProvider>.Instance);

        (float total, float free) = target.GetMetrics();

        Assert.Equal(16249772, total);
        Assert.Equal(5771344, free);
    }

    [Fact]
    public void ShouldGetMemoryUsage()
    {
        byte[] lines = Encoding.UTF8.GetBytes(@"Buffers:            421208 kB
MemTotal:           20 kB
Intentionally Empty line below for testing purpose: 30 kB

MemAvailable:            10 kB
");

        Mock<IMemInfoReader> memInfoReader = PrepareMemInfoReaderMock(lines);

        MemInfoFileMemoryMetricsProvider target = new MemInfoFileMemoryMetricsProvider(new MemInfoItemParser(), memInfoReader.Object, NullLogger<MemInfoFileMemoryMetricsProvider>.Instance);

        float rate = target.GetNextValue();

        Assert.Equal(50, rate);
    }

    [Fact]
    public void ShouldNotGetMemoryUsageWhenUnitsAreDifferent()
    {
        byte[] lines = Encoding.UTF8.GetBytes(@"Buffers:            421208 kB
MemTotal:           20 MB
MemAvailable:            10 kB
        ");

        Mock<IMemInfoReader> memInfoReader = PrepareMemInfoReaderMock(lines);

        MemInfoFileMemoryMetricsProvider target = new MemInfoFileMemoryMetricsProvider(new MemInfoItemParser(), memInfoReader.Object, NullLogger<MemInfoFileMemoryMetricsProvider>.Instance);
        float rate = target.GetNextValue();

        Assert.Equal(0, rate);
    }

    [Theory]
    [InlineData("MemTotal:       16249772 kB", true, "MemTotal", 16249772ul, "kB")]                  // Normal case, a real one from /proc/meminfo
    [InlineData(" Available:  123 Mb ", true, "Available", 123ul, "Mb")]                             // Leading spaces and tailing spaces.
    public void ShouldParseLine(string input, bool expectedResult, string expectedName, ulong expectedValue, string expectedUnit)
    {
        MemInfoItemParser testTarget = new MemInfoItemParser();
        bool result = testTarget.TryParse(input, out var metric);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedName, metric.name);
        Assert.Equal(expectedValue, metric.value);
        Assert.Equal(expectedUnit, metric.unit);
    }

    [Theory]
    [InlineData("222 kB", true, 222ul, "kB")]                            // Normal case
    [InlineData("9223372036854775807 KB", true, long.MaxValue, "KB")]   // Max value
    [InlineData(" 222 MB", true, 222ul, "MB")]                           // Leading space is ok.
    [InlineData("999 kb  ", true, 999ul, "kb")]                          // Tailing space is ok.
    [InlineData("222 ", false, 0, null)]                                // Unit is required.
    [InlineData("-128 Bytes", false, 0, null)]                          // Negative memory size doesn't make sense.
    public void ShouldParseValues(string input, bool expectedResult, ulong expectedValue, string expectedUnit)
    {
        MemInfoItemParser testTarget = new MemInfoItemParser();
        bool result = testTarget.TryParseMemInfoValue(input, out ulong actualValue, out string actualUnit);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedValue, actualValue);
        Assert.Equal(expectedUnit, actualUnit);
    }

    private Mock<IMemInfoReader> PrepareMemInfoReaderMock(byte[] content)
    {
        Mock<IMemInfoReader> memInfoReader = new();
        memInfoReader.Setup(r => r.Read(It.IsAny<string>())).Returns(() => new MemoryStream(content));

        return memInfoReader;
    }
}
