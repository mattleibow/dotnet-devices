using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetDevices.Logging
{
    public static class LoggingExtensions
    {
        public static ILogger<T> AddLogging<T>(this IConsole console, string? verbosity)
        {
            var logLevel = GetLogLevel(verbosity);

            var factory = new LoggerFactory();
            factory.AddProvider(new ConsoleLoggerProvider(console, logLevel));

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(factory);
            serviceCollection.AddLogging();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var logger = serviceProvider.GetService<ILogger<T>>();

            return logger;
        }

        public static ILogger CreateLogger(this IConsole console, string? verbosity)
        {
            var logLevel = GetLogLevel(verbosity);
            return new ConsoleLogger(console, logLevel);
        }

        public static LogLevel GetLogLevel(string? verbosity)
        {
            switch (verbosity)
            {
                case "q":
                case "quiet":
                    return LogLevel.Error;
                case "m":
                case "minimal":
                    return LogLevel.Warning;
                case "n":
                case "normal":
                    return LogLevel.Information;
                case "d":
                case "detailed":
                    return LogLevel.Debug;
                case "diag":
                case "diagnostic":
                    return LogLevel.Trace;
                default:
                    return LogLevel.Information;
            }
        }
    }
}
