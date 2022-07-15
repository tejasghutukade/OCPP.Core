using Newtonsoft.Json;

namespace OCPP.Core.Library.Messages_OCPP16.OICS;

public partial class RemoteStartTransactionResponse
{
    [JsonProperty("status", Required = Required.Always)]
    public RemoteStartTransaction Status { get; set; }
}

public enum RemoteStartTransaction { Accepted, Rejected };