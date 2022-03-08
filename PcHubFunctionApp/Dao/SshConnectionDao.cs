using Microsoft.Extensions.Logging;
using PcHubFunctionApp.Helper;
using PcHubFunctionApp.Model;

namespace PcHubFunctionApp.Dao;

internal class SshConnectionDao : Dao<SshConnection>
{
    public SshConnectionDao(Config config, ILogger logger) : base(config, logger)
    {
    }
}