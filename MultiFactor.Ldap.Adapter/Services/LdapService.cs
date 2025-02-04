//Copyright(c) 2022 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core;
using MultiFactor.Ldap.Adapter.Core.NameResolving;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter.Services
{
    public class LdapService
    {
        private readonly RequestFactory _requestFactory = new RequestFactory();

        public async Task<string> GetDefaultNamingContext(Stream ldapConnectedStream)
        {
            var request = _requestFactory.CreateRootDSERequest();
            var requestData = request.GetBytes();

            await ldapConnectedStream.WriteAsync(requestData, 0, requestData.Length);

            string defaultNamingContext = null;

            LdapPacket packet;
            while ((packet = await LdapPacket.ParsePacket(ldapConnectedStream)) != null)
            {
                var searchResult = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.SearchResultEntry);
                if (searchResult != null)
                {
                    var attrs = searchResult.ChildAttributes[1];
                    var entry = GetEntry(attrs.ChildAttributes[0]);

                    defaultNamingContext = entry.Values.FirstOrDefault();
                }
            }

            return defaultNamingContext;
        }
        
        public async Task<string> GetBaseDn(Stream ldapConnectedStream, string userName)
        {
            string baseDn;

            if (IdentityTypeParser.Parse(userName) == IdentityType.DistinguishedName)
            {
                //if userName is distinguishedName, get basedn from it
                baseDn = LdapProfile.GetBaseDn(userName);
            }
            else
            {
                //else query defaultNamingContext from ldap
                baseDn = await GetDefaultNamingContext(ldapConnectedStream);
            }
            return baseDn;
        }

        public async Task<LdapProfile> LoadProfile(Stream ldapConnectedStream, string baseDn, string userName)
        {
            var request = _requestFactory.CreateLoadProfileRequest(userName, baseDn);
            var requestData = request.GetBytes();

            await ldapConnectedStream.WriteAsync(requestData, 0, requestData.Length);

            LdapProfile profile = null;
            LdapPacket packet;

            while ((packet = await LdapPacket.ParsePacket(ldapConnectedStream)) != null)
            {
                var searchResult = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.SearchResultEntry);
                if (searchResult != null)
                {
                    profile = profile ?? new LdapProfile();

                    var dn = searchResult.ChildAttributes[0].GetValue<string>();
                    var attrs = searchResult.ChildAttributes[1];

                    profile.Dn = dn;

                    foreach (var valueAttr in attrs.ChildAttributes)
                    {
                        var entry = GetEntry(valueAttr);

                        switch (entry.Name)
                        {
                            case "uid":
                                profile.Uid = entry.Values.FirstOrDefault();    //openldap, freeipa
                                break;
                            case "sAMAccountName":
                                profile.Uid = entry.Values.FirstOrDefault();    //ad
                                break;
                            case "displayName":
                                profile.DisplayName = entry.Values.FirstOrDefault();
                                break;
                            case "userPrincipalName":
                                profile.Upn = entry.Values.FirstOrDefault();
                                break;
                            case "email":
                            case "mail":
                                profile.Email = entry.Values.FirstOrDefault();
                                break;
                            case "memberOf":
                                profile.MemberOf.AddRange(entry.Values.Select(v => DnToCn(v)));
                                break;
                        }
                    }
                }
            }

            return profile;
        }

        public async Task<List<string>> GetAllGroups(Stream ldapConnectedStream, LdapProfile profile, ClientConfiguration clientConfiguration)
        {
            if (!clientConfiguration.LoadActiveDirectoryNestedGroups)
            {
                return profile.MemberOf;
            }

            var request = _requestFactory.CreateMemberOfRequest(profile.Dn);
            var requestData = request.GetBytes();
            await ldapConnectedStream.WriteAsync(requestData, 0, requestData.Length);

            var groups = new List<string>();

            LdapPacket packet;
            while ((packet = await LdapPacket.ParsePacket(ldapConnectedStream)) != null)
            {
                groups.AddRange(GetGroups(packet));
            }

            return groups;
        }


        public async Task<NetbiosDomainName[]> GetDomains(Stream serverStream, string baseDn)
        {
            var request = _requestFactory.CreateGetPartitions(baseDn);
            var buffer = request.GetBytes();
            await serverStream.WriteAsync(buffer, 0, buffer.Length);
            LdapPacket packet;
            var result = new List<NetbiosDomainName>();
            while ((packet = await LdapPacket.ParsePacket(serverStream)) != null)
            {
                var searchResult = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.SearchResultEntry);
                if (searchResult != null)
                {
                    var attrs = searchResult.ChildAttributes[1];
                    var domain = new NetbiosDomainName();
                    foreach (var valueAttr in attrs.ChildAttributes)
                    {
                        var entry = GetEntry(valueAttr);

                        if (entry.Name == "nETBIOSName")
                        {
                            domain.NetbiosName = entry.Values.First();
                        }

                        if (entry.Name == "dnsRoot")
                        {
                            domain.Domain = entry.Values.First();
                        }
                    }
                    result.Add(domain);
                }
            }

            return result.ToArray();
        }

        public async Task<LdapProfile> ResolveProfile(Stream serverStream, string name, string baseDn)
        {
            var request = _requestFactory.CreateResolveProfileRequest(baseDn, name);
            var buffer = request.GetBytes();
            await serverStream.WriteAsync(buffer, 0, buffer.Length);
            var result = new List<NetbiosDomainName>();

            LdapProfile profile = null;
            LdapPacket packet;

            while ((packet = await LdapPacket.ParsePacket(serverStream)) != null)
            {
                var searchResult = packet.ChildAttributes.SingleOrDefault(c => c.LdapOperation == LdapOperation.SearchResultEntry);
                if (searchResult != null)
                {
                    profile = profile ?? new LdapProfile();

                    var dn = searchResult.ChildAttributes[0].GetValue<string>();
                    var attrs = searchResult.ChildAttributes[1];

                    profile.Dn = dn;

                    foreach (var valueAttr in attrs.ChildAttributes)
                    {
                        var entry = GetEntry(valueAttr);

                        switch (entry.Name)
                        {
                            case "uid":
                                profile.Uid = entry.Values.FirstOrDefault();    //openldap, freeipa
                                break;
                            case "sAMAccountName":
                                profile.Uid = entry.Values.FirstOrDefault();    //ad
                                break;
                            case "userPrincipalName":
                                profile.Upn = entry.Values.FirstOrDefault();
                                break;
                        }
                    }
                }
            }

            return profile;
        }

        private IEnumerable<string> GetGroups(LdapPacket packet)
        {
            var groups = new List<string>();

            foreach (var searchResultEntry in packet.ChildAttributes.FindAll(attr => attr.LdapOperation == LdapOperation.SearchResultEntry))
            {
                if (searchResultEntry.ChildAttributes.Count > 0)
                {
                    var group = searchResultEntry.ChildAttributes[0].GetValue<string>();
                    groups.Add(DnToCn(group));
                }
            }

            return groups;
        }

        /// <summary>
        /// Extracts CN from DN
        /// </summary>
        private string DnToCn(string dn)
        {
            return dn.Split(',')[0].Split(new[] { '=' })[1];
        }

        private static LdapSearchResultEntry GetEntry(LdapAttribute ldapAttribute)
        {
            var name = ldapAttribute.ChildAttributes[0].GetValue<string>();
            var ret = new LdapSearchResultEntry { Name = name, Values = new List<string>() };

            if (ldapAttribute.ChildAttributes.Count > 1)
            {
                foreach (var valueAttribute in ldapAttribute.ChildAttributes[1].ChildAttributes)
                {
                    ret.Values.Add(valueAttribute.GetValue()?.ToString());
                }
            }

            return ret;
        }

        private class LdapSearchResultEntry
        {
            public string Name { get; set; }
            public IList<string> Values { get; set; }
        }
    }
}