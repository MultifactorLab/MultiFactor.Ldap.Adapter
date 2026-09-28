using MultiFactor.Ldap.Adapter.Core.NameResolving;
using MultiFactor.Ldap.Adapter.Core.NameResolving.NameTranslators;
using MultiFactor.Ldap.Adapter.Services;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Core.NameResolving
{
    public class NameTranslatorsTests
    {
        private static NameResolverContext Context(LdapProfile profile, params NetbiosDomainName[] domains)
        {
            return new NameResolverContext { Domains = domains, Profile = profile };
        }

        private static NetbiosDomainName Domain(string netbios, string domain)
        {
            return new NetbiosDomainName { NetbiosName = netbios, Domain = domain };
        }

        [Theory]
        [InlineData("DOMAIN\\user")]
        [InlineData("domain\\user")]
        [InlineData("DoMaIn\\user")]
        public void NetbiosToUpn_ReplacesNetbiosPrefixWithDomainSuffix(string name)
        {
            var ctx = Context(null, Domain("DOMAIN", "Domain.Local"));

            Assert.Equal("user@domain.local", new NetbiosToUpnNameTranslator().Translate(ctx, name));
        }

        [Fact]
        public void NetbiosToUpn_PicksTheMatchingDomainAmongSeveral()
        {
            var ctx = Context(null, Domain("FIRST", "first.local"), Domain("SECOND", "second.local"));

            Assert.Equal("user@second.local",
                new NetbiosToUpnNameTranslator().Translate(ctx, "SECOND\\user"));
        }

        [Fact]
        public void NetbiosToUpn_UnknownNetbiosName_ReturnsInputUnchanged()
        {
            var ctx = Context(null, Domain("DOMAIN", "domain.local"));

            Assert.Equal("OTHER\\user", new NetbiosToUpnNameTranslator().Translate(ctx, "OTHER\\user"));
        }

        [Theory]
        [InlineData("user@DOMAIN")]
        [InlineData("user@domain")]
        public void SamAccountNameAndNetbiosToUpn_ReplacesNetbiosSuffixWithDomain(string name)
        {
            var ctx = Context(null, Domain("DoMaIn", "Domain.Local"));

            Assert.Equal("user@domain.local",
                new sAMAccountNameAndNetbiosToUpnNameTranslator().Translate(ctx, name));
        }

        [Fact]
        public void SamAccountNameAndNetbiosToUpn_UnknownNetbiosName_ReturnsInputUnchanged()
        {
            var ctx = Context(null, Domain("DOMAIN", "domain.local"));

            Assert.Equal("user@OTHER",
                new sAMAccountNameAndNetbiosToUpnNameTranslator().Translate(ctx, "user@OTHER"));
        }

        [Fact]
        public void Translators_ProfileWins_OverTheDomainList()
        {
            var ctx = Context(new LdapProfile { Upn = "from.profile@domain.local" }, Domain("DOMAIN", "domain.local"));

            Assert.Equal("from.profile@domain.local",
                new NetbiosToUpnNameTranslator().Translate(ctx, "DOMAIN\\user"));
            Assert.Equal("from.profile@domain.local",
                new sAMAccountNameAndNetbiosToUpnNameTranslator().Translate(ctx, "user@DOMAIN"));
            Assert.Equal("from.profile@domain.local",
                new DistinguishedNameToUpnTranslator().Translate(ctx, "CN=user,DC=domain,DC=local"));
        }
    }
}
