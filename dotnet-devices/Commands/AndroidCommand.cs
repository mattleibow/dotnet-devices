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
            return new Command("android", "Work with Android emulators.")
            {
                new Command("list", "List the emulators.")
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
                new Command("boot", "Boot a particular simulator.")
                {
                    CommandLine.CreateVerbosity(),
                    new Argument<string?>("UDID", ParseUdid)
                    {
                        Description = "The UDID of the simulator to boot.",
                        Arity = ArgumentArity.ExactlyOne
                    },
                }.WithHandler(CommandHandler.Create(typeof(AndroidCommand).GetMethod(nameof(HandleBootAsync))!)),
            };

            static string? ParseUdid(ArgumentResult result)
            {
                var udid = result.Tokens[0].Value;

                if (Guid.TryParse(udid, out _))
                    return udid;

                result.ErrorMessage = "The UDID must be a valid UDID.";
                return null;
            }
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

            await avdmanager.CreateVirtualDeviceAsync("TESTING", "system-images;android-28;google_apis_playstore;x86_64");

            await avdmanager.CreateVirtualDeviceAsync("TESTING", "system-images;android-28;google_apis_playstore;x86_64", new VirtualDeviceCreateOptions { Overwrite = true });

            await avdmanager.DeleteVirtualDeviceAsync("TESTING");

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

        public static async Task<int> HandleBootAsync(
            string udid,
            string? verbosity = null,
            IConsole console = null!,
            CancellationToken cancellationToken = default)
        {
            var logger = console.CreateLogger(verbosity);

            var simctl = new SimulatorControl(logger);
            var simulator = await simctl.GetSimulatorAsync(udid, cancellationToken);

            if (simulator == null)
            {
                logger.LogError($"No simulator with UDID {udid} was found.");
                return 1;
            }

            if (simulator.State == SimulatorState.Booted)
                logger.LogInformation($"Simulator was already booted.");
            else
                await simctl.BootSimulatorAsync(udid, cancellationToken);

            return 0;
        }
    }
}
