using Microsoft.Extensions.Logging;
using PcHubFunctionApp.Helper;
using PcHubFunctionApp.Model;

namespace PcHubFunctionApp.Dao;

internal class SessionDao : Dao<Session>
{
    public SessionDao(Config config, ILogger logger) : base(config, logger)
    {
    }
}