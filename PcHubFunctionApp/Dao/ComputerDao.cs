using System.Collections.Generic;
using System.Linq;
using Azure;
using Microsoft.Azure.Management.ContainerRegistry.Fluent;
using Microsoft.Extensions.Logging;
using PcHubFunctionApp.Model;
using PcHubFunctionApp.Helper;

namespace PcHubFunctionApp.Dao;

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

    public Computer GetComputerByEmail(string location, string email)
    {
        var oDataQueryEntities = TableClient.Query<Computer>($"PartitionKey eq '{location}' and Email eq '{email}'");
        return oDataQueryEntities.FirstOrDefault();
    }

    public Computer GetComputerByMachineName(string location, string machineName)
    {
        var oDataQueryEntities = TableClient.Query<Computer>($"PartitionKey eq '{location}' and MachineName eq '{machineName}'");
        return oDataQueryEntities.FirstOrDefault();
    }

    public Computer GetComputerBySeatNumber(string location,int seatNumber)
    {
        var oDataQueryEntities =
            TableClient.Query<Computer>($"PartitionKey eq '{location}'").OrderBy(c=>c.MachineName);
        return oDataQueryEntities.ElementAtOrDefault(seatNumber);
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