using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Rendering;
using Microsoft.Extensions.Logging;

namespace DotNetDevices.Logging
{
    internal class ConsoleLogger : ILogger
    {
        private readonly object locker = new object();
        private readonly IDisposable nullScope = new NullScope();

        private readonly IConsole console;
        private readonly ITerminal terminal;
        private readonly LogLevel logLevel;

        private static IReadOnlyDictionary<LogLevel, ConsoleColor> LogLevelColorMap =>
            new Dictionary<LogLevel, ConsoleColor>
            {
                [LogLevel.Critical] = ConsoleColor.Red,
                [LogLevel.Error] = ConsoleColor.Red,
                [LogLevel.Warning] = ConsoleColor.Yellow,
                [LogLevel.Information] = ConsoleColor.White,
                [LogLevel.Debug] = ConsoleColor.Gray,
                [LogLevel.Trace] = ConsoleColor.Gray,
                [LogLevel.None] = ConsoleColor.White,
            }.ToImmutableDictionary();

        public ConsoleLogger(IConsole console, LogLevel logLevel)
        {
            this.console = console ?? throw new ArgumentNullException(nameof(console));
            this.logLevel = logLevel;

            terminal = console.GetTerminal();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            lock (locker)
            {
                var message = formatter(state, exception);
                message = $"{message}{Environment.NewLine}";

                if (terminal != null)
                {
                    terminal.ForegroundColor = LogLevelColorMap[logLevel];
                    terminal.Out.Write(message);
                    terminal.ResetColor();
                }
                else
                {
                    console.Out.Write(message);
                }
            }
        }

        public bool IsEnabled(LogLevel logLevel) =>
            (int)logLevel >= (int)this.logLevel;

        public IDisposable BeginScope<TState>(TState state) =>
            nullScope;

        private class NullScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
