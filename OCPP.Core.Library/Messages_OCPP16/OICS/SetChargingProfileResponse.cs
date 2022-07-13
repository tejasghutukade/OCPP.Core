namespace OCPP.Core.Library.Messages_OCPP16;

using Newtonsoft.Json;

public partial class SetChargingProfileResponse
{
    [JsonProperty("status", Required = Required.Always)]
    public SetChargingProfileStatus Status { get; set; }
}

public enum SetChargingProfileStatus { Accepted, NotSupported, Rejected };