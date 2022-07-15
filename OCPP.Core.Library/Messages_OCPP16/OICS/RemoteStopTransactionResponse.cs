using Newtonsoft.Json;

namespace OCPP.Core.Library.Messages_OCPP16.OICS;

public partial class RemoteStopTransactionRequest
{
    [JsonProperty("status", Required = Required.Always)]
    public RemoteStopTransactionStatus Status { get; set; }
}

public enum RemoteStopTransactionStatus { Accepted, Rejected }