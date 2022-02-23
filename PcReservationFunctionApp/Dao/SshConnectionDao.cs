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

    public List<SshConnection> GetAllUnassignedByLab(string partitionKey)
    {
        var oDataQueryEntities =
            TableClient.Query<SshConnection>(e => e.PartitionKey == partitionKey && e.Status == "Unassigned");
        return oDataQueryEntities.ToList();
    }

    public bool ChangeStatusToAssigned(SshConnection sshConnection)
    {
        try
        {
            sshConnection.Status = "Assigned";
            TableClient.UpdateEntity(sshConnection, sshConnection.ETag);
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