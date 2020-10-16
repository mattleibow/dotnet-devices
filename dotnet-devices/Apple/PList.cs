using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using DotNetDevices.Logging;
using DotNetDevices.Processes;
using Microsoft.Extensions.Logging;

namespace DotNetDevices.Apple
{
    public class PList
    {
        private readonly string plistPath;
        private readonly ILogger? logger;
        private readonly ProcessRunner processRunner;

        public XDocument? xdoc;

        public PList(string plistPath, ILogger? logger = null)
        {
            this.plistPath = plistPath ?? throw new ArgumentNullException(nameof(plistPath));
            this.logger = logger;

            processRunner = new ProcessRunner(logger);
        }

        public async Task<XDocument> GetDocumentAsync(CancellationToken cancellationToken = default)
        {
            if (xdoc != null)
                return xdoc;

            logger?.LogDebug($"Loading PList {plistPath}...");

            try
            {
                using var stream = File.OpenRead(plistPath);
                xdoc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);

                return xdoc;
            }
            catch (XmlException)
            {
                logger?.LogTrace("Unable to load PList as XML, trying a decoded version...");

                var result = await processRunner.RunAsync("plutil", $"-convert xml1 -o - \"{plistPath}\"").ConfigureAwait(false);

                using var reader = new StringReader(result.Output);
                xdoc = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken).ConfigureAwait(false);

                return xdoc;
            }
        }

        public async Task<string?> GetStringValueAsync(string key, CancellationToken cancellationToken = default)
        {
            var xdoc = await GetDocumentAsync(cancellationToken).ConfigureAwait(false);

            var keyElements = xdoc.Descendants("key").Where(x => x.Value == key).ToArray();
            if (keyElements.Length == 0)
                return null;

            if (keyElements.Length > 1)
                throw new Exception($"Found multiple instances of key {key}.");

            var value = keyElements[0].ElementsAfterSelf().FirstOrDefault();
            if (value == null)
                throw new Exception($"Unable to find value for key {key}.");

            return value.Value;
        }

        public Task<string?> GetBundleIdentifierAsync(CancellationToken cancellationToken = default) =>
            GetStringValueAsync("CFBundleIdentifier", cancellationToken);
    }
}
