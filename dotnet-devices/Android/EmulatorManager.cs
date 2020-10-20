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
    public class EmulatorManager
    {
        private static readonly Regex consoleListeningRegex = new Regex(@"emulator: control console listening on port (\d+), ADB on port (\d+)");
        private static readonly Regex adbConnectedRegex = new Regex(@"emulator: onGuestSendCommand: \[(.+)\] Adb connected, start proxing data");
        private static readonly Regex alreadyBootedRegex = new Regex(@"emulator: ERROR: Running multiple emulators with the same AVD is an experimental feature\.");

        private readonly ProcessRunner processRunner;
        private readonly ILogger? logger;
        private readonly string emulator;

        public EmulatorManager(string? sdkRoot = null, ILogger? logger = null)
        {
            this.logger = logger;
            processRunner = new ProcessRunner(logger);
            emulator = AndroidSDK.FindPath(sdkRoot, Path.Combine("emulator", "emulator"), logger)
                ?? throw new ArgumentException($"Unable to locate the Android Emulator. Make sure that ANDROID_HOME or ANDROID_SDK_ROOT is set.");
        }

        public async Task<int> BootVirtualDeviceAsync(string avdId, BootVirtualDeviceOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (avdId == null)
                throw new ArgumentNullException(nameof(avdId));

            logger?.LogInformation($"Booting virtual device '{avdId}'...");

            var args = $"-avd {avdId} -verbose";
            if (options?.NoWindow == true)
                args += " -no-boot-anim -no-window";
            if (options?.NoSnapshots == true)
                args += " -no-snapshot";
            if (options?.WipeData == true)
                args += " -wipe-data";

            var port = -1;
            try
            {
                await processRunner.RunAsync(emulator, args, FindComplete, cancellationToken).ConfigureAwait(false);
            }
            catch (ProcessResultException ex) when (IsAlreadyLaunched(ex))
            {
                // no-op
            }

            return port;

            bool FindComplete(ProcessOutput output)
            {
                if (!output.IsError && output.Data is string o)
                {
                    if (port <= 0)
                    {
                        // first find port
                        var match = consoleListeningRegex.Match(o);
                        if (match.Success)
                            port = int.Parse(match.Groups[1].Value);
                    }
                    else
                    {
                        // then wait for the boot finished
                        var match = adbConnectedRegex.Match(o);
                        if (match.Success)
                            return false;
                    }
                }

                return true;
            }

            static bool IsAlreadyLaunched(ProcessResultException ex)
            {
                foreach (var output in ex.ProcessResult.GetOutput())
                {
                    var match = alreadyBootedRegex.Match(output);
                    if (match.Success)
                        return true;
                }

                return false;
            }
        }

        public async Task<IEnumerable<string>> GetVirtualDevicesAsync(CancellationToken cancellationToken = default)
        {
            logger?.LogInformation("Retrieving all the virtual devices...");

            var args = $"-list-avds";

            var result = await processRunner.RunAsync(emulator, args, null, cancellationToken).ConfigureAwait(false);

            var avd = new List<string>(result.OutputCount);
            foreach (var output in result.GetOutput())
            {
                avd.Add(output.Trim());
            }
            return avd;
        }
    }
}
