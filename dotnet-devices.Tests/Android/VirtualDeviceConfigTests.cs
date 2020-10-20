using DotNetDevices.Android;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DotNetDevices.Tests
{
    public class VirtualDeviceConfigTests
    {
        public class CreateVirtualDeviceAsync
        {
            [Theory]
            [InlineData("TestData/Android/AvdConfigIni_Normal.avd", "pixel_2_q_10_0_-_api_29", "Pixel 2 Q 10.0 - API 29")]
            [InlineData("TestData/Android/AvdConfigIni_Tiny.avd", "pixel_2_q_10_0_-_api_29", "pixel_2_q_10_0_-_api_29")]
            public async Task CanCreateInstance(string file, string id, string name)
            {
                var config = new VirtualDeviceConfig(file);

                var device = await config.CreateVirtualDeviceAsync();

                Assert.NotNull(device);
                Assert.Equal(id, device.Id);
                Assert.Equal(name, device.Name);
            }

            [Fact]
            public async Task CanReadTV()
            {
                var config = new VirtualDeviceConfig("TestData/Android/AvdConfigIni_TV.avd");

                var device = await config.CreateVirtualDeviceAsync();

                Assert.Equal(VirtualDeviceType.TV, device.Type);
                Assert.Equal(new Version(10, 0), device.Version);
                Assert.Equal(29, device.ApiLevel);
                Assert.Equal(VirtualDeviceRuntime.AndroidTV, device.Runtime);
            }

            [Fact]
            public async Task CanReadWear()
            {
                var config = new VirtualDeviceConfig("TestData/Android/AvdConfigIni_Wear.avd");

                var device = await config.CreateVirtualDeviceAsync();

                Assert.Equal(VirtualDeviceType.Wearable, device.Type);
                Assert.Equal(new Version(9, 0), device.Version);
                Assert.Equal(28, device.ApiLevel);
                Assert.Equal(VirtualDeviceRuntime.AndroidWear, device.Runtime);
            }

            [Fact]
            public async Task CanReadTablet()
            {
                var config = new VirtualDeviceConfig("TestData/Android/AvdConfigIni_Tablet.avd");

                var device = await config.CreateVirtualDeviceAsync();

                Assert.Equal(VirtualDeviceType.Tablet, device.Type);
                Assert.Equal(new Version(9, 0), device.Version);
                Assert.Equal(28, device.ApiLevel);
                Assert.Equal(VirtualDeviceRuntime.Android, device.Runtime);
            }

            [Fact]
            public async Task CanReadGeneric()
            {
                var config = new VirtualDeviceConfig("TestData/Android/AvdConfigIni_Generic.avd");

                var device = await config.CreateVirtualDeviceAsync();

                Assert.Equal(VirtualDeviceType.Phone, device.Type);
                Assert.Equal(new Version(9, 0), device.Version);
                Assert.Equal(28, device.ApiLevel);
                Assert.Equal(VirtualDeviceRuntime.Android, device.Runtime);
            }

            [Fact]
            public async Task CanReadPhone()
            {
                var config = new VirtualDeviceConfig("TestData/Android/AvdConfigIni_Phone.avd");

                var device = await config.CreateVirtualDeviceAsync();

                Assert.Equal(VirtualDeviceType.Phone, device.Type);
                Assert.Equal(new Version(10, 0), device.Version);
                Assert.Equal(29, device.ApiLevel);
                Assert.Equal(VirtualDeviceRuntime.Android, device.Runtime);
            }
        }
    }
}
