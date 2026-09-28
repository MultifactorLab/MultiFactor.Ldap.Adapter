using System;
using System.Configuration;
using System.Net;
using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Configuration
{
    public class LdapServerConfigTests
    {
        private static AppSettingsSection Settings(string ldap = null, string ldaps = null)
        {
            var section = new AppSettingsSection();
            if (ldap != null)
            {
                section.Settings.Add(new KeyValueConfigurationElement(Constants.Configuration.AdapterLdapEndpoint, ldap));
            }
            if (ldaps != null)
            {
                section.Settings.Add(new KeyValueConfigurationElement(Constants.Configuration.AdapterLdapsEndpoint, ldaps));
            }
            return section;
        }

        [Fact]
        public void Parse_NullSettings_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => LdapServerConfig.Parse(null));
        }

        [Fact]
        public void Parse_NoEndpoints_IsEmpty()
        {
            var config = LdapServerConfig.Parse(Settings(ldap: ""));

            Assert.True(config.IsEmpty);
            Assert.Null(config.AdapterLdapEndpoint);
            Assert.Null(config.AdapterLdapsEndpoint);
        }

        [Fact]
        public void Parse_BothEndpoints_AreParsed()
        {
            var config = LdapServerConfig.Parse(Settings(ldap: "0.0.0.0:389", ldaps: "127.0.0.1:636"));

            Assert.False(config.IsEmpty);
            Assert.Equal(new IPEndPoint(IPAddress.Any, 389), config.AdapterLdapEndpoint);
            Assert.Equal(new IPEndPoint(IPAddress.Loopback, 636), config.AdapterLdapsEndpoint);
        }

        [Fact]
        public void Parse_MalformedEndpoint_Throws()
        {
            Assert.ThrowsAny<Exception>(() => LdapServerConfig.Parse(Settings(ldap: "not-an-address:389")));
        }
    }
}
