using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using PcReservationFunctionApp.Helper;

namespace PcReservationFunctionApp
{
    public class IotHubTriggerFunction
    {
        [FunctionName("IotHubTriggerFunction")]
        public async Task Run([EventHubTrigger(nameof(Config.Key.EventHubName), Connection = nameof(Config.Key.EventHubPrimaryConnectionString))] EventData myEventHubMessage,
            DateTime enqueuedTimeUtc,
            Int64 sequenceNumber,
            string offset,
            ILogger log)
        {
            log.LogInformation($"Event: {Encoding.UTF8.GetString(myEventHubMessage.Body)}");
            // Metadata accessed by binding to EventData
            log.LogInformation($"EnqueuedTimeUtc={myEventHubMessage.SystemProperties.EnqueuedTimeUtc}");
            log.LogInformation($"SequenceNumber={myEventHubMessage.SystemProperties.SequenceNumber}");
            log.LogInformation($"Offset={myEventHubMessage.SystemProperties.Offset}");
            // Metadata accessed by using binding expressions in method parameters
            log.LogInformation($"EnqueuedTimeUtc={enqueuedTimeUtc}");
            log.LogInformation($"SequenceNumber={sequenceNumber}");
            log.LogInformation($"Offset={offset}");

            if (myEventHubMessage.Properties.ContainsKey("opType"))
            {
                var opType = myEventHubMessage.Properties["opType"] as string;
                log.LogInformation(opType!);
                if (opType.Equals("deviceConnected"))
                {

                }else if (opType.Equals("deviceDisconnected"))
                {

                }
            }
        }
    }
}
