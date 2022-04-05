namespace OCPP.Core.Server.Messages_OCPP16;

using Newtonsoft.Json;

public partial class RemoteStopTransactionRequest
{
    [JsonProperty("status", Required = Required.Always)]
    public RemoteStopTransactionStatus Status { get; set; }
}

public enum RemoteStopTransactionStatus { Accepted, Rejected }