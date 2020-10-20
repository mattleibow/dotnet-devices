using DotNetDevices.Processes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetDevices.Android
{
    public class Adb
    {
        private readonly ProcessRunner processRunner;
        private readonly ILogger? logger;
        private readonly string adb;

        public Adb(string? sdkRoot = null, ILogger? logger = null)
        {
            this.logger = logger;
            adb = AndroidSDK.FindPath(sdkRoot, "platform-tools/adb", logger)
                ?? throw new ArgumentException($"Unable to locate adb. Make sure that ANDROID_HOME or ANDROID_SDK_ROOT is set.");

            processRunner = new ProcessRunner(logger);
        }

        public async Task<ConnectedDevice?> GetVirtualDeviceWithIdAsync(string avdId, CancellationToken cancellationToken = default)
        {
            var devices = await GetDevicesAsync(cancellationToken).ConfigureAwait(false);
            foreach (var device in devices)
            {
                var deviceAvdId = await GetVirtualDeviceIdAsync(device.Serial, cancellationToken).ConfigureAwait(false);
                if (deviceAvdId?.Equals(avdId, StringComparison.OrdinalIgnoreCase) == true)
                    return device;
            }

            return null;
        }

        public async IAsyncEnumerable<ConnectedDevice> GetVirtualDevicesWithIdAsync(string avdId, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (avdId == null)
                throw new ArgumentNullException(nameof(avdId));

            var devices = await GetDevicesAsync(cancellationToken).ConfigureAwait(false);
            foreach (var device in devices)
            {
                var deviceAvdId = await GetVirtualDeviceIdAsync(device.Serial, cancellationToken).ConfigureAwait(false);
                if (deviceAvdId?.Equals(avdId, StringComparison.OrdinalIgnoreCase) == true)
                    yield return device;
            }
        }

        public async Task<IEnumerable<ConnectedDevice>> GetDevicesAsync(CancellationToken cancellationToken = default)
        {
            logger?.LogInformation("Searching for conected devices...");

            return await GetDevicesNoLoggingAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<ConnectedDevice?> GetDeviceAsync(string serial, CancellationToken cancellationToken = default)
        {
            if (serial == null)
                throw new ArgumentNullException(nameof(serial));

            logger?.LogInformation($"Searching for conected device '{serial}'...");

            var devices = await GetDevicesNoLoggingAsync(cancellationToken).ConfigureAwait(false);

            return devices.FirstOrDefault(d => d.Serial.Equals(serial, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<string?> GetVirtualDeviceIdAsync(string serial, CancellationToken cancellationToken = default)
        {
            if (serial == null)
                throw new ArgumentNullException(nameof(serial));

            logger?.LogInformation($"Reading virtual device ID for '{serial}'...");

            await EnsureDeviceVisibleAsync(serial, cancellationToken).ConfigureAwait(false);

            var args = $"-s \"{serial}\" emu avd name";
            var result = await processRunner.RunAsync(adb, args, null, cancellationToken).ConfigureAwait(false);

            var nonEmptyLines = result.GetOutput()
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();
            if (nonEmptyLines.Length == 0)
                return null;

            if (nonEmptyLines.Length < 2 || !nonEmptyLines[1].Trim().Equals("OK", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Unable to read virtual device ID.");

            return nonEmptyLines[0].Trim();
        }

        public async Task<ProcessResult> LogcatAsync(string serial, LogcatOptions? options = null, CancellationToken cancellationToken = default)
        {
            if (serial == null)
                throw new ArgumentNullException(nameof(serial));

            logger?.LogInformation($"Starting logcat for '{serial}'...");

            await EnsureDeviceVisibleAsync(serial, cancellationToken).ConfigureAwait(false);

            try
            {
                var dump = options?.DumpOnly == true
                    ? "-d"
                    : string.Empty;

                var args = $"-s \"{serial}\" logcat {dump}";
                return await processRunner.RunAsync(adb, args, Wrap(options?.HandleOutput), cancellationToken).ConfigureAwait(false);
            }
            catch (ProcessResultException ex) when (ex.InnerException is OperationCanceledException && ex.ProcessResult != null)
            {
                return ex.ProcessResult;
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

        public async Task ClearLogcatAsync(string serial, CancellationToken cancellationToken = default)
        {
            if (serial == null)
                throw new ArgumentNullException(nameof(serial));

            logger?.LogInformation($"Clearing logcat for '{serial}'...");

            await EnsureDeviceVisibleAsync(serial, cancellationToken).ConfigureAwait(false);

            var args = $"-s \"{serial}\" logcat --clear";
            await processRunner.RunAsync(adb, args, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task LaunchActivityAsync(string serial, string activity, CancellationToken cancellationToken = default)
        {
            if (serial == null)
                throw new ArgumentNullException(nameof(serial));
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));

            logger?.LogInformation($"Launching activity '{activity}' on device '{serial}'...");

            await EnsureDeviceVisibleAsync(serial, cancellationToken).ConfigureAwait(false);

            var args = $"-s \"{serial}\" shell am start -n \"{activity}\"";
            await processRunner.RunAsync(adb, args, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task PullFileAsync(string serial, string packageName, string sourceFileName, string destFileName, bool overwrite, CancellationToken cancellationToken = default)
        {
            if (serial == null)
                throw new ArgumentNullException(nameof(serial));
            if (packageName == null)
                throw new ArgumentNullException(nameof(packageName));
            if (sourceFileName == null)
                throw new ArgumentNullException(nameof(sourceFileName));
            if (destFileName == null)
                throw new ArgumentNullException(nameof(destFileName));

            logger?.LogInformation($"Pulling file '{sourceFileName}' on device '{serial}' to '{destFileName}'...");

            if (File.Exists(destFileName))
            {
                if (!overwrite)
                    throw new IOException($"File {destFileName} already exists.");
                File.Delete(destFileName);
            }

            var guid = Guid.NewGuid().ToString();

            var command = $"cp \"{sourceFileName}\" \"/sdcard/Download/{guid}\"";
            await RunCommandAsAppAsync(serial, packageName, command, cancellationToken).ConfigureAwait(false);

            var args = $"-s \"{serial}\" pull \"/sdcard/Download/{guid}\" \"{destFileName}\"";
            await processRunner.RunAsync(adb, args, null, cancellationToken).ConfigureAwait(false);

            command = $"rm \"/sdcard/Download/{guid}\"";
            await RunCommandAsAppAsync(serial, packageName, command, cancellationToken).ConfigureAwait(false);
        }

        public async Task InstallAppAsync(string serial, string appPath, InstallAppOptions? options = default, CancellationToken cancellationToken = default)
        {
            if (serial == null)
                throw new ArgumentNullException(nameof(serial));
            if (appPath == null)
                throw new ArgumentNullException(nameof(appPath));
            if (!File.Exists(appPath))
                throw new FileNotFoundException($"Unable to find the app '{appPath}'.", appPath);

            logger?.LogInformation($"Installing '{appPath}' on virtual device '{serial}'...");

            if (options?.SkipSharedRuntimeValidation != true && HasSharedRuntime(appPath))
                throw new Exception("Installing apps that rely on the Mono Shared Runtime is not supported. Change the project configuration or use a Release build.");

            await EnsureDeviceVisibleAsync(serial, cancellationToken).ConfigureAwait(false);

            var args = $"-s \"{serial}\" install \"{appPath}\"";
            await processRunner.RunAsync(adb, args, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task UninstallAppAsync(string serial, string packageName, CancellationToken cancellationToken)
        {
            if (serial == null)
                throw new ArgumentNullException(nameof(serial));
            if (packageName == null)
                throw new ArgumentNullException(nameof(packageName));

            logger?.LogInformation($"Uninstalling '{packageName}' on virtual device '{serial}'...");

            await EnsureDeviceVisibleAsync(serial, cancellationToken).ConfigureAwait(false);

            var args = $"-s \"{serial}\" uninstall \"{packageName}\"";
            await processRunner.RunAsync(adb, args, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task ShutdownVirtualDeviceAsync(string serial, CancellationToken cancellationToken = default)
        {
            logger?.LogInformation($"Shutting down virtual device with serial '{serial}'...");

            await EnsureDeviceVisibleAsync(serial, cancellationToken).ConfigureAwait(false);

            var args = $"-s \"{serial}\" emu kill";
            await processRunner.RunAsync(adb, args, null, cancellationToken).ConfigureAwait(false);

            await EnsureShutdownAsync(serial, cancellationToken);
        }

        public async Task<string> GetDataDirectoryAsync(string serial, string packageName, CancellationToken cancellationToken = default)
        {
            if (serial == null)
                throw new ArgumentNullException(nameof(serial));
            if (packageName == null)
                throw new ArgumentNullException(nameof(packageName));

            logger?.LogInformation($"Retrieving data path for app '{packageName}' on device '{serial}'...");

            var command = $"pwd";
            var result = await RunCommandAsAppAsync(serial, packageName, command, cancellationToken).ConfigureAwait(false);
            var packageDataRoot = result.Output.Trim();

            return $"{packageDataRoot}/files";
        }

        public async Task<bool> PathExistsAsync(string serial, string packageName, string path, CancellationToken cancellationToken = default)
        {
            if (serial == null)
                throw new ArgumentNullException(nameof(serial));
            if (packageName == null)
                throw new ArgumentNullException(nameof(packageName));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            logger?.LogInformation($"Check for '{path}' for app '{packageName}' on device '{serial}'...");

            try
            {
                var command = $"ls \"{path}\"";
                var result = await RunCommandAsAppAsync(serial, packageName, command, cancellationToken).ConfigureAwait(false);

                return true;
            }
            catch (ProcessResultException ex) when (ex.ProcessResult.OutputCount == 1 && ex.ProcessResult.Output.Contains("No such file or directory"))
            {
                return false;
            }
        }

        public async Task<ProcessResult> RunCommandAsAppAsync(string serial, string packageName, string command, CancellationToken cancellationToken = default)
        {
            if (serial == null)
                throw new ArgumentNullException(nameof(serial));
            if (packageName == null)
                throw new ArgumentNullException(nameof(packageName));
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            logger?.LogInformation($"Running command '{command}' as app '{packageName}' on device '{serial}'...");

            command = $"run-as \"{packageName}\" {command}";
            return await RunCommandAsync(serial, command, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ProcessResult> RunCommandAsync(string serial, string command, CancellationToken cancellationToken = default)
        {
            if (serial == null)
                throw new ArgumentNullException(nameof(serial));
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            logger?.LogInformation($"Running command '{command}' on device '{serial}'...");

            await EnsureDeviceVisibleAsync(serial, cancellationToken).ConfigureAwait(false);

            var args = $"-s \"{serial}\" shell {command}";
            return await processRunner.RunAsync(adb, args, null, cancellationToken).ConfigureAwait(false);
        }

        private bool HasSharedRuntime(string appPath)
        {
            using var archive = ZipFile.OpenRead(appPath);

            var entries = archive.Entries;

            var hasMonodroid = entries.Any(x => x.Name.EndsWith("libmonodroid.so"));
            var hasRuntime = entries.Any(x => x.Name.EndsWith("mscorlib.dll"));
            var hasEnterpriseBundle = entries.Any(x => x.Name.EndsWith("libmonodroid_bundle_app.so"));

            return hasMonodroid && !hasRuntime && !hasEnterpriseBundle;
        }

        private async Task EnsureShutdownAsync(string serial, CancellationToken cancellationToken)
        {
            while (await IsDeviceVisibleAsync(serial, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task EnsureDeviceVisibleAsync(string serial, CancellationToken cancellationToken)
        {
            var foundDevice = await IsDeviceVisibleAsync(serial, cancellationToken).ConfigureAwait(false);
            if (!foundDevice)
                throw new Exception($"Unable to find virtual device '{serial}'.");
        }

        private async Task<bool> IsDeviceVisibleAsync(string serial, CancellationToken cancellationToken)
        {
            var devices = await GetDevicesNoLoggingAsync(cancellationToken).ConfigureAwait(false);
            return devices.Any(d => d.Serial.Equals(serial, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<IEnumerable<ConnectedDevice>> GetDevicesNoLoggingAsync(CancellationToken cancellationToken)
        {
            logger?.LogDebug("Searching for conected devices...");

            var args = $"devices";

            var result = await processRunner.RunAsync(adb, args, null, cancellationToken).ConfigureAwait(false);

            var devices = new List<ConnectedDevice>();

            foreach (var line in result.GetOutput())
            {
                if (!line.Contains('\t'))
                    continue;

                var parts = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                    continue;

                var serial = parts[0].Trim();
                var state = parts[1].Trim().ToLowerInvariant() switch
                {
                    "device" => ConnectedDeviceState.Connected,
                    "offline" => ConnectedDeviceState.Disconnected,
                    "no device" => ConnectedDeviceState.Unknown,
                    _ => ConnectedDeviceState.Unknown
                };
                devices.Add(new ConnectedDevice(serial, state));
            }

            return devices;
        }
    }
}
