using DotNetDevices.Android;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace DotNetDevices.Tests
{
    public class AaptToolTests
    {
        public class ParseXmlTree
        {
            private static readonly XNamespace AndroidNamespace = "http://schemas.android.com/apk/res/android";

            [Theory]
            [InlineData("TestData/Android/CompiledXmlDump.txt")]
            public void CanParse(string file)
            {
                var xmltree = File.ReadAllText(file);

                var xdoc = AaptTool.ParseXmlTree(xmltree);

                Assert.NotNull(xdoc);
            }

            [Fact]
            public void ParseIsValid()
            {
                var xmltree = File.ReadAllText("TestData/Android/CompiledXmlDump.txt");
                var xdoc = AaptTool.ParseXmlTree(xmltree);
                Assert.NotNull(xdoc);

                var manifest = xdoc.Root;
                Assert.Equal(AndroidNamespace, manifest.GetNamespaceOfPrefix("android"));
                Assert.Equal("(type 0x10)0x1", manifest.Attribute(AndroidNamespace + "versionCode").Value);
                Assert.Equal("1.0.1.0", manifest.Attribute(AndroidNamespace + "versionName").Value);
                Assert.Equal("10", manifest.Attribute(AndroidNamespace + "compileSdkVersionCodename").Value);
                Assert.Equal("net.dot.devicetests", manifest.Attribute("package").Value);
                Assert.Equal("(type 0x10)0x1d", manifest.Attribute("platformBuildVersionCode").Value);

                var usessdk = manifest.Element("uses-sdk");
                Assert.Equal("(type 0x10)0x13", usessdk.Attribute(AndroidNamespace + "minSdkVersion").Value);

                var usespermissions = manifest.Elements("uses-permission").ToList();
                Assert.Equal(2, usespermissions.Count);
                Assert.Equal("android.permission.INTERNET", usespermissions[0].Attribute(AndroidNamespace + "name").Value);

                var application = manifest.Element("application");
                Assert.Equal("@0x7f0c001b", application.Attribute(AndroidNamespace + "label").Value);
                Assert.Equal("android.app.Application", application.Attribute(AndroidNamespace + "name").Value);
            }
        }
    }
}
