using System;
using System.Xml.Linq;

namespace DotNetDevices.Android
{
    public class AndroidManifest
    {
        public AndroidManifest(XDocument xdoc)
        {
            Document = xdoc ?? throw new ArgumentNullException(nameof(xdoc));
        }

        public XDocument Document { get; }

        public string? PackageName => Document.Root?.Attribute("package")?.Value;
    }
}
