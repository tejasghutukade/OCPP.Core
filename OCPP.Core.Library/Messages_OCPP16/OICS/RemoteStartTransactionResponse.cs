namespace OCPP.Core.Library.Messages_OCPP16;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public partial class RemoteStartTransactionResponse
{
    [JsonProperty("status", Required = Required.Always)]
    public RemoteStartTransaction Status { get; set; }
}

public enum RemoteStartTransaction { Accepted, Rejected };