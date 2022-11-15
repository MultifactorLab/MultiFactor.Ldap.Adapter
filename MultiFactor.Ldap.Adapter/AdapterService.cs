//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Server;
using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace MultiFactor.Ldap.Adapter
{
    public partial class AdapterService : ServiceBase
    {
        private readonly IReadOnlyList<LdapServer> _ldapServers;

        public AdapterService(IReadOnlyList<LdapServer> ldapServers)
        {
            _ldapServers = ldapServers ?? throw new ArgumentNullException(nameof(ldapServers));
            InitializeComponent();
        }

        public void StartServer()
        {
            foreach (var server in _ldapServers)
            {
                server.Start();
            }
        }

        public void StopServer()
        {
            foreach (var server in _ldapServers)
            {
                server.Stop();
            }
            
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
