using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Services;
using Xunit;

namespace MultiFactor.Ldap.Adapter.Tests.Services
{
    public class PersonalDataTests
    {
        private static LdapProfile Profile() => new LdapProfile { Email = "user@domain.local" };

        [Theory]
        [InlineData("None")]
        [InlineData("Partial:Email")]
        [InlineData("Partial:email")]
        public void EmailIsSentWhenTheModeAllowsIt(string privacyMode)
        {
            var data = new PersonalData(Profile(), PrivacyModeDescriptor.Create(privacyMode));

            Assert.Equal("user@domain.local", data.Email);
        }

        [Theory]
        [InlineData("Full")]
        [InlineData("Partial")]
        [InlineData("Partial:Name")]
        public void EmailIsStrippedWhenTheModeForbidsIt(string privacyMode)
        {
            var data = new PersonalData(Profile(), PrivacyModeDescriptor.Create(privacyMode));

            Assert.Null(data.Email);
        }
    }
}
