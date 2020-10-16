using DotNetDevices.Android;
using DotNetDevices.Logging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetDevices.Commands
{
    public class AndroidCommand
    {
        public static Command Create()
        {
            return new Command("android", "Work with Android virtual devices.")
            {
                new Command("list", "List the virtual devices.")
                {
                    new Option<string?>(new[] { "--sdk" }, "The path to the Android SDK directory."),
                    CommandLine.CreateVerbosity(),
                }.WithHandler(CommandHandler.Create(typeof(AndroidCommand).GetMethod(nameof(HandleListAsync))!)),
                new Command("create", "Create a new virtual device.")
                {
                    new Option<string?>(new[] { "--sdk" }, "The path to the Android SDK directory."),
                    new Option(new[] { "--replace" }, "Replace any existing virtual devices with the same name."),
                    CommandLine.CreateVerbosity(),
                    new Argument<string?>("NAME", "The name of the new virtual device."),
                    new Argument<string?>("PACKAGE", "The package to use for the new virtual device."),
                }.WithHandler(CommandHandler.Create(typeof(AndroidCommand).GetMethod(nameof(HandleCreateAsync))!)),
                new Command("delete", "Delete an existing virtual device.")
                {
                    new Option<string?>(new[] { "--sdk" }, "The path to the Android SDK directory."),
                    CommandLine.CreateVerbosity(),
                    new Argument<string?>("NAME", "The name of the new virtual device."),
                }.WithHandler(CommandHandler.Create(typeof(AndroidCommand).GetMethod(nameof(HandleDeleteAsync))!)),
                new Command("boot", "Boot a particular virtual device.")
                {
                    new Option<string?>(new[] { "--sdk" }, "The path to the Android SDK directory."),
                    CommandLine.CreateVerbosity(),
                    new Argument<string?>("NAME", "The name of the virtual device to boot."),
                }.WithHandler(CommandHandler.Create(typeof(AndroidCommand).GetMethod(nameof(HandleBootAsync))!)),
                new Command("install", "Download and install packages using the SDK Manager.")
                {
                    new Option<string?>(new[] { "--sdk" }, "The path to the Android SDK directory."),
                    CommandLine.CreateVerbosity(),
                    new Argument<string?>("PACKAGE", "The package to install."),
                }.WithHandler(CommandHandler.Create(typeof(AndroidCommand).GetMethod(nameof(HandleInstallAsync))!)),
            };
        }

        public static async Task HandleListAsync(
            string? sdk = null,
            string? verbosity = null,
            IConsole console = null!,
            CancellationToken cancellationToken = default)
        {
            var logger = console.CreateLogger(verbosity);
            var avdmanager = new AVDManager(sdk, logger);

            var devices = await avdmanager.GetVirtualDevicesAsync(cancellationToken);

            var filtered = (IEnumerable<VirtualDevice>)devices;

            //term = term?.ToLowerInvariant()?.Trim();

            //var filtered = (IEnumerable<Simulator>)simulators;
            //if (!string.IsNullOrWhiteSpace(term))
            //{
            //    if (Guid.TryParse(term, out var guid))
            //        filtered = filtered.Where(s => s.Udid.ToLowerInvariant() == guid.ToString("d"));
            //    else if (Version.TryParse(term, out var versionFull))
            //        filtered = filtered.Where(s => s.Version == versionFull);
            //    else if (int.TryParse(term, out var versionMjor))
            //        filtered = filtered.Where(s => s.Version.Major == versionMjor);
            //    else if (Enum.TryParse<SimulatorRuntime>(term, true, out var r))
            //        filtered = filtered.Where(s => s.Runtime == r);
            //    else if (Enum.TryParse<SimulatorState>(term, true, out var state))
            //        filtered = filtered.Where(s => s.State == state);
            //    else if (Enum.TryParse<SimulatorAvailability>(term, true, out var availability))
            //        filtered = filtered.Where(s => s.Availability == availability);
            //    else if (Enum.TryParse<SimulatorType>(term, true, out var type))
            //        filtered = filtered.Where(s => s.Type == type);
            //    else
            //        filtered = filtered.Where(s => s.Name.ToLowerInvariant().Contains(term));
            //}
            //if (booted)
            //    filtered = filtered.Where(s => s.State == SimulatorState.Booted);
            //if (available)
            //    filtered = filtered.Where(s => s.Availability == SimulatorAvailability.Available);
            //if (runtime != null)
            //    filtered = filtered.Where(s => s.Runtime == runtime);
            //if (version != null)
            //{
            //    if (Version.TryParse(version, out var versionFull))
            //        filtered = filtered.Where(s => s.Version == versionFull);
            //    else if (int.TryParse(version, out var versionMjor))
            //        filtered = filtered.Where(s => s.Version.Major == versionMjor);
            //}

            var all = filtered.ToList();

            logger.LogInformation($"Found {all.Count} virtual device[s].");

            var table = new TableView<VirtualDevice>();
            table.AddColumn(s => s.Id, "Id");
            table.AddColumn(s => s.Name, "Name");
            table.AddColumn(s => s.Type, "Type");
            table.AddColumn(s => s.Version, "Version");
            table.AddColumn(s => s.ApiLevel, "API Level");
            //table.AddColumn(s => s.State, "State");
            table.Items = all;

            console.Append(new StackLayoutView { table });
        }

        public static async Task HandleCreateAsync(
            string name,
            string package,
            bool replace = false,
            string? sdk = null,
            string? verbosity = null,
            IConsole console = null!,
            CancellationToken cancellationToken = default)
        {
            var logger = console.CreateLogger(verbosity);

            var avdmanager = new AVDManager(sdk, logger);

            if (!replace)
            {
                var devices = await avdmanager.GetVirtualDeviceNamesAsync(cancellationToken);
                if (devices.Any(d => d.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    logger.LogInformation($"Virtual device already exists.");
                    return;
                }
            }

            var options = new CreateVirtualDeviceOptions
            {
                Overwrite = replace,
            };

            await avdmanager.CreateVirtualDeviceAsync(name, package, options, cancellationToken);
        }

        public static async Task HandleDeleteAsync(
            string name,
            string? sdk = null,
            string? verbosity = null,
            IConsole console = null!,
            CancellationToken cancellationToken = default)
        {
            var logger = console.CreateLogger(verbosity);

            var avdmanager = new AVDManager(sdk, logger);

            var devices = await avdmanager.GetVirtualDeviceNamesAsync(cancellationToken);
            if (devices.All(d => !d.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogInformation($"Virtual device does not exist.");
                return;
            }

            await avdmanager.DeleteVirtualDeviceAsync(name, cancellationToken);
        }

        public static async Task<int> HandleBootAsync(
            string name,
            string? sdk = null,
            string? verbosity = null,
            IConsole console = null!,
            CancellationToken cancellationToken = default)
        {
            var logger = console.CreateLogger(verbosity);

            var emulator = new EmulatorManager(sdk, logger);

            var avds = await emulator.GetVirtualDevicesAsync(cancellationToken);
            if (avds.All(a => !a.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogError($"No virtual device with name {name} was found.");
                return 1;
            }

            var options = new BootVirtualDeviceOptions
            {
                NoSnapshots = false,
                WipeData = true,
            };
            var port = await emulator.BootVirtualDeviceAsync(name, options, cancellationToken);
            if (port == -1)
                logger.LogInformation($"Virtual device was already booted.");
            else
                logger.LogInformation($"device was booted to port {port}.");

            return 0;
        }

        public static async Task<int> HandleInstallAsync(
            string package,
            string? sdk = null,
            string? verbosity = null,
            IConsole console = null!,
            CancellationToken cancellationToken = default)
        {
            var logger = console.CreateLogger(verbosity);

            var sdkmanager = new SDKManager(sdk, logger);

            await sdkmanager.InstallAsync(package, cancellationToken);

            return 0;
        }
    }
}
