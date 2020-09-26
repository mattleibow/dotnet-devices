using System;

namespace DotNetDevices.Android
{
    public class VirtualDevice
    {
        public VirtualDevice(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public string Name { get; }

        public override string ToString() => Name;
    }
}
