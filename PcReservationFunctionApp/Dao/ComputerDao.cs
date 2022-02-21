using Microsoft.Extensions.Logging;
using PcReservationFunctionApp.Helper;
using PcReservationFunctionApp.Model;

namespace PcReservationFunctionApp.Dao;

internal class ComputerDao : Dao<Computer>
{
    public ComputerDao(Config config, ILogger logger) : base(config, logger)
    {
    }
}