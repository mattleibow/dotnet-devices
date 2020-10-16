using DotNetDevices.Processes;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetDevices.Android
{
    public class SDKManager
    {
        private readonly ProcessRunner processRunner;
        private readonly ILogger? logger;
        private readonly string sdkmanager;

        public SDKManager(string? sdkRoot = null, ILogger? logger = null)
        {
            processRunner = new ProcessRunner(logger);
            this.logger = logger;

            sdkmanager = AndroidSDK.FindPath(sdkRoot, Path.Combine("tools", "bin", "sdkmanager"), logger)
                ?? throw new ArgumentException($"Unable to locate the SDK Manager. Make sure that ANDROID_HOME or ANDROID_SDK_ROOT is set.");
        }

        public async Task InstallAsync(string package, CancellationToken cancellationToken = default)
        {
            logger?.LogInformation("Installing packages...");

            var args = $"--install \"{package}\"";

            await processRunner.RunAsync(sdkmanager, args, null, cancellationToken).ConfigureAwait(false);
        }
    }
}
