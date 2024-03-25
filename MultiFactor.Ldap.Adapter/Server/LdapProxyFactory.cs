//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Services;
using MultiFactor.Ldap.Adapter.Services.SecondFactor;
using Serilog;
using System;
using System.IO;
using System.Net.Sockets;

namespace MultiFactor.Ldap.Adapter.Server
{
    public class LdapProxyFactory
    {
        private readonly RandomWaiter _waiter;
        private readonly SecondFactorVerifier _secondFactorVerifier;
        private readonly ILogger _logger;

        public LdapProxyFactory(RandomWaiter waiter, SecondFactorVerifier secondFactorVerifier, ILogger logger)
        {
            _waiter = waiter ?? throw new ArgumentNullException(nameof(waiter));
            _secondFactorVerifier = secondFactorVerifier ?? throw new ArgumentNullException(nameof(secondFactorVerifier));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public LdapProxy CreateProxy(TcpClient clientConnection, Stream clientStream, 
            TcpClient serverConnection, Stream serverStream, 
            ClientConfiguration clientConfig)
        {
            return new LdapProxy(clientConnection, clientStream, 
                serverConnection, serverStream, 
                clientConfig,
                _waiter,
                _secondFactorVerifier,
                _logger);
        }
    }
}