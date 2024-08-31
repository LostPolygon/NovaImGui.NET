using Microsoft.Extensions.Logging;

namespace NovaImGuiNet.Generator;

public static class Logging {
    private static ILoggerFactory loggerFactory = null!;

    public static ILogger Create<T>() => loggerFactory.CreateLogger(typeof(T).Name);

    public static ILogger Create(string name) => loggerFactory.CreateLogger(name);

    public static void SetLoggerFactory(ILoggerFactory factory) {
        if (loggerFactory != null)
            throw new InvalidOperationException("Logger factory already set");

        loggerFactory = factory;
    }
}
