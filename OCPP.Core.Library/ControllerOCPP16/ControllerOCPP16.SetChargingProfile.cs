using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Library.Messages_OCPP16;

namespace OCPP.Core.Library;

public partial class ControllerOcpp16
{
    private async Task HandleSetChargingProfile(OcppMessage msgIn)
    {
        SetChargingProfileResponse? response =
            JsonConvert.DeserializeObject<SetChargingProfileResponse>(msgIn.JsonPayload);
        
        if (response == null)return;
        
        var sendRequest = DbContext.SendRequests.Where(x => x.Uid == msgIn.UniqueId).FirstOrDefault();
        
        if (sendRequest == null) return ;

        switch (response.Status)
        {
            case SetChargingProfileStatus.Accepted:
                sendRequest.Status = nameof(SendRequestStatus.Completed);
                break;
            case SetChargingProfileStatus.Rejected:
            case SetChargingProfileStatus.NotSupported:
            default:
                sendRequest.Status = nameof(SendRequestStatus.Failed);
                break;
        }
        
        
        DbContext.SendRequests.Update(sendRequest);
        await DbContext.SaveChangesAsync();

    }
}