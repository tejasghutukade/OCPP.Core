using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Library.Messages_OCPP16.OICP;
using OCPP.Core.Library.Messages_OCPP16.OICS;

namespace OCPP.Core.Library;

public partial class ControllerOcpp16
{
    private async Task<string?> HandleRemoteStartTransaction(OcppMessage msgIn)
    {
        string? errorCode = null;
        RemoteStartTransactionResponse? response = JsonConvert.DeserializeObject<RemoteStartTransactionResponse>(msgIn.JsonPayload);

        if (response == null) return errorCode= "Invalid JSON";
        
        var sendRequest = Queryable.Where<SendRequest>(DbContext.SendRequests, x => x.Uid == msgIn.UniqueId).FirstOrDefault();
        
        if (sendRequest == null) return errorCode= "Invalid UniqueId. No Send-Request Request found";
        
        
        if (response.Status == RemoteStartTransaction.Accepted )
        {
            
            
            sendRequest.Status = nameof(SendRequestStatus.Completed);
            DbContext.SendRequests.Update(sendRequest);
            await DbContext.SaveChangesAsync();


            if (sendRequest.ChargeTag == null) return errorCode= "ChargeTag is null";
            
            
            ChargeTag chargeTag = ChargeTag.IsValid(DbContext,sendRequest.ChargeTagId??0);

            CpTagAccess cpTagAccess = CpTagAccess.IsValid(DbContext, chargeTag.Id, ChargePointStatus.Id);
            
            //Get last status of the Chargepoint
            
            var lastStatus = Queryable.LastOrDefault<ConnectorStatus>(DbContext.ConnectorStatuses, x=> x.ChargePointId == sendRequest.ChargePointId && x.ConnectorId == sendRequest.ConnectorId);
            if (lastStatus == null) return errorCode= "No last status found";
            

            StartTransactionRequest str = new StartTransactionRequest();
            str.Timestamp = lastStatus.LastMeterTime??DateTime.UtcNow;
            str.MeterStart = Convert.ToInt32(lastStatus.LastMeter*1000);

            await StartTransaction(sendRequest.ConnectorId??0 ,str,chargeTag,cpTagAccess);
            sendRequest.Status = nameof(SendRequestStatus.Completed);
        }
        else
        {
            //Transation Rejected
            sendRequest.Status = nameof(SendRequestStatus.Failed);
            errorCode= "Transaction Rejected";
        }
        DbContext.SendRequests.Update(sendRequest);
        await DbContext.SaveChangesAsync();
        return errorCode;
    }
}