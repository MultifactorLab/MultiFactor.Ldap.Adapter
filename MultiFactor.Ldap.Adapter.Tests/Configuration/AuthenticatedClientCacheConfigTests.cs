using System;
using MultiFactor.Ldap.Adapter.Configuration;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Configuration
{
    public class AuthenticatedClientCacheConfigTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("00:00:00")]
        public void Create_NoOrZeroLifetime_DisablesTheCache(string setting)
        {
            var config = AuthenticatedClientCacheConfig.Create(setting);

            Assert.Equal(TimeSpan.Zero, config.Lifetime);
            Assert.False(config.Enabled);
        }

        [Fact]
        public void Create_ValidLifetime_EnablesTheCache()
        {
            var config = AuthenticatedClientCacheConfig.Create("00:15:00");

            Assert.Equal(TimeSpan.FromMinutes(15), config.Lifetime);
            Assert.True(config.Enabled);
        }

        [Theory]
        [InlineData("garbage")]
        [InlineData("1:00:00")]
        [InlineData("00:15")]
        public void Create_MalformedLifetime_Throws(string setting)
        {
            Assert.Throws<FormatException>(() => AuthenticatedClientCacheConfig.Create(setting));
        }
    }
}
