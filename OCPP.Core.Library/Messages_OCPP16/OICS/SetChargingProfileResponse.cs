using Newtonsoft.Json;

namespace OCPP.Core.Library.Messages_OCPP16.OICS;

public partial class SetChargingProfileResponse
{
    [JsonProperty("status", Required = Required.Always)]
    public SetChargingProfileStatus Status { get; set; }
}

public enum SetChargingProfileStatus { Accepted, NotSupported, Rejected };