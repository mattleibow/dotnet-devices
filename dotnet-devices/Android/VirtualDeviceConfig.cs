using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetDevices.Android
{
    public class VirtualDeviceConfig
    {
        private static readonly Regex androidApiRegex = new Regex(@"android-(\d+)");

        private readonly string configPath;
        private readonly ILogger? logger;
        private readonly string? avdPath;

        public Dictionary<string, string>? properties;

        public VirtualDeviceConfig(string avdPath, ILogger? logger = null)
        {
            this.avdPath = avdPath ?? throw new ArgumentNullException(nameof(avdPath));
            this.logger = logger;

            configPath = Path.Combine(avdPath, "config.ini");
        }

        public async Task<IReadOnlyDictionary<string, string>> GetPropertiesAsync(CancellationToken cancellationToken = default)
        {
            if (properties != null)
                return properties;

            logger?.LogDebug($"Loading config.ini {configPath}...");

            var contents = await File.ReadAllTextAsync(configPath, cancellationToken);
            properties = ParseConfig(contents);

            return properties;
        }

        public async Task<string?> GetStringValueAsync(string key, CancellationToken cancellationToken = default)
        {
            var props = await GetPropertiesAsync(cancellationToken).ConfigureAwait(false);

            props.TryGetValue(key, out var value);

            return value;
        }

        public async Task<VirtualDevice> CreateVirtualDeviceAsync(CancellationToken cancellationToken = default)
        {
            var props = await GetPropertiesAsync(cancellationToken).ConfigureAwait(false);

            if (!props.TryGetValue("avdid", out var id))
                id = Path.GetFileNameWithoutExtension(avdPath);

            if (string.IsNullOrEmpty(id))
                throw new Exception($"Invalid config.ini. Unable to find the virtual device ID.");

            if (!props.TryGetValue("avd.ini.displayname", out var name))
                name = id;

            if (!props.TryGetValue("image.sysdir.1", out var package))
                package = "";
            var packageParts = package.Split(new[] { '\\', '/', ';' }, StringSplitOptions.RemoveEmptyEntries);
            package = string.Join(";", packageParts);

            var apiLevel = 0;
            if (packageParts.Length == 4)
            {
                var apiMatch = androidApiRegex.Match(packageParts[1]);
                if (apiMatch.Success)
                    apiLevel = int.Parse(apiMatch.Groups[1].Value);
            }

            if (!TryGetType(props, out var type))
                type = VirtualDeviceType.Unknown;

            return new VirtualDevice(id, name, package, type, apiLevel, avdPath);
        }

        private static bool TryGetType(IReadOnlyDictionary<string, string> props, out VirtualDeviceType value)
        {
            if (props.TryGetValue("tag.id", out var type))
            {
                switch (type.Trim().ToLowerInvariant())
                {
                    case "android-tv":
                        value = VirtualDeviceType.TV;
                        return true;
                    case "android-wear":
                        value = VirtualDeviceType.Wearable;
                        return true;
                    case "default":
                    case "google_apis":
                    case "google_apis_playstore":
                        value =
                            TryGetDimensions(props, out var width, out var height, out var density)
                            && Math.Min(width, height) / (density / 160) >= 600
                                ? VirtualDeviceType.Tablet
                                : VirtualDeviceType.Phone;
                        return true;
                }
            }

            value = VirtualDeviceType.Unknown;
            return false;
        }

        private static bool TryGetDimensions(IReadOnlyDictionary<string, string> props, out int width, out int height, out double density)
        {
            width = 0;
            height = 0;
            density = 160;

            if (!props.TryGetValue("hw.lcd.width", out var widthString) || !int.TryParse(widthString, out width))
                return false;

            if (!props.TryGetValue("hw.lcd.height", out var heightString) || !int.TryParse(heightString, out height))
                return false;

            if (!props.TryGetValue("hw.lcd.density", out var densityString) || !double.TryParse(densityString, out density))
                density = 160;

            return true;
        }

        private static Dictionary<string, string> ParseConfig(string contents)
        {
            var lines = contents.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            Dictionary<string, string> props = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                var pair = line.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                if (pair.Length == 2)
                {
                    props[pair[0].ToLowerInvariant()] = pair[1];
                }
            }

            return props;
        }
    }
}
