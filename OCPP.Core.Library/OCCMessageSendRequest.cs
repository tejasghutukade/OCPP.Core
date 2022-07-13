using OCPP.Core.Database;

namespace OCPP.Core.Library;

public class OccMessageSendRequest
{
    public OcppMessage Message;
    public SendRequest SendRequest;

    public OccMessageSendRequest(OcppMessage message, SendRequest sendRequest)
    {
        this.Message = message;
        this.SendRequest = sendRequest;
    }
}