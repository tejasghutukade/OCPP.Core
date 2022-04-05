namespace OCPP.Core.Server.Messages_OCPP16;

using Newtonsoft.Json;

public class ClearChargingProfileResponse
{
    [JsonProperty("status", Required = Required.Always)]
    public ClearChargingProfileResponseStatus Status { get; set; }
}

public enum ClearChargingProfileResponseStatus { Accepted, Unknown }