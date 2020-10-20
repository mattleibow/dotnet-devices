using DotNetDevices.Processes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetDevices.Apple
{
    public class SimulatorControl
    {
        private const string xcrun = "/usr/bin/xcrun";

        private readonly ProcessRunner processRunner;
        private readonly ILogger? logger;

        private static readonly Regex deviceKeyRegex = new Regex(@"com\.apple\.CoreSimulator\.SimRuntime\.(.+)\-(\d+)\-(\d+)");

        public SimulatorControl(ILogger? logger = null)
        {
            processRunner = new ProcessRunner(logger);
            this.logger = logger;
        }

        public async Task<Version> GetDefaultVersionAsync(SimulatorRuntime runtime, CancellationToken cancellationToken = default)
        {
            logger?.LogInformation("Retrieving the default version...");

            var sdk = runtime switch
            {
                SimulatorRuntime.tvOS => "appletvos",
                SimulatorRuntime.watchOS => "watchos",
                _ => "iphoneos"
            };

            var args = $"--sdk {sdk} --show-sdk-platform-version";

            var result = await processRunner.RunAsync(xcrun, args, null, cancellationToken).ConfigureAwait(false);

            return Version.Parse(result.Output);
        }

        public Task<Simulator?> GetSimulatorAsync(string udid, CancellationToken cancellationToken = default)
        {
            if (udid == null)
                throw new ArgumentNullException(nameof(udid));

            logger?.LogInformation($"Retrieving simulator {udid}...");

            return GetSimulatorNoLoggingAsync(udid, cancellationToken);
        }

        public async Task<Simulators> GetSimulatorsAsync(CancellationToken cancellationToken = default)
        {
            logger?.LogInformation("Retrieving all the simulators...");

            const string args = "simctl list devices --json";

            var jsonResult = await processRunner.RunAsync(xcrun, args, null, cancellationToken).ConfigureAwait(false);
            var devicesResult = JsonConvert.DeserializeObject<DevicesResult>(jsonResult.Output);

            if (devicesResult?.Devices == null)
                return new Simulators();

            var sims = ConvertToSimulators(devicesResult);

            return new Simulators(sims);
        }

        public async Task BootSimulatorAsync(string udid, CancellationToken cancellationToken = default)
        {
            if (udid == null)
                throw new ArgumentNullException(nameof(udid));

            logger?.LogInformation($"Booting simulator {udid}...");

            var sim = await GetSimulatorNoLoggingAsync(udid, cancellationToken).ConfigureAwait(false);
            if (sim == null)
                throw new Exception($"Unable to find simulator {udid}.");

            if (sim.State == SimulatorState.Booted)
                return;

            var args = $"simctl boot \"{udid}\"";
            await processRunner.RunAsync(xcrun, args, null, cancellationToken).ConfigureAwait(false);

            await EnsureBootedAsync(udid, cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> GetDataDirectoryAsync(string udid, string appBundleId, CancellationToken cancellationToken = default)
        {
            if (udid == null)
                throw new ArgumentNullException(nameof(udid));
            if (appBundleId == null)
                throw new ArgumentNullException(nameof(appBundleId));

            logger?.LogInformation($"Retrieving data path for app {appBundleId} on simulator {udid}...");

            var sim = await GetSimulatorNoLoggingAsync(udid, cancellationToken).ConfigureAwait(false);
            if (sim == null)
                throw new Exception($"Unable to find simulator {udid}.");
            if (sim.DataPath == null)
                throw new Exception($"Unable to find simulator {udid} data path.");

            var applicationDir = Path.Combine(sim.DataPath, "Containers", "Data", "Application");

            foreach (var appDirectory in Directory.EnumerateDirectories(applicationDir))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var metadataPath = Path.Combine(appDirectory, ".com.apple.mobile_container_manager.metadata.plist");
                if (metadataPath == null || !File.Exists(metadataPath))
                    continue;

                var plist = new PList(metadataPath, logger);
                var bundleId = await plist.GetStringValueAsync("MCMMetadataIdentifier", cancellationToken);
                if (bundleId != appBundleId)
                    continue;

                return appDirectory;
            }

            throw new Exception($"Unable to find app {appBundleId} on simulator {udid}.");
        }

        public async Task<string> GetInstalledAppPathAsync(string udid, string appBundleId, CancellationToken cancellationToken = default)
        {
            if (udid == null)
                throw new ArgumentNullException(nameof(udid));
            if (appBundleId == null)
                throw new ArgumentNullException(nameof(appBundleId));

            logger?.LogInformation($"Retrieving installed path for app {appBundleId} on simulator {udid}...");

            var sim = await GetSimulatorNoLoggingAsync(udid, cancellationToken).ConfigureAwait(false);
            if (sim == null)
                throw new Exception($"Unable to find simulator {udid}.");
            if (sim.DataPath == null)
                throw new Exception($"Unable to find simulator {udid} data path.");

            var applicationDir = Path.Combine(sim.DataPath, "Containers", "Bundle", "Application");

            foreach (var appDirectory in Directory.EnumerateDirectories(applicationDir))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var appPath = Directory.EnumerateDirectories(appDirectory, "*.app").SingleOrDefault();
                if (appPath == null)
                    continue;

                var plist = new PList(Path.Combine(appPath, "Info.plist"), logger);
                var bundleId = await plist.GetBundleIdentifierAsync(cancellationToken);
                if (bundleId != appBundleId)
                    continue;

                return appPath;
            }

            throw new Exception($"Unable to find app {appBundleId} on simulator {udid}.");
        }

        public async Task ShutdownSimulatorAsync(string udid, CancellationToken cancellationToken = default)
        {
            if (udid == null)
                throw new ArgumentNullException(nameof(udid));

            logger?.LogInformation($"Shutting down simulator {udid}...");

            var sim = await GetSimulatorNoLoggingAsync(udid, cancellationToken).ConfigureAwait(false);
            if (sim == null)
                throw new Exception($"Unable to find simulator {udid}.");

            if (sim.State == SimulatorState.Shutdown)
                return;

            var args = $"simctl shutdown \"{udid}\"";
            await processRunner.RunAsync(xcrun, args, null, cancellationToken).ConfigureAwait(false);

            await EnsureShutdownAsync(udid, cancellationToken).ConfigureAwait(false);
        }

        public async Task EraseSimulatorAsync(string udid, CancellationToken cancellationToken = default)
        {
            if (udid == null)
                throw new ArgumentNullException(nameof(udid));

            logger?.LogInformation($"Erasing simulator {udid}...");

            var sim = await GetSimulatorNoLoggingAsync(udid, cancellationToken).ConfigureAwait(false);
            if (sim == null)
                throw new Exception($"Unable to find simulator {udid}.");

            var args = $"simctl erase \"{udid}\"";
            await processRunner.RunAsync(xcrun, args, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task InstallAppAsync(string udid, string appPath, CancellationToken cancellationToken = default)
        {
            if (udid == null)
                throw new ArgumentNullException(nameof(udid));
            if (appPath == null)
                throw new ArgumentNullException(nameof(appPath));
            if (!Directory.Exists(appPath))
                throw new FileNotFoundException($"Unable to find the app {appPath}", appPath);

            logger?.LogInformation($"Installing {appPath} on simulator {udid}...");

            var sim = await GetSimulatorNoLoggingAsync(udid, cancellationToken).ConfigureAwait(false);
            if (sim == null)
                throw new Exception($"Unable to find simulator {udid}.");

            var args = $"simctl install \"{udid}\" \"{appPath}\"";
            await processRunner.RunAsync(xcrun, args, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task<LaunchAppResult> LaunchAppAsync(string udid, string appBundleId, LaunchAppOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (udid == null)
                throw new ArgumentNullException(nameof(udid));
            if (appBundleId == null)
                throw new ArgumentNullException(nameof(appBundleId));

            logger?.LogInformation($"Launching {appBundleId} on simulator {udid}...");

            var sim = await GetSimulatorNoLoggingAsync(udid, cancellationToken).ConfigureAwait(false);
            if (sim == null)
                throw new Exception($"Unable to find simulator {udid}.");

            var console = options?.CaptureOutput == true
                ? "--console"
                : string.Empty;

            var args = $"simctl launch {console} \"{udid}\" \"{appBundleId}\"";

            try
            {
                var result = await processRunner.RunAsync(xcrun, args, Wrap(options?.HandleOutput), cancellationToken).ConfigureAwait(false);
                return new LaunchAppResult(result);
            }
            catch (ProcessResultException ex) when (ex.InnerException is OperationCanceledException && ex.ProcessResult != null)
            {
                await TerminateAppAsync(udid, appBundleId, cancellationToken);
                return new LaunchAppResult(ex.ProcessResult);
            }

            static Func<ProcessOutput, bool>? Wrap(Action<ProcessOutput>? handle)
            {
                if (handle == null)
                    return null;

                return o =>
                {
                    handle(o);
                    return true;
                };
            }
        }

        public async Task TerminateAppAsync(string udid, string appBundleId, CancellationToken cancellationToken = default)
        {
            if (udid == null)
                throw new ArgumentNullException(nameof(udid));
            if (appBundleId == null)
                throw new ArgumentNullException(nameof(appBundleId));

            logger?.LogInformation($"Terminating {appBundleId} on simulator {udid}...");

            var sim = await GetSimulatorNoLoggingAsync(udid, cancellationToken).ConfigureAwait(false);
            if (sim == null)
                throw new Exception($"Unable to find simulator {udid}.");

            if (sim.State != SimulatorState.Booted)
                return;

            var args = $"simctl terminate \"{udid}\" \"{appBundleId}\"";
            await processRunner.RunAsync(xcrun, args, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task UninstallAppAsync(string udid, string appBundleId, CancellationToken cancellationToken = default)
        {
            if (udid == null)
                throw new ArgumentNullException(nameof(udid));
            if (appBundleId == null)
                throw new ArgumentNullException(nameof(appBundleId));

            logger?.LogInformation($"Uninstalling {appBundleId} on simulator {udid}...");

            var sim = await GetSimulatorNoLoggingAsync(udid, cancellationToken).ConfigureAwait(false);
            if (sim == null)
                throw new Exception($"Unable to find simulator {udid}.");

            var args = $"simctl uninstall \"{udid}\" \"{appBundleId}\"";
            await processRunner.RunAsync(xcrun, args, null, cancellationToken).ConfigureAwait(false);
        }

        private async Task EnsureBootedAsync(string udid, CancellationToken cancellationToken)
        {
            while ((await GetSimulatorNoLoggingAsync(udid, cancellationToken).ConfigureAwait(false))!.State != SimulatorState.Booted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task EnsureShutdownAsync(string udid, CancellationToken cancellationToken)
        {
            while ((await GetSimulatorNoLoggingAsync(udid, cancellationToken).ConfigureAwait(false))!.State != SimulatorState.Shutdown)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<Simulator?> GetSimulatorNoLoggingAsync(string udid, CancellationToken cancellationToken)
        {
            logger?.LogDebug($"Retrieving simulator {udid}...");

            var args = $"simctl list devices \"{udid}\" --json";

            var jsonResult = await processRunner.RunAsync(xcrun, args, null, cancellationToken).ConfigureAwait(false);
            var devicesResult = JsonConvert.DeserializeObject<DevicesResult>(jsonResult.Output);

            if (devicesResult?.Devices == null)
                return null;

            if (devicesResult.Devices.Count > 1)
                logger?.LogDebug($"Found more than 1 simulator for {udid}. Returning the first one.");

            var sims = ConvertToSimulators(devicesResult);

            return sims.FirstOrDefault();
        }

        private List<Simulator> ConvertToSimulators(DevicesResult devicesResult)
        {
            var sims = new List<Simulator>();

            if (devicesResult?.Devices?.Count > 0)
            {
                foreach (var deviceGroup in devicesResult.Devices)
                {
                    var match = deviceKeyRegex.Match(deviceGroup.Key);
                    if (!match.Success)
                    {
                        logger?.LogDebug($"Unknown simulator type: {deviceGroup.Key}.");
                        continue;
                    }

                    string deviceKey = match.Groups[1].Value;

                    foreach (var device in deviceGroup.Value)
                    {
                        var runtime = deviceKey switch
                        {
                            "watchOS" => SimulatorRuntime.watchOS,
                            "tvOS" => SimulatorRuntime.tvOS,
                            _ => SimulatorRuntime.iOS
                        };

                        var version = new Version($"{match.Groups[2].Value}.{match.Groups[3].Value}");

                        var state = device.State switch
                        {
                            "Booted" => SimulatorState.Booted,
                            "Shutdown" => SimulatorState.Shutdown,
                            _ => SimulatorState.Unknown,
                        };

                        var isAvailable =
                            device.Availability == "(available)" ||
                            device.IsAvailable == "YES" ||
                            device.IsAvailable == "true";
                        var availability = isAvailable
                            ? SimulatorAvailability.Available
                            : SimulatorAvailability.Unavailable;

                        sims.Add(new Simulator(
                            device.Udid!,
                            device.Name!,
                            runtime,
                            version,
                            state,
                            availability,
                            device.DataPath,
                            device.LogPath));
                    }
                }
            }

            return sims;
        }

        private class Device
        {
            [JsonProperty("state")]
            public string? State { get; set; }

            [JsonProperty("availability")]
            public string? Availability { get; set; }

            [JsonProperty("isAvailable")]
            public string? IsAvailable { get; set; }

            [JsonProperty("availabilityError")]
            public string? AvailabilityError { get; set; }

            [JsonProperty("name")]
            public string? Name { get; set; }

            [JsonProperty("udid")]
            public string? Udid { get; set; }

            [JsonProperty("dataPath")]
            public string? DataPath { get; set; }

            [JsonProperty("logPath")]
            public string? LogPath { get; set; }
        }

        private class DevicesResult
        {
            [JsonProperty("devices")]
            public Dictionary<string, List<Device>>? Devices { get; set; }
        }
    }
}
