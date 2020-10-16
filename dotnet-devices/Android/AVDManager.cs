using DotNetDevices.Processes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetDevices.Android
{
    public class AVDManager
    {
        private static Regex virtualDevicePathRegex = new Regex(@"\s*Path\:\s*(.+)");

        private readonly ProcessRunner processRunner;
        private readonly ILogger? logger;
        private readonly string avdmanager;

        public AVDManager(string? sdkRoot = null, ILogger? logger = null)
        {
            processRunner = new ProcessRunner(logger);
            this.logger = logger;

            avdmanager = AndroidSDK.FindPath(sdkRoot, Path.Combine("tools", "bin", "avdmanager"), logger)
                ?? throw new ArgumentException($"Unable to locate the AVD Manager. Make sure that ANDROID_HOME or ANDROID_SDK_ROOT is set.");
        }

        public async Task<IEnumerable<Device>> GetDevicesAsync(CancellationToken cancellationToken = default)
        {
            logger?.LogInformation("Retrieving all the devices...");

            var args = $"list device -c";

            var result = await processRunner.RunAsync(avdmanager, args, null, cancellationToken).ConfigureAwait(false);

            var devices = new List<Device>(result.OutputCount);
            foreach (var output in GetListResults(result))
            {
                devices.Add(new Device(output));
            }
            return devices;
        }

        public async Task<IEnumerable<DeviceTarget>> GetTargetsAsync(CancellationToken cancellationToken = default)
        {
            logger?.LogInformation("Retrieving all the device targets...");

            var args = $"list target -c";

            var result = await processRunner.RunAsync(avdmanager, args, null, cancellationToken).ConfigureAwait(false);

            var targets = new List<DeviceTarget>(result.OutputCount);
            foreach (var output in GetListResults(result))
            {
                targets.Add(new DeviceTarget(output));
            }
            return targets;
        }

        public async Task<IEnumerable<VirtualDevice>> GetVirtualDevicesAsync(CancellationToken cancellationToken = default)
        {
            logger?.LogInformation("Retrieving all the virtual devices...");

            var args = $"list avd";

            var result = await processRunner.RunAsync(avdmanager, args, null, cancellationToken).ConfigureAwait(false);

            var avds = new List<VirtualDevice>();

            foreach (var output in GetListResults(result))
            {
                var pathMatch = virtualDevicePathRegex.Match(output);
                if (pathMatch.Success)
                {
                    var path = pathMatch.Groups[1].Value;
                    var configIniPath = Path.Combine(path, "config.ini");
                    if (Directory.Exists(path) && File.Exists(configIniPath))
                    {
                        var config = new VirtualDeviceConfig(configIniPath, logger);
                        var avd = await config.CreateVirtualDeviceAsync(cancellationToken).ConfigureAwait(false);

                        avds.Add(avd);
                    }
                }
            }

            return avds;
        }

        public async Task DeleteVirtualDeviceAsync(string name, CancellationToken cancellationToken = default)
        {
            logger?.LogInformation($"Deleting virtual device '{name}'...");

            var args = $"delete avd --name \"{name}\"";

            await processRunner.RunAsync(avdmanager, args, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task CreateVirtualDeviceAsync(string name, string package, CreateVirtualDeviceOptions? options = null, CancellationToken cancellationToken = default)
        {
            logger?.LogInformation($"Creating virtual device '{name}'...");

            var args = $"create avd --name \"{name}\" --package \"{package}\"";
            if (options?.Overwrite == true)
                args += " --force";

            await processRunner.RunWithInputAsync("no", avdmanager, args, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task RenameVirtualDeviceAsync(string name, string newName, CancellationToken cancellationToken = default)
        {
            logger?.LogInformation($"Renaming virtual device '{name}'...");

            var args = $"move avd --name \"{name}\" --rename \"{newName}\"";

            await processRunner.RunAsync(avdmanager, args, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task MoveVirtualDeviceAsync(string name, string newPath, CancellationToken cancellationToken = default)
        {
            logger?.LogInformation($"Moving virtual device '{name}'...");

            var args = $"move avd --name \"{name}\" --path \"{newPath}\"";

            await processRunner.RunAsync(avdmanager, args, null, cancellationToken).ConfigureAwait(false);
        }

        private static IEnumerable<string> GetListResults(ProcessResult result)
        {
            foreach (var output in result.GetOutput())
            {
                if (string.IsNullOrWhiteSpace(output) || output.StartsWith("[") || output.StartsWith("Loading "))
                    continue;

                var o = output;

                // this is needed because sometimes the lines are merged...
                const string p = "package.xml";
                if (output.StartsWith("Parsing "))
                {
                    if (output.EndsWith(p))
                        continue;

                    o = output.Substring(output.LastIndexOf(p) + p.Length);
                    if (string.IsNullOrWhiteSpace(o))
                        continue;
                }

                yield return o;
            }
        }
    }
}
