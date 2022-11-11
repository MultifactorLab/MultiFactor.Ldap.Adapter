using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Server;
using MultiFactor.Ldap.Adapter.Services;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace MultiFactor.Ldap.Adapter.Extensions
{
    public static class ServicesConfiguration
    {
        public static void ConfigureApplicationServices(this IServiceCollection services, LoggingLevelSwitch levelSwitch, string syslogInfoMessage)
        {
            var configuration = ServiceConfiguration.Load(Log.Logger);
            SetLogLevel(configuration.LogLevel, levelSwitch);

            if (syslogInfoMessage != null)
            {
                Log.Logger.Information(syslogInfoMessage);
            }
            if (configuration.ServerConfig.AdapterLdapEndpoint != null)
            {
                GetOrCreateTlsCertificate(Core.Constants.ApplicationPath, configuration, Log.Logger);
            }

            services.AddSingleton(configuration);
            services.AddSingleton(Log.Logger);
            services.AddSingleton<LdapProxyFactory>();
            services.AddSingleton<LdapServersFactory>();
            services.AddSingleton(prov => prov.GetRequiredService<LdapServersFactory>().CreateServers());

            services.AddSingleton<AdapterService>();
        }

        private static void SetLogLevel(string level, LoggingLevelSwitch levelSwitch)
        {
            switch (level)
            {
                case "Debug":
                    levelSwitch.MinimumLevel = LogEventLevel.Debug;
                    break;
                case "Info":
                    levelSwitch.MinimumLevel = LogEventLevel.Information;
                    break;
                case "Warn":
                    levelSwitch.MinimumLevel = LogEventLevel.Warning;
                    break;
                case "Error":
                    levelSwitch.MinimumLevel = LogEventLevel.Error;
                    break;
            }

            Log.Logger.Information($"Logging level: {levelSwitch.MinimumLevel}");
        }

        private static void GetOrCreateTlsCertificate(string path, ServiceConfiguration configuration, ILogger logger)
        {
            var certDirectory = $"{path}Tls";
            if (!Directory.Exists(certDirectory))
            {
                Directory.CreateDirectory(certDirectory);
            }

            var certPath = $"{certDirectory}{Path.DirectorySeparatorChar}certificate.pfx";
            if (!File.Exists(certPath))
            {
                var subj = Dns.GetHostEntry("").HostName;

                logger.Debug($"Generating self-signing certificate for TLS with subject CN={subj}");

                var certService = new CertificateService();
                var cert = certService.GenerateCertificate(subj);

                var data = cert.Export(X509ContentType.Pfx);
                File.WriteAllBytes(certPath, data);

                logger.Information($"Self-signed certificate with subject CN={subj} saved to {certPath}");

                configuration.X509Certificate = cert;
            }
            else
            {
                logger.Debug($"Loading certificate for TLS from {certPath}");
                configuration.X509Certificate = new X509Certificate2(certPath);

            }
        }
    }
}
