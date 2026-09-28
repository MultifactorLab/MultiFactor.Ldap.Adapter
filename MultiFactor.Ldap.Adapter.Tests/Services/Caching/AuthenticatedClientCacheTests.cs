using System;
using System.Threading.Tasks;
using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Services.Caching;
using MultiFactor.Ldap.Adapter.Tests.TestDoubles;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Services.Caching
{
    public class AuthenticatedClientCacheTests
    {
        private static string Unique(string prefix) => prefix + "-" + Guid.NewGuid().ToString("N");

        private static ClientConfiguration Client(string lifetime)
        {
            return new ClientConfiguration
            {
                Name = Unique("client"),
                AuthenticationCacheLifetime = AuthenticatedClientCacheConfig.Create(lifetime)
            };
        }

        private static AuthenticatedClientCache Sut() => new AuthenticatedClientCache(SilentLogger.Instance);

        [Fact]
        public void TryHitCache_CachingDisabled_IsAlwaysAMiss()
        {
            var config = Client(null);
            var user = Unique("user");
            var cache = Sut();

            cache.SetCache(user, config);

            Assert.False(cache.TryHitCache(user, config));
        }

        [Fact]
        public void TryHitCache_AfterSetCache_IsAHit()
        {
            var config = Client("00:15:00");
            var user = Unique("user");

            Sut().SetCache(user, config);

            Assert.True(Sut().TryHitCache(user, config));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void BlankUserName_IsNeverCachedAndNeverHits(string user)
        {
            var config = Client("00:15:00");
            var cache = Sut();

            cache.SetCache(user, config);

            Assert.False(cache.TryHitCache(user, config));
        }

        [Fact]
        public void TryHitCache_SameUserDifferentClient_IsAMiss()
        {
            var user = Unique("user");
            var cache = Sut();

            cache.SetCache(user, Client("00:15:00"));

            Assert.False(cache.TryHitCache(user, Client("00:15:00")));
        }

        [Fact]
        public async Task TryHitCache_AfterLifetimeElapsed_IsAMiss()
        {
            var config = Client("00:00:01");
            var user = Unique("user");
            var cache = Sut();

            cache.SetCache(user, config);
            Assert.True(cache.TryHitCache(user, config));

            await Task.Delay(TimeSpan.FromMilliseconds(1300));

            Assert.False(cache.TryHitCache(user, config));
        }
    }
}
