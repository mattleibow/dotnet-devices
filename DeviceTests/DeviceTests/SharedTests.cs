using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DeviceTests
{
    public class SharedTests
    {
        static readonly Random rnd = new Random();

        [Fact]
        public async Task SuccessTest()
        {
            await Task.Delay(rnd.Next(1000, 3000));

            Assert.True(true);
        }

        [Fact]
        public async Task FailTest()
        {
            await Task.Delay(rnd.Next(1000, 3000));

            Assert.True(false);
        }

        [Fact(Skip = "Skip this test.")]
        public async Task SkipTest()
        {
            await Task.Delay(rnd.Next(1000, 3000));

            Assert.True(true);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public async Task TheoryTest(int value)
        {
            await Task.Delay(rnd.Next(1000, 3000));

            Assert.True(value > 0);
        }
    }
}
