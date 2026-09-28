using System;
using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Services;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Services
{
    public class RandomWaiterTests
    {
        [Fact]
        public void WaitSomeTimeAsync_ZeroDelayConfig_ReturnsAnAlreadyCompletedTask()
        {
            var waiter = new RandomWaiter(RandomWaiterConfig.Create("0"));

            Assert.True(waiter.WaitSomeTimeAsync().IsCompleted);
        }

        [Fact]
        public void WaitSomeTimeAsync_NonZeroDelay_DoesNotCompleteImmediately()
        {
            var waiter = new RandomWaiter(RandomWaiterConfig.Create("30-60"));

            Assert.False(waiter.WaitSomeTimeAsync().IsCompleted);
        }

        [Fact]
        public void WaitSomeTimeAsync_InvertedRange_Throws()
        {
            var waiter = new RandomWaiter(RandomWaiterConfig.Create("7-2"));

            Assert.Throws<ArgumentOutOfRangeException>(() => StartWithoutAwaiting(waiter));
        }

        private static void StartWithoutAwaiting(RandomWaiter waiter) => waiter.WaitSomeTimeAsync();
    }
}
