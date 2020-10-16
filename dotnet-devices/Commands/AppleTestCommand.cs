using DotNetDevices.Apple;
using DotNetDevices.Testing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetDevices.Commands
{
    public class AppleTestCommand
    {
        private readonly ILogger logger;
        private readonly SimulatorControl simctl;

        public AppleTestCommand(ILogger logger)
        {
            this.logger = logger;

            simctl = new SimulatorControl(logger);
        }

        public async Task RunTestsAsync(
            string app,
            string? deviceResults = null, // "TestResults.trx"
            string? outputResults = null, // "TestResults.trx"
            string? runtimeString = null,
            string? versionString = null,
            bool latest = false,
            string? deviceType = null,
            string? deviceName = null,
            bool reset = false,
            bool shutdown = false,
            CancellationToken cancellationToken = default)
        {
            // validate app / bundle
            var plist = new PList(Path.Combine(app, "Info.plist"), logger);
            var bundleId = await plist.GetBundleIdentifierAsync(cancellationToken);
            if (string.IsNullOrEmpty(bundleId))
                throw new Exception("Unable to determine the bundle identifer for the app.");

            logger.LogInformation($"Running tests on '{bundleId}'...");

            // validate requested OS
            var simulatorType = ParseSimulatorType(deviceType);
            var runtime = ParseSimulatorRuntime(runtimeString);
            var runtimeVersion = await ParseVersionAsync(versionString, runtime, cancellationToken);

            logger.LogInformation($"Looking for an available {simulatorType} ({runtimeVersion}) simulator...");
            var available = await GetAvailableSimulatorsAsync(simulatorType, runtime, runtimeVersion, latest, cancellationToken);

            // first look for a booted device
            var simulator = available.FirstOrDefault(s => s.State == SimulatorState.Booted) ?? available.FirstOrDefault();
            logger.LogInformation($"Using simulator {simulator.Name} ({simulator.Runtime} {simulator.Version}): {simulator.Udid}");

            try
            {
                if (reset)
                    await simctl.EraseSimulatorAsync(simulator.Udid, true, cancellationToken);

                await simctl.InstallAppAsync(simulator.Udid, app, true, cancellationToken);

                try
                {
                    var parser = new TestResultsParser();

                    var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                    var launched = await simctl.LaunchAppAsync(simulator.Udid, bundleId, new LaunchAppOptions
                    {
                        CaptureOutput = true,
                        BootSimulator = true,
                        HandleOutput = output =>
                        {
                            parser.ParseTestOutput(
                                output,
                                line => logger?.LogWarning(line),
                                async () =>
                                {
                                    try
                                    {
                                        // wait a few seconds before terminating
                                        await Task.Delay(1000, cts.Token);

                                        await simctl.TerminateAppAsync(simulator.Udid, bundleId, cts.Token);
                                    }
                                    catch (OperationCanceledException)
                                    {
                                        // we expected this
                                    }
                                });
                        },
                    }, cancellationToken);

                    cts.Cancel();

                    if (deviceResults != null)
                    {
                        var dest = outputResults ?? Path.GetFileName(deviceResults);

                        logger.LogInformation($"Copying test results from simulator to {dest}...");

                        var dataPath = await simctl.GetDataDirectoryAsync(simulator.Udid, bundleId, cancellationToken);
                        var results = Path.Combine(dataPath, "Documents", deviceResults);
                        if (File.Exists(results))
                            File.Copy(results, dest, true);
                        else
                            logger.LogInformation($"No test results found.");
                    }
                    else
                    {
                        logger.LogInformation($"Unable to determine the test results file.");
                    }
                }
                finally
                {
                    await simctl.UninstallAppAsync(simulator.Udid, bundleId, false, cancellationToken);
                }
            }
            finally
            {
                if (shutdown)
                    await simctl.ShutdownSimulatorAsync(simulator.Udid, cancellationToken);
            }
        }

        private async Task<List<Simulator>> GetAvailableSimulatorsAsync(SimulatorType type, SimulatorRuntime runtime, Version version, bool useLatest = true, CancellationToken cancellationToken = default)
        {
            // load all simulators
            var simulators = await simctl.GetSimulatorsAsync(cancellationToken);

            // find ones that can be used
            var available = simulators
                .Where(s => s.Availability == SimulatorAvailability.Available)
                .Where(s => s.Runtime == runtime)
                .Where(s => s.Type == type);
            logger.LogDebug($"Found some available simulators:");
            foreach (var sim in available)
            {
                logger.LogDebug($"  {sim.Name} ({sim.Runtime} {sim.Version}): {sim.Udid}");
            }

            // filter by version info
            string matchingPattern;
            if (useLatest)
            {
                var min = version;
                var max = new Version(min.Major + 1, 0);
                available = available.Where(s => s.Version >= min && s.Version < max);
                matchingPattern = $"[{min}, {max})";
            }
            else
            {
                available = available.Where(s => s.Version == version);
                matchingPattern = $"[{version}]";
            }

            var matching = available.ToList();
            if (matching.Count > 0)
            {
                logger.LogDebug($"Found matching simulators {matchingPattern}:");
                foreach (var sim in matching)
                {
                    logger.LogDebug($"  {sim.Name} ({sim.Runtime} {sim.Version}): {sim.Udid}");
                }
            }
            else
            {
                throw new Exception($"Unable to find any simulators that match version {matchingPattern}.");
            }

            return matching;
        }

        private async Task<Version> ParseVersionAsync(string? version, SimulatorRuntime os, CancellationToken cancellationToken = default)
        {
            var osVersion = version?.ToLowerInvariant().Trim();
            if (!Version.TryParse(osVersion, out var numberVersion))
            {
                if (int.TryParse(osVersion, out var v))
                    numberVersion = new Version(v, 0);
                else if (string.IsNullOrEmpty(osVersion) || osVersion == "default")
                    numberVersion = await simctl.GetDefaultVersionAsync(os, cancellationToken);
                else
                    throw new Exception($"Unable to determine the version for {osVersion}.");
            }

            return numberVersion;
        }

        private static SimulatorRuntime ParseSimulatorRuntime(string? runtime)
        {
            var osName = runtime?.ToLowerInvariant()?.Trim();
            var os = osName switch
            {
                null => SimulatorRuntime.iOS,
                "" => SimulatorRuntime.iOS,
                "ios" => SimulatorRuntime.iOS,
                "watchos" => SimulatorRuntime.watchOS,
                "tvos" => SimulatorRuntime.tvOS,
                _ => throw new Exception($"Unable to determine the OS for {runtime}.")
            };
            return os;
        }

        private static SimulatorType ParseSimulatorType(string? deviceType)
        {
            var deviceTypeName = deviceType?.ToLowerInvariant()?.Trim();
            var device = deviceTypeName switch
            {
                // iPhone
                null => SimulatorType.iPhone,
                "" => SimulatorType.iPhone,
                "iphone" => SimulatorType.iPhone,
                "phone" => SimulatorType.iPhone,
                // iPad
                "ipad" => SimulatorType.iPad,
                "tablet" => SimulatorType.iPad,
                // iPod
                "ipod" => SimulatorType.iPod,
                // Apple TV
                "tv" => SimulatorType.AppleTV,
                "appletv" => SimulatorType.AppleTV,
                // Apple Watch
                "watch" => SimulatorType.AppleWatch,
                "applewatch" => SimulatorType.AppleWatch,
                //
                _ => throw new Exception($"Unable to determine the simulator type for {deviceType}.")
            };
            return device;
        }
    }
}
