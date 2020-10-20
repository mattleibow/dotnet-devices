using System;
using System.Text.RegularExpressions;

namespace DotNetDevices.Android
{
    public class ConnectedDevice
    {
        private static readonly Regex emulatorPortRegex = new Regex(@"emulator-(\d+)");

        public ConnectedDevice(string serial, ConnectedDeviceState state)
        {
            Serial = serial ?? throw new ArgumentNullException(nameof(serial));
            State = state;

            var match = emulatorPortRegex.Match(Serial);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var newPort))
                Port = newPort;
            else
                Port = -1;
        }

        public string Serial { get; }

        public ConnectedDeviceState State { get; }

        public int Port { get; }
    }
}
