using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Formatting;
using System.IO;
using MultiFactor.Ldap.Adapter.Configuration;
using MultiFactor.Ldap.Adapter.Syslog;
using Serilog.Sinks.Syslog;
using System.Configuration;
using System;
using System.Net;
using System.Linq;
using System.Net.Sockets;

namespace MultiFactor.Ldap.Adapter.Extensions
{
    public static class LoggingConfiguration
    {
        public static LoggerConfiguration ConfigureConsoleLogging(this LoggerConfiguration loggerConfiguration)
        {
            var formatter = GetLogFormatter();
            if (formatter != null)
            {
                loggerConfiguration.WriteTo.Console(formatter);
            }
            else
            {
                loggerConfiguration.WriteTo.Console();
            }

            return loggerConfiguration;
        }

        public static LoggerConfiguration ConfigureFileLogging(this LoggerConfiguration loggerConfiguration)
        {
            var formatter = GetLogFormatter();
            if (formatter != null)
            {
                loggerConfiguration
                    .WriteTo.File(formatter, $"{Core.Constants.ApplicationPath}Logs{Path.DirectorySeparatorChar}log-.txt", rollingInterval: RollingInterval.Day);
            }
            else
            {
                loggerConfiguration
                    .WriteTo.File($"{Core.Constants.ApplicationPath}Logs{Path.DirectorySeparatorChar}log-.txt", rollingInterval: RollingInterval.Day);
            }

            return loggerConfiguration;
        }

        public static LoggerConfiguration ConfigureSyslogLogging(this LoggerConfiguration loggerConfiguration, out string logMessage)
        {
            logMessage = null;

            var appSettings = ConfigurationManager.AppSettings;
            var sysLogServer = appSettings["syslog-server"];
            var sysLogFormatSetting = appSettings["syslog-format"];
            var sysLogFramerSetting = appSettings["syslog-framer"];
            var sysLogFacilitySetting = appSettings["syslog-facility"];
            var sysLogAppName = appSettings["syslog-app-name"] ?? "multifactor-ldap";

            var isJson = ServiceConfiguration.GetLogFormat() == "json";

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

            return loggerConfiguration;
        }

        private static ITextFormatter GetLogFormatter()
        {
            var format = ServiceConfiguration.GetLogFormat();
            switch (format?.ToLower())
            {
                case "json":
                    return new RenderedCompactJsonFormatter();
                default:
                    return null;
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

    }
}
