using System.Collections.Generic;
using System.Linq;
using Azure;
using Microsoft.Extensions.Logging;
using PcReservationFunctionApp.Helper;
using PcReservationFunctionApp.Model;

namespace PcReservationFunctionApp.Dao;

internal class SshConnectionDao : Dao<SshConnection>
{
    public SshConnectionDao(Config config, ILogger logger) : base(config, logger)
    {
    }




}