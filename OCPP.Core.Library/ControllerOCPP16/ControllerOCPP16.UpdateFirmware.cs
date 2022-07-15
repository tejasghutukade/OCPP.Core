using OCPP.Core.Database;

namespace OCPP.Core.Library;

public partial class ControllerOcpp16
{
    private async Task HandleUpdateFirmware(OcppMessage msgIn)
    {
        
        
        var sendRequest = Queryable.Where<SendRequest>(DbContext.SendRequests, x => x.Uid == msgIn.UniqueId).FirstOrDefault();
        
        if (sendRequest == null) return ;

        
        sendRequest.Status = nameof(SendRequestStatus.Completed);
        DbContext.SendRequests.Update(sendRequest);
        await DbContext.SaveChangesAsync();

    }
}