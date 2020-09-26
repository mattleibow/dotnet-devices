using System;

namespace DotNetDevices.Android
{
    public class Device
    {
        public Device(string id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        public string Id { get; }

        public override string ToString() => Id;
    }
}
