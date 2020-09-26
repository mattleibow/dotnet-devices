using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace DotNetDevices.Android
{
    public static class AndroidSDK
    {
        private static readonly string[] envvars = { "ANDROID_HOME", "ANDROID_SDK_ROOT" };

        public static string? FindPath(string? sdkRoot = null, string? toolPath = null, ILogger? logger = null)
        {
            sdkRoot = sdkRoot?.Trim();
            toolPath = toolPath?.Trim();

            // check and throw exceptions because we asked for this
            if (!string.IsNullOrEmpty(sdkRoot))
            {
                if (!Directory.Exists(sdkRoot))
                    throw new DirectoryNotFoundException($"Android SDK directory '{sdkRoot}' is invalid.");

                // return the SDK if we just asked for that
                if (string.IsNullOrEmpty(toolPath))
                    return sdkRoot;

                var path = Path.Combine(sdkRoot, toolPath);
                if (!File.Exists(path))
                    throw new FileNotFoundException($"Path to tool '{toolPath}' was not found in the SDK '{sdkRoot}'.", path);

                // return the full path to the tool
                return path;
            }

            foreach (var envvar in envvars)
            {
                var varSdkRoot = Environment.GetEnvironmentVariable(envvar);
                if (string.IsNullOrEmpty(varSdkRoot))
                    continue;

                if (!Directory.Exists(varSdkRoot))
                {
                    logger?.LogWarning($"Found environment variable '{envvar}' with value '{varSdkRoot}', but it was invalid.");
                    continue;
                }

                // return the SDK if we just asked for that
                if (string.IsNullOrEmpty(toolPath))
                    return varSdkRoot;

                var path = Path.Combine(varSdkRoot, toolPath);
                if (!File.Exists(path))
                {
                    logger?.LogWarning($"Found SDK at '{varSdkRoot}', but it did not contan the tool '{toolPath}'.");
                    continue;
                }

                // return the full path to the tool
                return path;
            }

            return null;
        }
    }
}
