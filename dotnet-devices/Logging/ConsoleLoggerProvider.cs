using System.CommandLine;
using Microsoft.Extensions.Logging;

namespace DotNetDevices.Logging
{
    internal class ConsoleLoggerProvider : ILoggerProvider
    {
        private readonly IConsole console;
        private readonly LogLevel logLevel;

        public ConsoleLoggerProvider(IConsole console, LogLevel logLevel)
        {
            this.console = console;
            this.logLevel = logLevel;
        }

        public ILogger CreateLogger(string name)
        {
            return new ConsoleLogger(console, logLevel);
        }

        public void Dispose()
        {
        }
    }
}
