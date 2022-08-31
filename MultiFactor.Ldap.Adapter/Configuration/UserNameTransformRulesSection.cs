//Copyright(c) 2022 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/blob/main/LICENSE.md

using System.Configuration;

namespace MultiFactor.Ldap.Adapter.Configuration
{
    public class UserNameTransformRulesSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public UserNameTransformRulesCollection Members
        {
            get { return (UserNameTransformRulesCollection)base[""]; }
        }
    }
}
