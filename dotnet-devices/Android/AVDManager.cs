using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetDevices.Processes;
using Microsoft.Extensions.Logging;

namespace DotNetDevices.Android
{
    public class AVDManager
    {
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

            return await GetVirtualDeviceNoLogging(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteVirtualDeviceAsync(string name, CancellationToken cancellationToken = default)
        {
            logger?.LogInformation($"Deleting virtual device '{name}'...");

            var avds = await GetVirtualDeviceNoLogging(cancellationToken);
            if (!avds.Any(x => x.Name.ToLowerInvariant() == name.ToLowerInvariant()))
                throw new Exception($"Unable to find virtual device '{name}'.");

            var args = $"delete avd --name \"{name}\"";

            await processRunner.RunAsync(avdmanager, args, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task CreateVirtualDeviceAsync(string name, string package, VirtualDeviceCreateOptions? options = null, CancellationToken cancellationToken = default)
        {
            logger?.LogInformation($"Creating virtual device '{name}'...");

            if (options?.Overwrite != true)
            {
                var avds = await GetVirtualDeviceNoLogging(cancellationToken);
                if (avds.Any(x => x.Name.ToLowerInvariant() == name.ToLowerInvariant()))
                    throw new Exception($"Virtual device '{name}' already exists.");
            }

            var args = $"create avd --name \"{name}\" --package \"{package}\"";
            if (options?.Overwrite == true)
                args += " --force";

            await processRunner.RunWithInputAsync("no", avdmanager, args, null, cancellationToken).ConfigureAwait(false);
        }

        private IEnumerable<string> GetListResults(ProcessResult result)
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

        private async Task<IEnumerable<VirtualDevice>> GetVirtualDeviceNoLogging(CancellationToken cancellationToken = default)
        {
            logger?.LogDebug("Retrieving all the virtual devices...");

            var args = $"list avd -c";

            var result = await processRunner.RunAsync(avdmanager, args, null, cancellationToken).ConfigureAwait(false);

            var avd = new List<VirtualDevice>(result.OutputCount);
            foreach (var output in GetListResults(result))
            {
                avd.Add(new VirtualDevice(output));
            }
            return avd;
        }
    }

    public class VirtualDeviceCreateOptions
    {
        public string? Device { get; set; }

        public bool Overwrite { get; set; }

        public string? Path { get; set; }

        public string? SharedSdCardPath { get; set; }

        public string? NewSdCardSize { get; set; }
    }
}
