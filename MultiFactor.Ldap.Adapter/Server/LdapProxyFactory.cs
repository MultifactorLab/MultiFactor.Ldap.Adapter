//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core.NameResolve;
using MultiFactor.Ldap.Adapter.Services;
using Serilog;
using System;
using System.IO;
using System.Net.Sockets;

namespace MultiFactor.Ldap.Adapter.Server
{
    public class LdapProxyFactory
    {
        private readonly RandomWaiter _waiter;
        private readonly MultiFactorApiClient _apiClient;
        private readonly ILogger _logger;
        private readonly NameResolverService _nameResolverService;
        public LdapProxyFactory(RandomWaiter waiter, MultiFactorApiClient apiClient, ILogger logger, NameResolverService nameResolverService)
        {
            _waiter = waiter ?? throw new ArgumentNullException(nameof(waiter));
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _nameResolverService = nameResolverService ?? throw new ArgumentNullException(nameof(_nameResolverService));
        }

        public LdapProxy CreateProxy(TcpClient clientConnection, Stream clientStream, 
            TcpClient serverConnection, Stream serverStream, 
            ClientConfiguration clientConfig)
        {
            return new LdapProxy(clientConnection, clientStream, 
                serverConnection, serverStream, 
                clientConfig,
                _waiter, _apiClient,
                _logger,
                _nameResolverService);
        }
    }
}