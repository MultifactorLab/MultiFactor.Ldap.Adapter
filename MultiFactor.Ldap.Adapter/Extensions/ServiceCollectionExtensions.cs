using Microsoft.Extensions.DependencyInjection;
using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Core.NameResolving;
using MultiFactor.Ldap.Adapter.Server;
using MultiFactor.Ldap.Adapter.Services;
using MultiFactor.Ldap.Adapter.Services.Caching;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace MultiFactor.Ldap.Adapter.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Добавляет HttpClient с прокси, если это задано в настройках.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        public static void AddHttpClientWithProxy(this IServiceCollection services)
        {
            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger>();
            var conf = serviceProvider.GetService<ServiceConfiguration>();
            services.AddHttpContextAccessor();
            services.AddTransient<MfTraceIdHeaderSetter>();

            services.AddHttpClient(nameof(MultiFactorApiClient), client =>
            {
                client.Timeout = conf.ApiTimeout;
            })
            .ConfigurePrimaryHttpMessageHandler(prov =>
            {
                var handler = new HttpClientHandler();

                if (string.IsNullOrWhiteSpace(conf.ApiProxy)) return handler;
                logger.Debug("Using proxy " + conf.ApiProxy);
                if (!WebProxyFactory.TryCreateWebProxy(conf.ApiProxy, out var webProxy))
                {
                    throw new Exception("Unable to initialize WebProxy. Please, check whether multifactor-api-proxy URI is valid.");
                }
                handler.Proxy = webProxy;

                return handler;
            })
            .AddHttpMessageHandler<MfTraceIdHeaderSetter>();
        }

        public static void ConfigureApplicationServices(this IServiceCollection services, LoggingLevelSwitch levelSwitch, string syslogInfoMessage)
        {
            var configuration = ServiceConfiguration.Load(Log.Logger);
            SetLogLevel(configuration.LogLevel, levelSwitch);

            if (syslogInfoMessage != null)
            {
                Log.Logger.Information(syslogInfoMessage);
            }
            if (configuration.ServerConfig.AdapterLdapsEndpoint != null)
            {
                GetOrCreateTlsCertificate(Core.Constants.ApplicationPath, configuration, Log.Logger);
            }

            services.AddSingleton(configuration);
            services.AddSingleton(Log.Logger);
            services.AddSingleton<LdapProxyFactory>();
            services.AddSingleton<LdapServersFactory>();
            services.AddSingleton(prov => new RandomWaiter(prov.GetRequiredService<ServiceConfiguration>().InvalidCredentialDelay));
            services.AddSingleton(prov => prov.GetRequiredService<LdapServersFactory>().CreateServers());
            services.AddSingleton<AuthenticatedClientCache>();
            services.AddSingleton<MultiFactorApiClient>();
            services.AddHttpClientWithProxy();
            services.AddTransient<NameResolverService>();
            services.AddSingleton<AdapterService>();
        }

        private static void SetLogLevel(string level, LoggingLevelSwitch levelSwitch)
        {
            if (!Enum.TryParse<LogEventLevel>(level, out var logLevel))
            {
                logLevel = LogEventLevel.Information;
            }
            levelSwitch.MinimumLevel = logLevel;

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
                if (!string.IsNullOrEmpty(configuration.CertificatePassword))
                {
                    logger.Debug($"Loading certificate for TLS from {certPath} with CertificatePassword XXX");
                    configuration.X509Certificate = new X509Certificate2(certPath, configuration.CertificatePassword);
                }
                else
                {
                    logger.Debug($"Loading certificate for TLS from {certPath}");
                    configuration.X509Certificate = new X509Certificate2(certPath);
                }
            }
        }
    }
}
