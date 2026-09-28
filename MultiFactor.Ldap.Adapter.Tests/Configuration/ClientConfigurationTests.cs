using MultiFactor.Ldap.Adapter.Configuration;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Configuration
{
    public class ClientConfigurationTests
    {
        [Fact]
        public void SplittedLdapServers_NotConfigured_IsEmpty()
        {
            Assert.Empty(new ClientConfiguration().SplittedLdapServers);
        }

        [Fact]
        public void SplittedLdapServers_SplitsOnSemicolonAndIgnoresEmptyEntries()
        {
            var config = new ClientConfiguration { LdapServer = ";;ldap://dc1.domain.local;ldaps://dc2.domain.local;;" };

            Assert.Equal(
                new[] { "ldap://dc1.domain.local", "ldaps://dc2.domain.local" },
                config.SplittedLdapServers);
        }

        [Fact]
        public void SplittedLdapServers_DeduplicatesIgnoringCase()
        {
            var config = new ClientConfiguration { LdapServer = "ldap://DC1.domain.local;ldap://dc1.DOMAIN.local" };

            Assert.Single(config.SplittedLdapServers);
        }
    }
}
