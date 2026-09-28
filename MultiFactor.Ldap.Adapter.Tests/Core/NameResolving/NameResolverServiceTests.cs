using MultiFactor.Ldap.Adapter.Core.NameResolving;
using MultiFactor.Ldap.Adapter.Core.NameResolving.NameTranslators;
using MultiFactor.Ldap.Adapter.Services;
using MultiFactor.Ldap.Adapter.Tests.TestDoubles;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Core.NameResolving
{
    public class NameResolverServiceTests
    {
        private static NameResolverService Sut() => new NameResolverService(SilentLogger.Instance);

        private static NameResolverContext Context(LdapProfile profile = null, params NetbiosDomainName[] domains)
        {
            return new NameResolverContext { Domains = domains, Profile = profile };
        }

        [Theory]
        [InlineData(LdapIdentityFormat.UidAndNetbios, typeof(sAMAccountNameAndNetbiosToUpnNameTranslator))]
        [InlineData(LdapIdentityFormat.NetBIOSAndUid, typeof(NetbiosToUpnNameTranslator))]
        [InlineData(LdapIdentityFormat.DistinguishedName, typeof(DistinguishedNameToUpnTranslator))]
        [InlineData(LdapIdentityFormat.SamAccountName, typeof(sAMAccountNameToUpnNameTranslator))]
        public void GetTranslator_ToUpn_ReturnsExpectedTranslator(LdapIdentityFormat from, System.Type expected)
        {
            Assert.IsType(expected, Sut().GetTranslator(Context(), from, LdapIdentityFormat.Upn));
        }

        [Fact]
        public void GetTranslator_UpnToUpn_WithProfile_UsesProfileTranslator()
        {
            var translator = Sut().GetTranslator(
                Context(new LdapProfile { Upn = "u@domain.local" }),
                LdapIdentityFormat.Upn,
                LdapIdentityFormat.Upn);

            Assert.IsType<UpnFromProfileNameTranslator>(translator);
        }

        [Fact]
        public void GetTranslator_UpnToUpn_WithoutProfile_ReturnsNull()
        {
            Assert.Null(Sut().GetTranslator(Context(), LdapIdentityFormat.Upn, LdapIdentityFormat.Upn));
        }

        [Theory]
        [InlineData("DOMAIN\\user")]
        [InlineData("user@DOMAIN")]
        public void Resolve_NetbiosForms_ToUpn(string name)
        {
            var ctx = Context(null, new NetbiosDomainName { NetbiosName = "DOMAIN", Domain = "domain.local" });

            Assert.Equal("user@domain.local", Sut().Resolve(ctx, name, LdapIdentityFormat.Upn));
        }

        [Fact]
        public void Resolve_NoSuitableTranslator_ReturnsNameUnchanged()
        {
            Assert.Equal("user@domain.local",
                Sut().Resolve(Context(), "user@domain.local", LdapIdentityFormat.Upn));
        }

        [Fact]
        public void Resolve_AlreadyUpn_WithProfile_PrefersProfileUpn()
        {
            var ctx = Context(new LdapProfile { Upn = "real.upn@domain.local" });

            Assert.Equal("real.upn@domain.local",
                Sut().Resolve(ctx, "samaccount@domain.local", LdapIdentityFormat.Upn));
        }
    }
}
