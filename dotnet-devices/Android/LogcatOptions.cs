using DotNetDevices.Processes;
using System;

namespace DotNetDevices.Android
{
    public class LogcatOptions
    {
        public bool DumpOnly { get; set; }

        public Action<ProcessOutput>? HandleOutput { get; set; }
    }
}
