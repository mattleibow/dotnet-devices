using System;

namespace DotNetDevices.Apple
{
    public class Simulator
    {
        public Simulator(
            string udid,
            string name,
            SimulatorRuntime runtime,
            Version version,
            SimulatorState state,
            SimulatorAvailability availability,
            string? dataPath,
            string? logPath)
        {
            Udid = udid ?? throw new ArgumentNullException(nameof(udid));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Runtime = runtime;
            Version = version ?? throw new ArgumentNullException(nameof(version));
            State = state;
            Availability = availability;
            DataPath = dataPath;
            LogPath = logPath;
        }

        public string Udid { get; }

        public string Name { get; }

        public SimulatorRuntime Runtime { get; }

        public Version Version { get; }

        public SimulatorState State { get; }

        public SimulatorAvailability Availability { get; }

        public string? DataPath { get; }

        public string? LogPath { get; }

        public SimulatorType Type
        {
            get
            {
                if (Name.StartsWith("iPhone", StringComparison.OrdinalIgnoreCase))
                    return SimulatorType.iPhone;
                if (Name.StartsWith("iPad", StringComparison.OrdinalIgnoreCase))
                    return SimulatorType.iPad;
                if (Name.StartsWith("iPod", StringComparison.OrdinalIgnoreCase))
                    return SimulatorType.iPod;
                if (Name.StartsWith("Apple Watch", StringComparison.OrdinalIgnoreCase))
                    return SimulatorType.AppleWatch;
                if (Name.StartsWith("Apple TV", StringComparison.OrdinalIgnoreCase))
                    return SimulatorType.AppleTV;

                return SimulatorType.Unknown;
            }
        }

        public override string ToString() =>
            $"{Name} ({Version}) [{Availability}]";
    }
}
