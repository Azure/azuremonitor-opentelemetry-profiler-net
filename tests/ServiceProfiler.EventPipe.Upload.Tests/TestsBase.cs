using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace ServiceProfiler.EventPipe.Upload.Tests
{
    public class TestsBase
    {
        public IServiceProvider GetServiceProvider()
            => CreateServiceCollection().BuildServiceProvider();

        protected virtual ServiceCollection CreateServiceCollection()
            => new ServiceCollection();

        public ILoggerFactory GetLoggerFactory()
        {
            Mock<ILoggerFactory> loggerFactoryMock = new Mock<ILoggerFactory>();
            return loggerFactoryMock.Object;
        }

        public ILogger<T> GetLogger<T>()
        {
            Mock<ILogger<T>> loggerMock = new Mock<ILogger<T>>();
            return loggerMock.Object;
        }
    }
}