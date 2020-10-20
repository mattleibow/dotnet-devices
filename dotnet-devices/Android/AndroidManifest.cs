using System;
using System.Linq;
using System.Xml.Linq;

namespace DotNetDevices.Android
{
    public class AndroidManifest
    {
        private static readonly XNamespace xmlnsAndroid = "http://schemas.android.com/apk/res/android";

        public AndroidManifest(XDocument xdoc)
        {
            Document = xdoc ?? throw new ArgumentNullException(nameof(xdoc));
        }

        public XDocument Document { get; }

        public string? PackageName => 
            Document.Root
                ?.Attribute("package")?.Value;

        public string? MainLauncherActivity =>
            Document.Root
                ?.Element("application")
                ?.Elements("activity")
                ?.FirstOrDefault(a =>
                    a?.Element("intent-filter")
                        ?.Element("action")?.Attribute(xmlnsAndroid + "name")?.Value == "android.intent.action.MAIN" &&
                    a?.Element("intent-filter")
                        ?.Element("category")?.Attribute(xmlnsAndroid + "name")?.Value == "android.intent.category.LAUNCHER")
                ?.Attribute(xmlnsAndroid + "name")
                ?.Value;
    }
}
