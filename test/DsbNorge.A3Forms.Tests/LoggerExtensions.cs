using Microsoft.Extensions.Logging;
using Moq;

namespace DsbNorge.A3Forms.Tests;

public static class LoggerExtensions
{
    public static void VerifyNoLogging<T>(this Mock<ILogger<T>> logger)
    {
        logger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<object>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<object, Exception, string>>()!),
            Times.Never());
    }

    public static void VerifyErrorLogging<T>(this Mock<ILogger<T>> logger, string expectedLogMessage)
    {
        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedLogMessage)),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
            ),
            Times.Once
        );
    }
    
}