using System;

namespace DotNetDevices.Android
{
    public class VirtualDevice
    {
        private readonly string? configPath;

        public VirtualDevice(string id, string name, string? configPath = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Name = name ?? throw new ArgumentNullException(nameof(name));

            this.configPath = configPath;
        }

        public string Id { get; }

        public string Name { get; }

        public override string ToString() =>
            $"{Name}";
    }
}
