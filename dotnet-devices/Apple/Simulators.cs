using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DotNetDevices.Apple
{
    public class Simulators : IEnumerable<Simulator>
    {
        public Simulators()
        {
            All = Array.Empty<Simulator>();
        }

        public Simulators(IReadOnlyList<Simulator> simulators)
        {
            All = simulators ?? throw new ArgumentNullException(nameof(simulators));
        }

        public IReadOnlyList<Simulator> All { get; }

        public Simulator this[string udid] =>
            All.FirstOrDefault(v => v.Udid == udid);

        public IReadOnlyList<Simulator> GetAvailable(
            SimulatorRuntime? runtime = null,
            Version? version = null,
            Version? minVersion = null,
            Version? maxVersion = null,
            SimulatorState? state = null,
            string? deviceName = null,
            SimulatorType? type = null)
        {
            var all = All.Where(s => s.Availability == SimulatorAvailability.Available);

            if (runtime != null)
                all = all.Where(s => s.Runtime == runtime);

            if (version != null)
                all = all.Where(s => s.Version == version);

            if (minVersion != null)
                all = all.Where(s => s.Version >= minVersion);

            if (maxVersion != null)
                all = all.Where(s => s.Version < maxVersion);

            if (state != null)
                all = all.Where(s => s.State == state);

            if (deviceName != null)
                all = all.Where(s => s.Name == deviceName);

            if (type != null)
                all = all.Where(s => s.Type == type);

            return all.ToList();
        }

        public IEnumerator<Simulator> GetEnumerator() =>
            All.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
