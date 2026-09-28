using Serilog;
using Serilog.Core;

namespace MultiFactor.Ldap.Adapter.Tests.TestDoubles
{
    internal static class SilentLogger
    {
        public static ILogger Instance { get; } = Logger.None;
    }
}
