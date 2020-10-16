using Microsoft.Extensions.Logging;
using System;
using System.IO;

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
                var foundPath = FindFuzzyPath(path);
                if (foundPath == null)
                    throw new FileNotFoundException($"Path to tool '{toolPath}' was not found in the SDK '{sdkRoot}'.", path);

                // return the full path to the tool
                return foundPath;
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
                var foundPath = FindFuzzyPath(path);
                if (foundPath == null)
                {
                    logger?.LogWarning($"Found SDK at '{varSdkRoot}', but it did not contan the tool '{toolPath}'.");
                    continue;
                }

                // return the full path to the tool
                return foundPath;
            }

            return null;
        }

        public static string? FindBuildToolPath(string? sdkRoot, string tool, ILogger? logger)
        {
            var newSdkRoot = FindPath(sdkRoot, null, logger);
            if (newSdkRoot == null)
            {
                logger?.LogDebug($"Unable to resolve Android SDK root directory from '{sdkRoot}'.");
                return null;
            }

            var versions = Directory.GetDirectories(Path.Combine(newSdkRoot, "build-tools"));
            if (versions.Length == 0)
            {
                logger?.LogDebug($"Unable to locate any build tools in '{newSdkRoot}'.");
                return null;
            }
            else
            {
                logger?.LogDebug($"Found {versions.Length} build tools versions in '{newSdkRoot}'.");
            }

            var path = default(string);
            var latestSoFar = new Version();

            foreach (var versionDir in versions)
            {
                var v = Path.GetFileName(versionDir);
                if (Version.TryParse(v, out var version) && version > latestSoFar)
                {
                    var foundPath = FindFuzzyPath(Path.Combine(versionDir, tool));
                    if (foundPath != null)
                    {
                        latestSoFar = version;
                        path = foundPath;
                    }
                }
                else
                {
                    logger?.LogDebug($"Found invalid build tool version: '{v}'.");
                }
            }

            if (path == null)
                logger?.LogDebug($"Unable to find any build tools in  '{newSdkRoot}'.");
            else
                logger?.LogDebug($"Found build tool '{path}'.");

            return path;
        }

        private static string? FindFuzzyPath(string path)
        {
            if (File.Exists(path))
                return path;

            var otherPath = Path.ChangeExtension(path, ".exe");
            if (File.Exists(otherPath))
                return otherPath;

            otherPath = Path.ChangeExtension(path, ".bat");
            if (File.Exists(otherPath))
                return otherPath;

            return null;
        }
    }
}
