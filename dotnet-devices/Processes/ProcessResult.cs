using System;
using System.Text;

namespace DotNetDevices.Processes
{
    public class ProcessResult
    {
        private readonly ProcessOutput[] outputItems;

        private StringBuilder? outputBuilder = null;
        private string? outputString = null;

        public ProcessResult(ProcessOutput[]? output, int exitCode, DateTimeOffset start, long elapsed)
            : this(output, exitCode, start, TimeSpan.FromMilliseconds(elapsed))
        {
        }

        public ProcessResult(ProcessOutput[]? output, int exitCode, DateTimeOffset start, TimeSpan elapsed)
        {
            outputItems = output ?? Array.Empty<ProcessOutput>();
            ExitCode = exitCode;
            StartTimestamp = start;
            Elapsed = elapsed;
        }

        public string Output =>
            outputString ??= GetOutputBuilder().ToString();

        public int ExitCode { get; }

        public DateTimeOffset StartTimestamp { get; }

        public TimeSpan Elapsed { get; }

        public override string ToString() =>
            $"Completed with exit code {ExitCode} in {Elapsed}.";

        private StringBuilder GetOutputBuilder()
        {
            if (outputBuilder == null)
            {
                var builder = new StringBuilder();
                foreach (var processOutput in outputItems)
                {
                    if (processOutput != null)
                        builder.AppendLine(processOutput.Data);
                }
                outputBuilder = builder;
            }

            return outputBuilder;
        }
    }
}
