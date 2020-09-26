using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DotNetDevices.Logging;
using Microsoft.Extensions.Logging;

namespace DotNetDevices.Commands
{
    public class TestCommand
    {
        public static Command Create()
        {
            return new Command("test", "Run the unit test apps.")
            {
                new Option<string?>(new[] { "--device-results" }, description: "The path on the device where the test results are saved.",
                    getDefaultValue: () => "TestResults.trx"),
                new Option<string?>(new[] { "--output-results" }, description: "The path on the host where the test results are to be saved.",
                    getDefaultValue: () => "TestResults.trx"),
                new Option<string?>(new[] { "--runtime" }, "The runtime to use when looking for a device."),
                new Option<string?>(new[] { "--version" }, description: "The runtime version to use when looking for a device. This could be in either <major> or <major>.<minor> version formats.",
                    parseArgument: CommandLine.ParseVersion),
                new Option(new[] { "--latest" }, "Whether or not to use the latest version of the filtered devices."),
                new Option<string?>(new[] { "--device-type" }, "The device type to use as part of the filter."),
                new Option<string?>(new[] { "--device-name" }, "The device name/identifier to use as part of the filter."),
                new Option(new[] { "--reset" }, "Whether or not to reset the device before the tests."),
                new Option(new[] { "--shutdown" }, "Whether or not to shutdown the device after the tests."),
                CommandLine.CreateVerbosity(),
                new Argument<string?>("APP", "The path to the app (.app or .apk) to install and test.")
                    { Arity = ArgumentArity.ZeroOrOne },
            }.WithHandler(CommandHandler.Create(typeof(TestCommand).GetMethod(nameof(HandleTestAsync))!));
        }

        public static async Task<int> HandleTestAsync(
            string? app = null,
            string? deviceResults = null,
            string? outputResults = null,
            string? runtime = null,
            string? version = null,
            bool latest = false,
            string? deviceType = null,
            string? deviceName = null,
            bool reset = false,
            bool shutdown = false,
            string? verbosity = null,
            IConsole console = null!,
            CancellationToken cancellationToken = default)
        {
            var logger = console.CreateLogger(verbosity);

            if (string.IsNullOrEmpty(app))
            {
                // TODO: look in working directory

                logger.LogError("The path the app (.app or .apk) was not specified.");
                return 1;
            }

            try
            {
                // detect iOS .app files (directories)
                if (Path.GetExtension(app).Equals(".app", StringComparison.OrdinalIgnoreCase))
                {
                    var cmd = new AppleTestCommand(logger);
                    await cmd.RunTestsAsync(app, deviceResults, outputResults, runtime, version, latest, deviceType, deviceName, reset, shutdown, cancellationToken);
                }

                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError($"There was an problem running the tests: {ex.Message}");
                logger.LogDebug(ex.ToString());

                return 1;
            }
        }
    }
}
