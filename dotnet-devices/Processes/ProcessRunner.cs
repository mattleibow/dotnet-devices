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

        public async Task<ProcessResult> RunAsync(string path, string? arguments = null, Func<ProcessOutput, bool>? handleOutput = null, CancellationToken cancellationToken = default)
        {
            var result = await RunProcessAsync(FindCommand(path), arguments, null, handleOutput, cancellationToken);

            if (result.ExitCode != 0)
                throw new ProcessResultException(result, $"Failed to execute: {path} {arguments} - exit code: {result.ExitCode}{Environment.NewLine}{result.Output}");

            logger?.LogDebug(result.ToString());
            logger?.LogTrace(result.Output);

            return result;
        }

        public async Task<ProcessResult> RunWithInputAsync(string input, string path, string? arguments = null, Func<ProcessOutput, bool>? handleOutput = null, CancellationToken cancellationToken = default)
        {
            var result = await RunProcessAsync(FindCommand(path), arguments, input, handleOutput, cancellationToken);

            if (result.ExitCode != 0)
                throw new ProcessResultException(result, $"Failed to execute: {path} {arguments} - exit code: {result.ExitCode}{Environment.NewLine}{result.Output}");

            logger?.LogDebug(result.ToString());
            logger?.LogTrace(result.Output);

            return result;
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

        private Task<ProcessResult> RunProcessAsync(string path, string? arguments = null, string? input = null, Func<ProcessOutput, bool>? handleOutput = null, CancellationToken cancellationToken = default)
        {
            var psi = new ProcessStartInfo
            {
                FileName = path,
                CreateNoWindow = true,
            };

            if (arguments != null)
                psi.Arguments = arguments;

            logger?.LogDebug($"Starting process {path} {arguments} in {Environment.CurrentDirectory}...");

            return psi.RunAsync(input, handleOutput, cancellationToken);
        }
    }
}
