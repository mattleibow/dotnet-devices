using System;

namespace DotNetDevices.Processes
{
    public class ProcessOutput
    {
        public ProcessOutput(string data, long elapsed, bool isError = false)
            : this(data, TimeSpan.FromMilliseconds(elapsed), isError)
        {
        }

        public ProcessOutput(string data, TimeSpan elapsed, bool isError = false)
        {
            Data = data ?? string.Empty;
            Elapsed = elapsed;
            IsError = isError;
        }

        public string Data { get; }

        public TimeSpan Elapsed { get; }

        public bool IsError { get; }

        public override string ToString() => Data;
    }
}
