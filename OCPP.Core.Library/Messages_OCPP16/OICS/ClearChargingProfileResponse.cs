using Newtonsoft.Json;

namespace OCPP.Core.Library.Messages_OCPP16.OICS;

public class ClearChargingProfileResponse
{
    [JsonProperty("status", Required = Required.Always)]
    public ClearChargingProfileResponseStatus Status { get; set; }
}

public enum ClearChargingProfileResponseStatus { Accepted, Unknown }