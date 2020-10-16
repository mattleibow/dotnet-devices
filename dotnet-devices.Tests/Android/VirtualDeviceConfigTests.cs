using DotNetDevices.Android;
using System.Threading.Tasks;
using Xunit;

namespace DotNetDevices.Tests
{
    public class VirtualDeviceConfigTests
    {
        public class CreateVirtualDeviceAsync
        {
            [Theory]
            [InlineData("TestData/Android/AvdConfigIni_Normal.txt", "pixel_2_q_10_0_-_api_29", "Pixel 2 Q 10.0 - API 29")]
            [InlineData("TestData/Android/AvdConfigIni_Tiny.txt", "pixel_2_q_10_0_-_api_29", "pixel_2_q_10_0_-_api_29")]
            public async Task CanCreateInstance(string file, string id, string name)
            {
                var config = new VirtualDeviceConfig(file);

                var device = await config.CreateVirtualDeviceAsync();

                Assert.NotNull(device);
                Assert.Equal(id, device.Id);
                Assert.Equal(name, device.Name);
            }
        }
    }
}
