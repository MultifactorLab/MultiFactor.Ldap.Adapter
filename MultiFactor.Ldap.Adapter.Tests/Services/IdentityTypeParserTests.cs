using MultiFactor.Ldap.Adapter.Services;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Services
{
    public class IdentityTypeParserTests
    {
        [Theory]
        [InlineData("user@domain.local", IdentityType.UserPrincipalName)]
        [InlineData("CN=User Name,OU=Users,DC=domain,DC=local", IdentityType.DistinguishedName)]
        [InlineData("cn=user,dc=domain,dc=local", IdentityType.DistinguishedName)]
        [InlineData("user", IdentityType.sAMAccountName)]
        [InlineData("DOMAIN\\user", IdentityType.sAMAccountName)]
        [InlineData("CN=user@domain.local,DC=domain,DC=local", IdentityType.UserPrincipalName)]
        public void Parse_DetectsTheIdentityType(string userName, IdentityType expected)
        {
            Assert.Equal(expected, IdentityTypeParser.Parse(userName));
        }
    }
}
