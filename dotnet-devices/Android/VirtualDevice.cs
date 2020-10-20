using System;

namespace DotNetDevices.Android
{
    public class VirtualDevice
    {
        public VirtualDevice(string id, string name, string package, VirtualDeviceType type, int apiLevel, string? avdPath)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Package = package ?? throw new ArgumentNullException(nameof(package));
            Type = type;
            ApiLevel = apiLevel;
            AvdPath = avdPath;
        }

        public string Id { get; }

        public string Name { get; }

        public string Package { get; }

        public VirtualDeviceType Type { get; }

        public int ApiLevel { get; }

        public string? AvdPath { get; }

        public Version Version =>
            ApiLevel switch
            {
                1 => new Version(1, 0),
                2 => new Version(1, 1),
                3 => new Version(1, 5),
                4 => new Version(1, 6),
                5 => new Version(2, 0),
                6 => new Version(2, 0, 1),
                7 => new Version(2, 1),
                8 => new Version(2, 2),
                9 => new Version(2, 3),
                10 => new Version(2, 3, 3),
                11 => new Version(3, 0),
                12 => new Version(3, 1),
                13 => new Version(3, 2),
                14 => new Version(4, 0),
                15 => new Version(4, 0, 3),
                16 => new Version(4, 1),
                17 => new Version(4, 2),
                18 => new Version(4, 3),
                19 => new Version(4, 4),
                20 => new Version(4, 4), // 4.4W (wear)
                21 => new Version(5, 0),
                22 => new Version(5, 1),
                23 => new Version(6, 0),
                24 => new Version(7, 0),
                25 => new Version(7, 1),
                26 => new Version(8, 0),
                27 => new Version(8, 1),
                28 => new Version(9, 0),
                29 => new Version(10, 0),
                30 => new Version(11, 0),
                _ => new Version(),
            };

        public VirtualDeviceRuntime Runtime =>
            Type switch
            {
                VirtualDeviceType.Unknown => VirtualDeviceRuntime.Android,
                VirtualDeviceType.Phone => VirtualDeviceRuntime.Android,
                VirtualDeviceType.Tablet => VirtualDeviceRuntime.Android,
                VirtualDeviceType.Wearable => VirtualDeviceRuntime.AndroidWear,
                VirtualDeviceType.TV => VirtualDeviceRuntime.AndroidTV,
                _ => VirtualDeviceRuntime.Android,
            };

        public override string ToString() =>
            $"{Name} (API {ApiLevel})";
    }
}
