namespace OCPP.Core.Server.Messages_OCPP16;

using Newtonsoft.Json;

public partial class TriggerMessageResponse
{
    [JsonProperty("status", Required = Required.Always)]
    public TriggerMessageStatus Status { get; set; }
}

public enum TriggerMessageStatus { Accepted, NotImplemented, Rejected };