using DotNetDevices.Android;
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
    internal class AndroidTestCommand
    {
        private readonly string? sdkRoot;
        private readonly ILogger logger;
        private readonly AVDManager avdmanager;
        private readonly EmulatorManager emulator;
        private readonly Aapt aapt;
        private readonly Adb adb;

        public AndroidTestCommand(string? sdkRoot, ILogger logger)
        {
            this.sdkRoot = sdkRoot;
            this.logger = logger;

            avdmanager = new AVDManager(sdkRoot, logger);
            emulator = new EmulatorManager(sdkRoot, logger);
            aapt = new Aapt(sdkRoot, logger);
            adb = new Adb(sdkRoot, logger);
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
            var androidManifest = await aapt.GetAndroidManifestAsync(app, cancellationToken);
            var packageName = androidManifest.PackageName;
            if (string.IsNullOrEmpty(packageName))
                throw new Exception("Unable to determine the package name for the app.");
            var activityName = androidManifest.MainLauncherActivity;
            if (string.IsNullOrEmpty(activityName))
                throw new Exception("Unable to determine the main launcher activity name for the app.");

            logger.LogInformation($"Running tests on '{packageName}'...");
            logger.LogInformation($"Detected main launcher activity '{activityName}'.");

            // validate requested OS
            var avdRuntime = ParseDeviceRuntime(runtimeString);
            var avdTypes = ParseDeviceTypes(deviceType, avdRuntime);
            var avdApiLevel = ParseApiLevel(versionString);
            if (avdApiLevel == 0)
                latest = true;

            logger.LogInformation($"Looking for an available {string.Join("|", avdTypes)}{(avdApiLevel == 0 ? "" : $" (API {avdApiLevel})")} virtual device...");
            var available = await GetAvailableDevicesAsync(deviceName, avdTypes, avdApiLevel, latest, cancellationToken);

            // get the first device
            var avd = available.FirstOrDefault();
            logger.LogInformation($"Using virtual device {avd.Name} ({avd.Runtime} {avd.Version}): {avd.Id}");

            string? serial = null;
            try
            {
                if (reset)
                {
                    var bootedDevice = await adb.GetVirtualDeviceWithIdAsync(avd.Id, cancellationToken);
                    if (bootedDevice != null)
                        await adb.ShutdownVirtualDeviceAsync(bootedDevice.Serial);

                    await avdmanager.ResetVirtualDeviceAsync(avd.Id, cancellationToken);
                }

                var bootOptions = new BootVirtualDeviceOptions
                {
                    WipeData = reset,
                };
                var port = await emulator.BootVirtualDeviceAsync(avd.Id, bootOptions, cancellationToken);
                if (port == -1)
                {
                    // device was already booted, so find it
                    var bootedDevice = await adb.GetVirtualDeviceWithIdAsync(avd.Id, cancellationToken);
                    if (bootedDevice == null)
                        throw new Exception($"Virtual device '{avd.Name}' was already booted, but was not able to be found.");

                    serial = bootedDevice.Serial;
                    logger.LogDebug($"Virutal device was already booted, found serial '{serial}'");
                }
                else
                {
                    serial = $"emulator-{port}";
                    logger.LogDebug($"Virtual device was booted to port {port}, assuming serial '{serial}'");
                }

                var installOptions = new InstallAppOptions
                {
                    SkipSharedRuntimeValidation = false,
                };
                await adb.InstallAppAsync(serial, app, installOptions, cancellationToken);

                try
                {
                    await adb.ClearLogcatAsync(serial, cancellationToken);

                    var parser = new TestResultsParser();

                    var logcatTask = adb.LogcatAsync(serial, new LogcatOptions
                    {
                        HandleOutput = output =>
                        {
                            parser.ParseTestOutput(
                                output,
                                line => logger?.LogWarning(line),
                                () => throw new TaskCanceledException());
                        }
                    }, cancellationToken);

                    await adb.LaunchActivityAsync(serial, $"{packageName}/{activityName}", cancellationToken);

                    await logcatTask;

                    if (deviceResults != null)
                    {
                        var dest = outputResults ?? Path.GetFileName(deviceResults);

                        logger.LogInformation($"Copying test results from virtual device to {dest}...");

                        var dataPath = await adb.GetDataDirectoryAsync(serial, packageName, cancellationToken);
                        var results = Path.Combine(dataPath, deviceResults).Replace("\\", "/");
                        if (await adb.PathExistsAsync(serial, packageName, results, cancellationToken))
                            await adb.PullFileAsync(serial, packageName, results, dest, true, cancellationToken);
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
                    await adb.UninstallAppAsync(serial, packageName, cancellationToken);
                }
            }
            finally
            {
                if (shutdown && serial != null)
                    await adb.ShutdownVirtualDeviceAsync(serial, cancellationToken);
            }
        }

        private async Task<List<VirtualDevice>> GetAvailableDevicesAsync(string? deviceName, VirtualDeviceType[] types, int apiLevel, bool useLatest = true, CancellationToken cancellationToken = default)
        {
            // load all virtual devices
            var avds = await avdmanager.GetVirtualDevicesAsync(cancellationToken);

            // use the name directly
            if (!string.IsNullOrEmpty(deviceName))
                return avds.Where(d => d.Id == deviceName || d.Name == deviceName).ToList();

            // find ones that can be used
            var available = avds
                .Where(s => types.Contains(s.Type));
            logger.LogDebug($"Found some available virtual devices:");
            foreach (var avd in available)
            {
                logger.LogDebug($"  {avd.Name} ({avd.Runtime} API {avd.ApiLevel}): {avd.Id}");
            }

            // filter by version info
            string matchingPattern;
            if (useLatest)
            {
                var max = available.Where(d => d.ApiLevel >= apiLevel).Max(d => d.ApiLevel);
                available = available.Where(d => d.ApiLevel == max);
                matchingPattern = apiLevel > 0 ? $"[{apiLevel})" : $"[{max}]";
            }
            else
            {
                available = available.Where(d => d.ApiLevel == apiLevel);
                matchingPattern = $"[{apiLevel}]";
            }

            var matching = available.ToList();
            if (matching.Count == 0)
                throw new Exception($"Unable to find any virtual devices that match version {matchingPattern}.");

            logger.LogDebug($"Found matching virtual devices {matchingPattern}:");
            foreach (var avd in matching)
            {
                logger.LogDebug($"  {avd.Name} ({avd.Runtime} API {avd.ApiLevel}): {avd.Id}");
            }

            return matching;
        }

        private int ParseApiLevel(string? version)
        {
            var osVersion = version?.ToLowerInvariant().Trim();

            if (string.IsNullOrEmpty(osVersion))
                return 0;

            if (!int.TryParse(osVersion, out var numberVersion))
                throw new Exception($"Unable to determine the version for {osVersion}.");

            return numberVersion;
        }

        private static VirtualDeviceRuntime ParseDeviceRuntime(string? runtime)
        {
            var osName = runtime?.ToLowerInvariant()?.Trim();
            var os = osName switch
            {
                null => VirtualDeviceRuntime.Android,
                "" => VirtualDeviceRuntime.Android,
                "android" => VirtualDeviceRuntime.Android,
                "watch" => VirtualDeviceRuntime.AndroidWear,
                "wear" => VirtualDeviceRuntime.AndroidWear,
                "androidwear" => VirtualDeviceRuntime.AndroidWear,
                "wearable" => VirtualDeviceRuntime.AndroidWear,
                "tv" => VirtualDeviceRuntime.AndroidTV,
                "androidtv" => VirtualDeviceRuntime.AndroidTV,
                _ => throw new Exception($"Unable to determine the OS for {runtime}.")
            };
            return os;
        }

        private static VirtualDeviceType[] ParseDeviceTypes(string? deviceType, VirtualDeviceRuntime runtime)
        {
            var fallback = runtime switch
            {
                VirtualDeviceRuntime.Android => new[] { VirtualDeviceType.Phone, VirtualDeviceType.Tablet },
                VirtualDeviceRuntime.AndroidWear => new[] { VirtualDeviceType.Wearable },
                VirtualDeviceRuntime.AndroidTV => new[] { VirtualDeviceType.TV },
                _ => new[] { VirtualDeviceType.Phone | VirtualDeviceType.Tablet },
            };

            var deviceTypeName = deviceType?.ToLowerInvariant()?.Trim();
            var device = deviceTypeName switch
            {
                // phone
                null => fallback,
                "" => fallback,
                "phone" => new[] { VirtualDeviceType.Phone },
                // tablet
                "tab" => new[] { VirtualDeviceType.Tablet },
                "tablet" => new[] { VirtualDeviceType.Tablet },
                // TV
                "tv" => new[] { VirtualDeviceType.TV },
                // Wear
                "watch" => new[] { VirtualDeviceType.Wearable },
                "wear" => new[] { VirtualDeviceType.Wearable },
                "wearable" => new[] { VirtualDeviceType.Wearable },
                //
                _ => throw new Exception($"Unable to determine the virtual device type for {deviceType}.")
            };
            return device;
        }
    }
}
