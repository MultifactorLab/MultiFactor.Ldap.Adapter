//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Configuration;
using Serilog;
using System;
using System.IO;
using System.Net.Sockets;

namespace MultiFactor.Ldap.Adapter.Server
{
    public class LdapProxyFactory
    {
        private readonly ServiceConfiguration _serviceConfiguration;
        private readonly ILogger _logger;

        public LdapProxyFactory(ServiceConfiguration serviceConfiguration, ILogger logger)
        {
            _serviceConfiguration = serviceConfiguration ?? throw new ArgumentNullException(nameof(serviceConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public LdapProxy CreateProxy(TcpClient clientConnection, Stream clientStream, 
            TcpClient serverConnection, Stream serverStream, 
            ClientConfiguration clientConfig)
        {
            return new LdapProxy(clientConnection, clientStream, 
                serverConnection, serverStream, 
                _serviceConfiguration, 
                clientConfig,
                _logger);
        }
    }
}