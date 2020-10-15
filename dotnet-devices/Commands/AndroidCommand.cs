using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetDevices.Android;
using DotNetDevices.Apple;
using DotNetDevices.Logging;
using Microsoft.Extensions.Logging;

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
                    new Option<string?>(new[] { "--sdk" }, "Whether or not to only include the available simulators."),
                    new Option(new[] { "--available" }, "Whether or not to only include the available simulators."),
                    new Option(new[] { "--booted" }, "Whether or not to only include the booted simulators."),
                    new Option<SimulatorRuntime>(new[] { "--runtime" }, "The runtime to use when filtering."),
                    new Option<string?>(new[] { "--version" }, description: "The runtime version to use when filtering. This could be in either <major> or <major>.<minor> version formats.",
                        parseArgument: CommandLine.ParseVersion),
                    CommandLine.CreateVerbosity(),
                    new Argument<string?>("TERM", "The search term to use when filtering simulators. This could be any number of properties (UDID, runtime, version, availability, or state) as well as part of the simulator name.")
                        { Arity = ArgumentArity.ZeroOrOne },
                }.WithHandler(CommandHandler.Create(typeof(AndroidCommand).GetMethod(nameof(HandleListAsync))!)),
                new Command("create", "Create a new virtual device.")
                {
                    new Option<string?>(new[] { "--sdk" }, "The path to the Android SDK directory."),
                    new Option(new[] { "--replace" }, "Replace any existing virtual devices with the same name."),
                    CommandLine.CreateVerbosity(),
                    new Argument<string?>("NAME", "The name of the new virtual device."),
                    new Argument<string?>("PACKAGE", "The package to use for the new virtual device."),
                }.WithHandler(CommandHandler.Create(typeof(AndroidCommand).GetMethod(nameof(HandleCreateAsync))!)),
                new Command("boot", "Boot a particular simulator.")
                {
                    new Option<string?>(new[] { "--sdk" }, "Whether or not to only include the available simulators."),
                    CommandLine.CreateVerbosity(),
                    new Argument<string?>("NAME", "The UDID of the simulator to boot."),
                }.WithHandler(CommandHandler.Create(typeof(AndroidCommand).GetMethod(nameof(HandleBootAsync))!)),
            };
        }

        public static async Task HandleListAsync(
            string? term = null,
            string? sdk = null,
            bool available = false,
            bool booted = false,
            SimulatorRuntime? runtime = null,
            string? version = null,
            string? verbosity = null,
            IConsole console = null!,
            CancellationToken cancellationToken = default)
        {
            var logger = console.CreateLogger(verbosity);
            var avdmanager = new AVDManager(sdk, logger);

            var devices = await avdmanager.GetDevicesAsync();
            foreach (var device in devices)
            {
                logger?.LogInformation(" - " + device.ToString());
            }

            var targets = await avdmanager.GetTargetsAsync();
            foreach (var target in targets)
            {
                logger?.LogInformation(" - " + target.ToString());
            }

            var avds = await avdmanager.GetVirtualDevicesAsync();
            foreach (var avd in avds)
            {
                logger?.LogInformation(" - " + avd.ToString());
            }

            try
            {
                await avdmanager.DeleteVirtualDeviceAsync("TESTING");
            }
            catch { }

            try
            {
                await avdmanager.DeleteVirtualDeviceAsync("TESTED");
            }
            catch { }

            await avdmanager.CreateVirtualDeviceAsync("TESTING", "system-images;android-28;google_apis_playstore;x86_64");

            await avdmanager.CreateVirtualDeviceAsync("TESTING", "system-images;android-28;google_apis_playstore;x86_64", new VirtualDeviceCreateOptions { Overwrite = true });

            await avdmanager.RenameVirtualDeviceAsync("TESTING", "TESTED");
            await avdmanager.MoveVirtualDeviceAsync("TESTED", "/Users/matthew/.android/avd/tested.avd");

            //await avdmanager.DeleteVirtualDeviceAsync("TESTING");

            //term = term?.ToLowerInvariant()?.Trim();

            //var simctl = new SimulatorControl(logger);
            //var simulators = await simctl.GetSimulatorsAsync(cancellationToken);

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

            //var all = filtered.ToList();

            //logger.LogInformation($"Found {all.Count} simulator[s].");

            //var table = new TableView<Simulator>();
            //table.AddColumn(s => s.Udid, "UDID");
            //table.AddColumn(s => s.Name, "Name");
            //table.AddColumn(s => s.Runtime, "Runtime");
            //table.AddColumn(s => s.Version, "Version");
            //table.AddColumn(s => s.Availability, "Availability");
            //table.AddColumn(s => s.State, "State");
            //table.Items = all;

            //console.Append(new StackLayoutView { table });
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

            var options = new VirtualDeviceCreateOptions
            {
                Overwrite = replace
            };

            await avdmanager.CreateVirtualDeviceAsync(name, package, options, cancellationToken);
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
            if (avds.All(a => !a.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
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
    }
}
