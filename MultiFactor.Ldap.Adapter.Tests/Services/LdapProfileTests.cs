using MultiFactor.Ldap.Adapter.Services;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Services
{
    public class LdapProfileTests
    {
        [Theory]
        [InlineData("CN=User Name,OU=Users,DC=domain,DC=local", "DC=domain,DC=local")]
        [InlineData("cn=user,ou=users,dc=sub,dc=domain,dc=local", "dc=sub,dc=domain,dc=local")]
        [InlineData("CN=user,OU=users", "")]
        public void GetBaseDn_KeepsOnlyDomainComponents(string dn, string expected)
        {
            Assert.Equal(expected, LdapProfile.GetBaseDn(dn));
        }

        [Fact]
        public void BaseDn_IsDerivedFromDn()
        {
            var profile = new LdapProfile { Dn = "CN=User,OU=Users,DC=domain,DC=local" };

            Assert.Equal("DC=domain,DC=local", profile.BaseDn);
        }
    }
}
