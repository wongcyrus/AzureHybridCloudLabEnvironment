using Microsoft.Extensions.Logging;
using PcHubFunctionApp.Helper;
using PcHubFunctionApp.Model;

namespace PcHubFunctionApp.Dao
{
    internal class ComputerErrorLogDao: Dao<ComputerErrorLog>
    {
        public ComputerErrorLogDao(Config config, ILogger logger) : base(config, logger)
        {
        }
    }
}
