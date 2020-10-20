using DotNetDevices.Processes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DotNetDevices.Android
{
    public class Aapt
    {
        private readonly static Regex xmltreeNamespaceRegex = new Regex(@"^N:\s*(?<ns>[^=]+)=(?<url>.*)$");
        private readonly static Regex xmltreeElementRegex = new Regex(@"^E:\s*((?<ns>[^:]+):)?(?<name>.*) \(line=\d+\)$");
        private readonly static Regex xmltreeAttributeRegex = new Regex(@"^A:\s*((?<ns>[^:]+):)?(?<name>[^(]+)(\(.*\))?=(?<value>.*)$");

        private readonly ProcessRunner processRunner;
        private readonly ILogger? logger;
        private readonly string aapt;

        public Aapt(string? sdkRoot = null, ILogger? logger = null)
        {
            this.logger = logger;
            aapt = AndroidSDK.FindBuildToolPath(sdkRoot, "aapt", logger)
                ?? throw new ArgumentException($"Unable to locate aapt. Make sure that ANDROID_HOME or ANDROID_SDK_ROOT is set.");

            processRunner = new ProcessRunner(logger);
        }

        public async Task<AndroidManifest> GetAndroidManifestAsync(string apk, CancellationToken cancellationToken = default)
        {
            logger?.LogInformation("Loading AndroidManifest.xml...");

            var args = $"dump xmltree \"{apk}\" AndroidManifest.xml";

            var result = await processRunner.RunAsync(aapt, args, null, cancellationToken).ConfigureAwait(false);

            return new AndroidManifest(ParseXmlTree(result.Output));
        }

        public static XDocument ParseXmlTree(string xmltree)
        {
            var lines = xmltree.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            var xdoc = new XDocument();

            var stack = new Stack<ParsedElement>();
            stack.Push(new ParsedElement(xdoc, 0));

            var namespaces = new Dictionary<string, XNamespace>();

            foreach (var line in lines)
            {
                ParseXmlTreeLine(line, stack, namespaces);
            }

            return xdoc;
        }

        private static void ParseXmlTreeLine(string line, Stack<ParsedElement> stack, Dictionary<string, XNamespace> namespaces)
        {
            var trimmedLine = line.TrimStart();
            var indent = line.Length - trimmedLine.Length;

            if (trimmedLine.StartsWith("N"))
            {
                var match = xmltreeNamespaceRegex.Match(trimmedLine);
                if (!match.Success)
                    throw new Exception($"Invalid namespace: {line}");

                var namespaceName = match.Groups["ns"].Value;
                if (!namespaces.ContainsKey(namespaceName))
                    namespaces.Add(namespaceName, XNamespace.Get(match.Groups["url"].Value));
            }
            else if (trimmedLine.StartsWith("E"))
            {
                // pop out if the current line is higher than previous
                while (stack.Count > 0 && stack.Peek().Indent >= indent)
                    stack.Pop();

                var match = xmltreeElementRegex.Match(trimmedLine);
                if (!match.Success)
                    throw new Exception($"Invalid element: {line}");

                var element = new XElement(GetXName(match, namespaces, line));

                // this is the first element, so add the namespaces to it
                if (stack.Count == 1)
                {
                    foreach (var pair in namespaces)
                    {
                        element.Add(new XAttribute(XNamespace.Xmlns + pair.Key, pair.Value));
                    }
                }

                stack.Peek().Container.Add(element);
                stack.Push(new ParsedElement(element, indent));
            }
            else if (trimmedLine.StartsWith("A"))
            {
                var match = xmltreeAttributeRegex.Match(trimmedLine);
                if (!match.Success)
                    throw new Exception($"Invalid attribute: {line}");

                // TODO: parse the (type) and use the correct value

                var value = match.Groups["value"].Value;
                var strMatch = Regex.Match(value, @"\""(?<value>.*)\""\s*\(Raw:.*\)");
                var xName = GetXName(match, namespaces, line);
                stack.Peek().Container.Add(strMatch.Success
                    ? new XAttribute(xName, strMatch.Groups["value"].Value)
                    : new XAttribute(xName, value));
            }
        }

        static XName GetXName(Match match, Dictionary<string, XNamespace> namespaces, string line)
        {
            var namespaceName = match.Groups["ns"].Value;
            if (!string.IsNullOrWhiteSpace(namespaceName) && !namespaces.ContainsKey(namespaceName))
                throw new Exception($"Unknown xml namespace: {namespaceName}.");

            XName xName;
            try
            {
                xName = string.IsNullOrWhiteSpace(namespaceName)
                    ? XName.Get(match.Groups["name"].Value)
                    : XName.Get(match.Groups["name"].Value, namespaces[namespaceName].ToString());
            }
            catch
            {
                throw new Exception($"Invalid attribute: {line}");
            }
            return xName;
        }

        class ParsedElement
        {
            public ParsedElement(XContainer container, int indent)
            {
                Container = container;
                Indent = indent;
            }

            public XContainer Container { get; }

            public int Indent { get; }
        }
    }
}
