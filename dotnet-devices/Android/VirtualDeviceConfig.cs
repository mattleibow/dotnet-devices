using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetDevices.Android
{
    public class VirtualDeviceConfig
    {
        private readonly string configPath;
        private readonly ILogger? logger;

        public Dictionary<string, string>? properties;

        public VirtualDeviceConfig(string configPath, ILogger? logger = null)
        {
            this.configPath = configPath ?? throw new ArgumentNullException(nameof(configPath));
            this.logger = logger;
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
                throw new Exception($"Invalid config.ini. Unable to find the virtual device ID.");

            if (!props.TryGetValue("avd.ini.displayname", out var name))
                name = id;

            return new VirtualDevice(id, name, configPath);
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
