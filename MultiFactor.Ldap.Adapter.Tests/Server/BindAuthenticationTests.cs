using System.Linq;
using System.Text;
using MultiFactor.Ldap.Adapter.Core;
using MultiFactor.Ldap.Adapter.Server;
using MultiFactor.Ldap.Adapter.Server.Authentication;
using MultiFactor.Ldap.Adapter.Tests.TestDoubles;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Server
{
    public class BindAuthenticationTests
    {
        [Fact]
        public void IsNtlm_NtlmSignatureFollowedByAMessage_IsTrue()
        {
            var data = Encoding.ASCII.GetBytes("NTLMSSP\0").Concat(new byte[] { 1, 0, 0, 0 }).ToArray();

            Assert.True(BindAuthentication.IsNtlm(data));
        }

        [Theory]
        [InlineData("NTLMSSP")]
        [InlineData("SPNEGO\0\0")]
        [InlineData("")]
        public void IsNtlm_AnythingElse_IsFalse(string signature)
        {
            Assert.False(BindAuthentication.IsNtlm(Encoding.ASCII.GetBytes(signature)));
        }
    }

    public class SimpleBindAuthenticationTests
    {
        private static SimpleBindAuthentication Sut() => new SimpleBindAuthentication(SilentLogger.Instance);

        private static LdapAttribute BindRequest(string name)
        {
            var bindRequest = new LdapAttribute(LdapOperation.BindRequest);
            bindRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.Integer, 3));
            bindRequest.ChildAttributes.Add(new LdapAttribute(UniversalDataType.OctetString, name));
            bindRequest.ChildAttributes.Add(new LdapAttribute((byte)0, "secret"));
            return bindRequest;
        }

        [Theory]
        [InlineData("user@domain.local")]
        [InlineData("DOMAIN\\user")]
        [InlineData("CN=User,OU=Users,DC=domain,DC=local")]
        public void Parse_ReturnsTheBindDn(string name)
        {
            Assert.Equal(name, Sut().Parse(BindRequest(name)));
        }

        [Fact]
        public void Parse_MsNtlmMarker_ReturnsNull()
        {
            Assert.Null(Sut().Parse(BindRequest("NTLM")));
        }

        [Fact]
        public void TryParse_MalformedRequest_FailsWithoutThrowing()
        {
            var empty = new LdapAttribute(LdapOperation.BindRequest);

            Assert.False(Sut().TryParse(empty, out var userName));
            Assert.Null(userName);
        }
    }
}
