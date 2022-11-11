//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/blob/main/LICENSE.md

using System.Net;

namespace MultiFactor.Ldap.Adapter.Configuration
{
    public interface ILdapServerConfig
    {
        /// <summary>
        /// This service LDAP endpoint
        /// </summary>
        IPEndPoint AdapterLdapEndpoint { get; }

        /// <summary>
        /// This service LDAPS endpoint
        /// </summary>
        IPEndPoint AdapterLdapsEndpoint { get; }
    }
}