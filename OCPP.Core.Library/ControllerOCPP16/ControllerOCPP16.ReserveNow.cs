using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Library.Messages_OCPP16.OICS;

namespace OCPP.Core.Library;

public partial class ControllerOcpp16
{
    private async Task HandleReserveNow(OcppMessage msgIn)
    {
        ReserveNowResponse? response =
            JsonConvert.DeserializeObject<ReserveNowResponse>(msgIn.JsonPayload);
        
        if (response == null)return;
        
        var sendRequest = Queryable.Where<SendRequest>(DbContext.SendRequests, x => x.Uid == msgIn.UniqueId).FirstOrDefault();
        
        if (sendRequest == null) return ;

        switch (response.Status)
        {
            case ReserveNowStatus.Accepted:
            case ReserveNowStatus.Occupied:
            case ReserveNowStatus.Unavailable:
                sendRequest.Status = nameof(SendRequestStatus.Completed);
                break;
            case ReserveNowStatus.Faulted:
            case ReserveNowStatus.Rejected:
            default:
                sendRequest.Status = nameof(SendRequestStatus.Failed);
                break;
        }
        
        
        DbContext.SendRequests.Update(sendRequest);
        await DbContext.SaveChangesAsync();
    }
}