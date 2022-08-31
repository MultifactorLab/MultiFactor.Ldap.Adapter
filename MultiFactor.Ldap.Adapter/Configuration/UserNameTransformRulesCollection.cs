//Copyright(c) 2022 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/blob/main/LICENSE.md

using System.Configuration;

namespace MultiFactor.Ldap.Adapter.Configuration
{
    public class UserNameTransformRulesCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new UserNameTransformRulesElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            var attribute = (UserNameTransformRulesElement)element;
            return $"{attribute.Match}:{attribute.Replace}";
        }
    }
}
