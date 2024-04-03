using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace MultiFactor.Ldap.Adapter.Core.NameResolving
{
    public class NameTypeDetector
    {
        public static LdapIdentityFormat? GetType(string name)
        {
            if (name.Contains('\\'))
            {
                return LdapIdentityFormat.NetBIOSAndUid;
            }
            if (name.IndexOf("CN=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return LdapIdentityFormat.DistinguishedName;
            }
            var domainRegex = new Regex("^[^@]+@(.+)$");
            var domainMatch = domainRegex.Match(name);
            if (!domainMatch.Success || domainMatch.Groups.Count < 2)
            {
                return LdapIdentityFormat.SamAccountName;
            }
            return domainMatch.Groups[1].Value.Count(x => x == '.') == 0 
                ? LdapIdentityFormat.UidAndNetbios
                : LdapIdentityFormat.Upn;
        } 
    }
}
