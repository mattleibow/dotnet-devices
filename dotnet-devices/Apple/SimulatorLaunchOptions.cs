using System;
using DotNetDevices.Processes;

namespace DotNetDevices.Apple
{
    public class SimulatorLaunchOptions
    {
        public bool CaptureOutput { get; set; } = false;

        public bool BootSimulator { get; set; } = false;

        public Action<ProcessOutput>? HandleOutput { get; set; }
    }
}
