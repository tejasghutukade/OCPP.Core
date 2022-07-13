using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Library.Messages_OCPP16;

namespace OCPP.Core.Library;

public partial class ControllerOcpp16
{
    private async Task<string?> HandleRemoteStopTransaction(OcppMessage msgIn)
    {
        string? errorCode = null;
        RemoteStopTransactionRequest? response =
            JsonConvert.DeserializeObject<RemoteStopTransactionRequest>(msgIn.JsonPayload);
        
        if(response ==null) return errorCode = "Invalid JSON";  
        
        var sendRequest = DbContext.SendRequests.Where(x => x.Uid == msgIn.UniqueId).FirstOrDefault();
        
        if(sendRequest == null) return errorCode = "No matching request found";
        
        var transaction = DbContext.Transactions.Where(x => x.TransactionId == response.TransactionId).FirstOrDefault();
        
        if(transaction == null) return errorCode = "No matching transaction found";

        var lastStatus = DbContext.ConnectorStatuses.LastOrDefault(x=> x.ChargePointId == sendRequest.ChargePointId && x.ConnectorId == sendRequest.ConnectorId);
        //if (lastStatus == null) return errorCode= "No last status found";
        
        int transactionId = (int)response.TransactionId;

        ChargeTag chargeTag = ChargeTag.IsValid(DbContext, sendRequest.ChargeTagId??0);
        CpTagAccess cpTagAccess = CpTagAccess.IsValid(DbContext, chargeTag.Id, ChargePointStatus.Id);
        StopTransactionRequest stopTransactionRequest = new StopTransactionRequest();
        stopTransactionRequest.Timestamp = DateTime.UtcNow;
        stopTransactionRequest.IdTag = chargeTag.TagId;
        
        if(DateTime.UtcNow.Subtract(lastStatus.LastMeterTime.Value).TotalSeconds > 30)
        {
           return errorCode = "Last meter time is more than 30 seconds old";
        }
        
        
        var lastMeter = lastStatus?.LastMeter??0;
        stopTransactionRequest.MeterStop = Convert.ToInt32(lastMeter);
        
        switch (response.Status)
        {
            case RemoteStopTransactionStatus.Accepted:
                //Stop Transaction
                
                await CompleteTransaction(transactionId, stopTransactionRequest, chargeTag,
                    cpTagAccess);
                sendRequest.Status = nameof(SendRequestStatus.Completed);
                break;
            case RemoteStopTransactionStatus.Rejected:
                //Reject Transaction
                sendRequest.Status = nameof(SendRequestStatus.Failed);
                break;
            default:
                return errorCode = "Invalid status";
        }

        DbContext.SendRequests.Update(sendRequest);
        await DbContext.SaveChangesAsync();
        return errorCode;
    }
}