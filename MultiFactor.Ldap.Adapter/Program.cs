//Copyright(c) 2021 MultiFactor
//Please see licence at 
//https://github.com/MultifactorLab/MultiFactor.Ldap.Adapter/blob/main/LICENSE.md

using MultiFactor.Ldap.Adapter.Core;
using MultiFactor.Ldap.Adapter.Services;
using MultiFactor.Ldap.Adapter.Syslog;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Syslog;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace MultiFactor.Ldap.Adapter
{
    static class Program
    {
        /// <summary>
        /// Main entry point
        /// </summary>
        static void Main(string[] args)
        {
            var path = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + Path.DirectorySeparatorChar;

            //create logging
            var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch);

            var formatter = GetLogFormatter();
            if (formatter != null)
            {
                loggerConfiguration
                    .WriteTo.Console(formatter)
                    .WriteTo.File(formatter, $"{path}Logs{Path.DirectorySeparatorChar}log-.txt", rollingInterval: RollingInterval.Day);
            }
            else
            {
                loggerConfiguration
                    .WriteTo.Console()
                    .WriteTo.File($"{path}Logs{Path.DirectorySeparatorChar}log-.txt", rollingInterval: RollingInterval.Day);
            }

            ConfigureSyslog(loggerConfiguration, out var syslogInfoMessage);

            Log.Logger = loggerConfiguration.CreateLogger();

            if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "/i":
                        InstallService();
                        return;
                    case "/u":
                        UnInstallService();
                        return;
                    default:
                        Log.Logger.Warning($"Unknown command line argument: {args[0]}");
                        return;
                }
            }

            try
            {
                //init configuration
                var configuration = Configuration.Load();

                SetLogLevel(configuration.LogLevel, levelSwitch);
                if (syslogInfoMessage != null)
                {
                    Log.Logger.Information(syslogInfoMessage);
                }

                if (configuration.StartLdapsServer)
                {
                    GetOrCreateTlsCertificate(path, configuration, Log.Logger);
                }

                var adapterService = new AdapterService(configuration, Log.Logger);

                if (Environment.UserInteractive)
                {
                    //start as console
                    Log.Logger.Information("Console mode");
                    Log.Logger.Information("Press CTRL+C to exit");

                    Console.OutputEncoding = Encoding.UTF8;

                    Serilog.Debugging.SelfLog.Enable(Console.Error);

                    var cts = new CancellationTokenSource();

                    Console.CancelKeyPress += (sender, eventArgs) =>
                    {
                        adapterService.StopServer();
                        eventArgs.Cancel = true;
                        cts.Cancel();
                    };
                    
                    adapterService.StartServer();

                    cts.Token.WaitHandle.WaitOne();
                }
                else
                {
                    //start as service
                    Log.Logger.Information("Service mode");
                    ServiceBase[] ServicesToRun;
                    ServicesToRun = new ServiceBase[]
                    {
                        adapterService
                    };
                    ServiceBase.Run(ServicesToRun);
                }
            }
            catch(Exception ex)
            {
                Log.Logger.Error($"Unable to start: {ex.Message}");
            }
        }

        private static void InstallService()
        {
            Log.Logger.Information($"Installing service {Configuration.ServiceUnitName}");
            System.Configuration.Install.ManagedInstallerClass.InstallHelper(new string[] { "/i", Assembly.GetExecutingAssembly().Location });
            Log.Logger.Information("Service installed");
            Log.Logger.Information($"Use 'net start {Configuration.ServiceUnitName}' to run");
            Log.Logger.Information("Press any key to exit");
            Console.ReadKey();
        }

        public static void UnInstallService()
        {
            Log.Logger.Information($"UnInstalling service {Configuration.ServiceUnitName}");
            System.Configuration.Install.ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
            Log.Logger.Information("Service uninstalled");
            Log.Logger.Information("Press any key to exit");
            Console.ReadKey();
        }

        private static void GetOrCreateTlsCertificate(string path, Configuration configuration, ILogger logger)
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

        private static void ConfigureSyslog(LoggerConfiguration loggerConfiguration, out string logMessage)
        {
            logMessage = null;

            var appSettings = ConfigurationManager.AppSettings;
            var sysLogServer = appSettings["syslog-server"];
            var sysLogFormatSetting = appSettings["syslog-format"];
            var sysLogFramerSetting = appSettings["syslog-framer"];
            var sysLogFacilitySetting = appSettings["syslog-facility"];
            var sysLogAppName = appSettings["syslog-app-name"] ?? "multifactor-ldap";

            var isJson = Configuration.GetLogFormat() == "json";

            var facility = ParseSettingOrDefault(sysLogFacilitySetting, Facility.Auth);
            var format = ParseSettingOrDefault(sysLogFormatSetting, SyslogFormat.RFC5424);
            var framer = ParseSettingOrDefault(sysLogFramerSetting, FramingType.OCTET_COUNTING);

            if (sysLogServer != null)
            {
                var uri = new Uri(sysLogServer);

                if (uri.Port == -1)
                {
                    throw new ConfigurationErrorsException($"Invalid port number for syslog-server {sysLogServer}");
                }

                switch (uri.Scheme)
                {
                    case "udp":
                        var serverIp = ResolveIP(uri.Host);
                        loggerConfiguration
                            .WriteTo
                            .JsonUdpSyslog(serverIp, port: uri.Port, appName: sysLogAppName, format: format, facility: facility, json: isJson);
                        logMessage = $"Using syslog server: {sysLogServer}, format: {format}, facility: {facility}, appName: {sysLogAppName}";
                        break;
                    case "tcp":
                        loggerConfiguration
                            .WriteTo
                            .JsonTcpSyslog(uri.Host, uri.Port, appName: sysLogAppName, format: format, framingType: framer, facility: facility, json: isJson);
                        logMessage = $"Using syslog server {sysLogServer}, format: {format}, framing: {framer}, facility: {facility}, appName: {sysLogAppName}";
                        break;
                    default:
                        throw new NotImplementedException($"Unknown scheme {uri.Scheme} for syslog-server {sysLogServer}. Expected udp or tcp");
                }
            }
        }

        private static TEnum ParseSettingOrDefault<TEnum>(string setting, TEnum defaultValue) where TEnum : struct
        {
            if (Enum.TryParse<TEnum>(setting, out var val))
            {
                return val;
            }

            return defaultValue;
        }

        private static string ResolveIP(string host)
        {
            if (!IPAddress.TryParse(host, out var addr))
            {
                addr = Dns.GetHostAddresses(host)
                    .First(x => x.AddressFamily == AddressFamily.InterNetwork); //only ipv4

                return addr.ToString();
            }

            return host;
        }

        private static ITextFormatter GetLogFormatter()
        {
            var format = Configuration.GetLogFormat();
            switch (format?.ToLower())
            {
                case "json":
                    return new RenderedCompactJsonFormatter();
                default:
                    return null;
            }
        }
    }
}