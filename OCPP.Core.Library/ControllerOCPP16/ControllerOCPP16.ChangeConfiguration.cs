using Newtonsoft.Json;
using OCPP.Core.Database;

using OCPP.Core.Library.Messages_OCPP16.OICS;

namespace OCPP.Core.Library;

public partial class ControllerOcpp16
{
    private async Task HandleChangeConfiguration(OcppMessage msgIn)
    {
        ChangeConfigurationResponse? response =
            JsonConvert.DeserializeObject<ChangeConfigurationResponse>(msgIn.JsonPayload);
        
        if (response == null)return;
        
        var sendRequest = Queryable.Where<SendRequest>(DbContext.SendRequests, x => x.Uid == msgIn.UniqueId).FirstOrDefault();
        
        if (sendRequest == null) return ;

        switch (response.Status)
        {
            case ChangeConfigurationResponseStatus.Accepted:
            case ChangeConfigurationResponseStatus.RebootRequired:
                sendRequest.Status = nameof(SendRequestStatus.Completed);
                break;
            case ChangeConfigurationResponseStatus.Rejected:
            case ChangeConfigurationResponseStatus.NotSupported:
            default:
                sendRequest.Status = nameof(SendRequestStatus.Failed);
                break;
        }
        
        
        DbContext.SendRequests.Update(sendRequest);
        await DbContext.SaveChangesAsync();
    }
}