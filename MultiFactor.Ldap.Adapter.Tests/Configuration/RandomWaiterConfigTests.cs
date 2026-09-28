using System;
using MultiFactor.Ldap.Adapter.Configuration;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Configuration
{
    public class RandomWaiterConfigTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("0")]
        [InlineData("0-0")]
        public void Create_NoOrZeroSetting_MeansNoDelay(string setting)
        {
            var config = RandomWaiterConfig.Create(setting);

            Assert.Equal(0, config.Min);
            Assert.Equal(0, config.Max);
            Assert.True(config.ZeroDelay);
        }

        [Theory]
        [InlineData("5", 5, 5)]
        [InlineData("2-7", 2, 7)]
        public void Create_ParsesBounds(string setting, int min, int max)
        {
            var config = RandomWaiterConfig.Create(setting);

            Assert.Equal(min, config.Min);
            Assert.Equal(max, config.Max);
            Assert.False(config.ZeroDelay);
        }

        [Theory]
        [InlineData("-1")]
        [InlineData("abc")]
        [InlineData("1-2-3")]
        [InlineData("1-b")]
        public void Create_InvalidSetting_Throws(string setting)
        {
            Assert.Throws<ArgumentException>(() => RandomWaiterConfig.Create(setting));
        }

        [Fact]
        public void Create_InvertedRange_IsAcceptedAsIs()
        {
            var config = RandomWaiterConfig.Create("7-2");

            Assert.Equal(7, config.Min);
            Assert.Equal(2, config.Max);
        }
    }
}
