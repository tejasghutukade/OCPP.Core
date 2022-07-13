using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Library.Messages_OCPP16;

namespace OCPP.Core.Library;

public partial class ControllerOcpp16
{
    private async Task HandleClearCache(OcppMessage msgIn)
    {
        ClearCacheResponse? response =
            JsonConvert.DeserializeObject<ClearCacheResponse>(msgIn.JsonPayload);
        
        if (response == null)return;
        
        var sendRequest = DbContext.SendRequests.Where(x => x.Uid == msgIn.UniqueId).FirstOrDefault();
        
        if (sendRequest == null) return ;

        switch (response.Status)
        {
            case ClearCacheResponseStatus.Accepted:
                sendRequest.Status = nameof(SendRequestStatus.Completed);
                break;
            case ClearCacheResponseStatus.Rejected:
            default:
                sendRequest.Status = nameof(SendRequestStatus.Failed);
                break;
        }
        
        
        DbContext.SendRequests.Update(sendRequest);
        await DbContext.SaveChangesAsync();
    }
}