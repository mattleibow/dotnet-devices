using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DotNetDevices.Logging;
using Microsoft.Extensions.Logging;

namespace DotNetDevices.Processes
{
    public class ProcessRunner
    {
        private readonly ILogger? logger;

        public ProcessRunner(ILogger? logger = null)
        {
            this.logger = logger;
        }

        public async Task<ProcessResult> RunAsync(string path, string? arguments = null, Action<ProcessOutput>? handleOutput = null, CancellationToken cancellationToken = default)
        {
            var output = await RunProcessAsync(FindCommand(path), arguments, handleOutput, cancellationToken);

            if (output.ExitCode != 0)
                throw new Exception($"Failed to execute: {path} {arguments} - exit code: {output.ExitCode}{Environment.NewLine}{output.Output}");

            logger?.LogDebug(output.ToString());
            logger?.LogTrace(output.Output);

            return output;
        }

        private string FindCommand(string path, bool allowOSFallback = true)
        {
            if (!File.Exists(path))
            {
                var exePath = Path.ChangeExtension(path, "exe");
                var noExtensionPath = Path.ChangeExtension(path, null);

                if (File.Exists(exePath))
                    path = exePath;
                else if (File.Exists(noExtensionPath))
                    path = noExtensionPath;
                else if (!allowOSFallback)
                    throw new FileNotFoundException("Unable to find command file.", path);
            }

            return path;
        }

        private Task<ProcessResult> RunProcessAsync(string path, string? arguments = null, Action<ProcessOutput>? handleOutput = null, CancellationToken cancellationToken = default)
        {
            var psi = new ProcessStartInfo
            {
                FileName = path,
                CreateNoWindow = true
            };

            if (arguments != null)
                psi.Arguments = arguments;

            logger?.LogDebug($"Starting process {path} {arguments} in {Environment.CurrentDirectory}...");

            return psi.RunAsync(handleOutput, cancellationToken);
        }
    }
}
