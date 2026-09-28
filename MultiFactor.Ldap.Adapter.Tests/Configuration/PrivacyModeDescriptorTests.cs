using System;
using MultiFactor.Ldap.Adapter.Configuration;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Configuration
{
    public class PrivacyModeDescriptorTests
    {
        [Theory]
        [InlineData(null, PrivacyMode.None)]
        [InlineData("", PrivacyMode.None)]
        [InlineData("none", PrivacyMode.None)]
        [InlineData("Full", PrivacyMode.Full)]
        [InlineData("FULL", PrivacyMode.Full)]
        [InlineData("Partial", PrivacyMode.Partial)]
        [InlineData("Partial:Email", PrivacyMode.Partial)]
        public void Create_ParsesTheMode(string value, PrivacyMode expected)
        {
            Assert.Equal(expected, PrivacyModeDescriptor.Create(value).Mode);
        }

        [Fact]
        public void Create_PartialWithFields_ExposesThemCaseInsensitively()
        {
            var descriptor = PrivacyModeDescriptor.Create("Partial:Email,Name");

            Assert.True(descriptor.HasField("Email"));
            Assert.True(descriptor.HasField("name"));
            Assert.False(descriptor.HasField("Phone"));
        }

        [Theory]
        [InlineData("Partial")]
        [InlineData("Partial:")]
        [InlineData("Full:Email")]
        public void Create_NoUsableFieldList_HasNoFields(string value)
        {
            Assert.False(PrivacyModeDescriptor.Create(value).HasField("Email"));
        }

        [Fact]
        public void Create_UnknownMode_Throws()
        {
            Assert.Throws<Exception>(() => PrivacyModeDescriptor.Create("Unknown"));
        }
    }
}
