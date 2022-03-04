using System.Collections.Generic;
using System.Linq;
using Azure;
using Microsoft.Extensions.Logging;
using PcReservationFunctionApp.Helper;
using PcReservationFunctionApp.Model;

namespace PcReservationFunctionApp.Dao;

internal class ComputerDao : Dao<Computer>
{
    public ComputerDao(Config config, ILogger logger) : base(config, logger)
    {
    }

    public List<Computer> GetFreeComputer(string location)
    {
        var oDataQueryEntities =
            TableClient.Query<Computer>($"PartitionKey eq '{location}' and IsOnline eq true and IsReserved eq false");
        return oDataQueryEntities.ToList();
    }

    public Computer GetComputer(string location, string email)
    {
        var oDataQueryEntities = TableClient.Query<Computer>($"PartitionKey eq '{location}' and Email eq '{email}'");
        return oDataQueryEntities.Any() ? oDataQueryEntities.First() : null;
    }

    public bool UpdateReservation(Computer computer, string email)
    {
        try
        {
            computer.IsReserved = !string.IsNullOrEmpty(email);
            computer.Email = email;
            TableClient.UpdateEntity(computer, computer.ETag);
            return true;
        }
        catch (RequestFailedException ex)
        {
            if (ex.Status == 412)
                Logger.LogInformation("Optimistic concurrency violation – entity has changed since it was retrieved.");
            else
                Logger.LogError(ex.Message);
            return false;
        }
    }
}