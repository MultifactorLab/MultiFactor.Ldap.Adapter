//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Server;
using Serilog;
using System;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter
{
    public partial class AdapterService : ServiceBase
    {
        private LdapServer _ldapServer;
        private LdapsServer _ldapsServer;
        private Configuration _configuration;

        public AdapterService(Configuration configuration, ILogger logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            if (_configuration.StartLdapServer)
            {
                _ldapServer = new LdapServer(_configuration.AdapterLdapEndpoint, _configuration, logger);
            }
            if (_configuration.StartLdapsServer)
            {
                _ldapsServer = new LdapsServer(_configuration.AdapterLdapsEndpoint, _configuration, logger);
            }

            InitializeComponent();
        }

        public void StartServer()
        {
            if (_configuration.StartLdapServer)
            {
                _ldapServer.Start();
            }

            if (_configuration.StartLdapsServer)
            {
                _ldapsServer.Start();
            }
        }

        public void StopServer()
        {
            _ldapServer?.Stop();
            _ldapsServer?.Stop();
            
            //2 sec delay to flush logs
            Task.WaitAny(Task.Delay(TimeSpan.FromSeconds(2)));
        }

        protected override void OnStart(string[] args)
        {
            StartServer();
        }

        protected override void OnStop()
        {
            StopServer();
        }
    }
}
