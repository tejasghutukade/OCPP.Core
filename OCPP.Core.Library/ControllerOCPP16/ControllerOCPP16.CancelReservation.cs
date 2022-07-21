using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Library.Messages_OCPP16.OICS;

namespace OCPP.Core.Library;

public partial class ControllerOcpp16
{
    private async Task HandleCancelReservation(OcppMessage msgIn)
    {
        CancelReservationResponse? response =
            JsonConvert.DeserializeObject<CancelReservationResponse>(msgIn.JsonPayload);
        
        if (response == null)return;
        
        var sendRequest = Queryable.Where<SendRequest>(DbContext.SendRequests, x => x.Uid == msgIn.UniqueId).FirstOrDefault();
        
        if (sendRequest == null) return ;

        switch (response.Status)
        {
            case CancelReservationResponseStatus.Accepted:
                sendRequest.Status = nameof(SendRequestStatus.Completed);
                break;
            case CancelReservationResponseStatus.Rejected:
                default:
                sendRequest.Status = nameof(SendRequestStatus.Failed);
                break;
        }
        
        
        DbContext.SendRequests.Update(sendRequest);
        await DbContext.SaveChangesAsync();

    }
}