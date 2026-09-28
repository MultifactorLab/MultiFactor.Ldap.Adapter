using System;
using System.Threading;
using MultiFactor.Ldap.Adapter.Configuration;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Configuration
{
    public class HttpClientTimeoutTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Parse_NoSetting_UsesRecommendedWithoutWarning(string raw)
        {
            var timeout = HttpClientTimeout.Parse(raw);

            Assert.Equal(HttpClientTimeout.Recommended, timeout.Value);
            Assert.Null(timeout.Warning);
        }

        [Theory]
        [InlineData("00:01:05")]
        [InlineData("00:02:00")]
        public void Parse_AtOrAboveRecommended_IsAcceptedWithoutWarning(string raw)
        {
            var timeout = HttpClientTimeout.Parse(raw);

            Assert.Equal(TimeSpan.ParseExact(raw, @"hh\:mm\:ss", null), timeout.Value);
            Assert.Null(timeout.Warning);
        }

        [Fact]
        public void Parse_Zero_MeansInfinite()
        {
            Assert.Equal(Timeout.InfiniteTimeSpan, HttpClientTimeout.Parse("00:00:00").Value);
        }

        [Fact]
        public void Parse_BelowRecommended_IsIgnoredAndWarns()
        {
            var timeout = HttpClientTimeout.Parse("00:00:30");

            Assert.Equal(HttpClientTimeout.Recommended, timeout.Value);
            Assert.Contains("force", timeout.Warning);
        }

        [Fact]
        public void Parse_BelowRecommendedButForced_IsHonouredAndStillWarns()
        {
            var timeout = HttpClientTimeout.Parse("00:00:30!");

            Assert.Equal(TimeSpan.FromSeconds(30), timeout.Value);
            Assert.NotNull(timeout.Warning);
        }

        [Fact]
        public void Parse_Malformed_FallsBackToRecommendedAndWarns()
        {
            var timeout = HttpClientTimeout.Parse("garbage");

            Assert.Equal(HttpClientTimeout.Recommended, timeout.Value);
            Assert.Contains("Can't parse", timeout.Warning);
        }
    }
}
