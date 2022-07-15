using Newtonsoft.Json;

namespace OCPP.Core.Library.Messages_OCPP16.OICS;

public partial class TriggerMessageResponse
{
    [JsonProperty("status", Required = Required.Always)]
    public TriggerMessageStatus Status { get; set; }
}

public enum TriggerMessageStatus { Accepted, NotImplemented, Rejected };