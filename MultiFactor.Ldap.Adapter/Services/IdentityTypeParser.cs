//Copyright(c) 2022 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/blob/main/LICENSE.md

using System;

namespace MultiFactor.Ldap.Adapter.Services
{
    public static class IdentityTypeParser
    {
        public static IdentityType Parse(string userName)
        {         
            if (userName.Contains("@")) return IdentityType.UserPrincipalName;
            if (userName.IndexOf("CN=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return IdentityType.DistinguishedName;
            }

            return IdentityType.sAMAccountName;       
        }
    }
}
