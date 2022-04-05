namespace OCPP.Core.Server.Messages_OCPP16;
using Newtonsoft.Json;


public partial class ClearChargingProfileRequest
{
    [JsonProperty("chargingProfilePurpose", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public ClearChargingProfileChargingProfilePurpose? ChargingProfilePurpose { get; set; }

    [JsonProperty("connectorId", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public long? ConnectorId { get; set; }

    [JsonProperty("id", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public long? Id { get; set; }

    [JsonProperty("stackLevel", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public long? StackLevel { get; set; }
}

public enum ClearChargingProfileChargingProfilePurpose { ChargePointMaxProfile, TxDefaultProfile, TxProfile }