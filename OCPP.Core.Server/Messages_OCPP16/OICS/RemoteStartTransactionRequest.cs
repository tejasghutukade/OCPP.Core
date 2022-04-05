using System;

namespace OCPP.Core.Server.Messages_OCPP16;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public partial class RemoteStartTransactionRequest
{
    [JsonProperty("chargingProfile", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public ChargingProfile ChargingProfile { get; set; }

    [JsonProperty("connectorId", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public long? ConnectorId { get; set; }

    [JsonProperty("idTag", Required = Required.Always)]
    [System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
    [System.ComponentModel.DataAnnotations.StringLength(20)]
    public string IdTag { get; set; }
}
public partial class ChargingProfile
{
    [JsonProperty("chargingProfileId", Required = Required.Always)]
    public long ChargingProfileId { get; set; }

    [JsonProperty("chargingProfileKind", Required = Required.Always)]
    public ChargingProfileKind ChargingProfileKind { get; set; }

    [JsonProperty("chargingProfilePurpose", Required = Required.Always)]
    public RemoteStartTransactionChargingProfilePurpose ChargingProfilePurpose { get; set; }

    [JsonProperty("chargingSchedule", Required = Required.Always)]
    public ChargingSchedule ChargingSchedule { get; set; }

    [JsonProperty("recurrencyKind", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public RecurrencyKind? RecurrencyKind { get; set; }

    [JsonProperty("stackLevel", Required = Required.Always)]
    public long StackLevel { get; set; }

    [JsonProperty("transactionId", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public long? TransactionId { get; set; }

    [JsonProperty("validFrom", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? ValidFrom { get; set; }

    [JsonProperty("validTo", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? ValidTo { get; set; }
}
public partial class ChargingSchedule
{
    [JsonProperty("chargingRateUnit", Required = Required.Always)]
    public ChargingRateUnit ChargingRateUnit { get; set; }

    [JsonProperty("chargingSchedulePeriod", Required = Required.Always)]
    public ChargingSchedulePeriod[] ChargingSchedulePeriod { get; set; }

    [JsonProperty("duration", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public long? Duration { get; set; }

    [JsonProperty("minChargingRate", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public double? MinChargingRate { get; set; }

    [JsonProperty("startSchedule", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public DateTimeOffset? StartSchedule { get; set; }
}


public partial class ChargingSchedulePeriod
{
    [JsonProperty("limit", Required = Required.Always)]
    public double Limit { get; set; }

    [JsonProperty("numberPhases", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
    public long? NumberPhases { get; set; }

    [JsonProperty("startPeriod", Required = Required.Always)]
    public long StartPeriod { get; set; }
}

public enum ChargingProfileKind { Absolute, Recurring, Relative }

public enum RemoteStartTransactionChargingProfilePurpose { ChargePointMaxProfile, TxDefaultProfile, TxProfile }

public enum ChargingRateUnit { A, W }

public enum RecurrencyKind { Daily, Weekly }