using Microsoft.Extensions.Logging;
using PcReservationFunctionApp.Helper;
using PcReservationFunctionApp.Model;

namespace PcReservationFunctionApp.Dao;

internal class SessionDao : Dao<Session>
{
    public SessionDao(Config config, ILogger logger) : base(config, logger)
    {
    }
}