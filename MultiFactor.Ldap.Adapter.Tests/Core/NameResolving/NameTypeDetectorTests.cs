using MultiFactor.Ldap.Adapter.Core.NameResolving;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Core.NameResolving
{
    public class NameTypeDetectorTests
    {
        [Theory]
        [InlineData("DOMAIN\\user", LdapIdentityFormat.NetBIOSAndUid)]
        [InlineData("CN=User Name,OU=Users,DC=domain,DC=local", LdapIdentityFormat.DistinguishedName)]
        [InlineData("cn=user,dc=domain,dc=local", LdapIdentityFormat.DistinguishedName)]
        [InlineData("user@domain.local", LdapIdentityFormat.Upn)]
        [InlineData("user@sub.domain.local", LdapIdentityFormat.Upn)]
        [InlineData("user@DOMAIN", LdapIdentityFormat.UidAndNetbios)]
        [InlineData("user", LdapIdentityFormat.SamAccountName)]
        [InlineData("", LdapIdentityFormat.SamAccountName)]
        [InlineData("user@", LdapIdentityFormat.SamAccountName)]
        [InlineData("@domain.local", LdapIdentityFormat.SamAccountName)]
        public void GetType_DetectsTheIdentityFormat(string name, LdapIdentityFormat expected)
        {
            Assert.Equal(expected, NameTypeDetector.GetType(name));
        }

        [Theory]
        [InlineData("DOMAIN\\CN=user", LdapIdentityFormat.NetBIOSAndUid)]
        [InlineData("DOMAIN\\user@domain.local", LdapIdentityFormat.NetBIOSAndUid)]
        [InlineData("CN=user@domain.local", LdapIdentityFormat.DistinguishedName)]
        public void GetType_AmbiguousName_ResolvesByCheckOrder(string name, LdapIdentityFormat expected)
        {
            Assert.Equal(expected, NameTypeDetector.GetType(name));
        }
    }
}
