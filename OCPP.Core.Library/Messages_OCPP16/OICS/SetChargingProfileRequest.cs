using System;

namespace OCPP.Core.Library.Messages_OCPP16;

using Newtonsoft.Json;

public partial class SetChargingProfileRequest
    {
        [JsonProperty("connectorId", Required = Required.Always)]
        public long ConnectorId { get; set; }

        [JsonProperty("csChargingProfiles", Required = Required.Always)]
        public CsChargingProfiles CsChargingProfiles { get; set; }
    }

    public partial class CsChargingProfiles
    {
        [JsonProperty("chargingProfileId", Required = Required.Always)]
        public long ChargingProfileId { get; set; }

        [JsonProperty("chargingProfileKind", Required = Required.Always)]
        public SetChargingProfileChargingProfileKind ChargingProfileKind { get; set; }

        [JsonProperty("chargingProfilePurpose", Required = Required.Always)]
        public ChargingProfilePurpose ChargingProfilePurpose { get; set; }

        [JsonProperty("chargingSchedule", Required = Required.Always)]
        public SetChargingProfileChargingSchedule ChargingSchedule { get; set; }

        [JsonProperty("recurrencyKind", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public SetChargingProfileRecurrencyKind? RecurrencyKind { get; set; }

        [JsonProperty("stackLevel", Required = Required.Always)]
        public long StackLevel { get; set; }

        [JsonProperty("transactionId", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public long? TransactionId { get; set; }

        [JsonProperty("validFrom", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? ValidFrom { get; set; }

        [JsonProperty("validTo", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? ValidTo { get; set; }
    }

    public partial class SetChargingProfileChargingSchedule
    {
        [JsonProperty("chargingRateUnit", Required = Required.Always)]
        public SetChargingProfileChargingRateUnit ChargingRateUnit { get; set; }

        [JsonProperty("chargingSchedulePeriod", Required = Required.Always)]
        public ChargingSchedulePeriod[] ChargingSchedulePeriod { get; set; }

        [JsonProperty("duration", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public long? Duration { get; set; }

        [JsonProperty("minChargingRate", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public double? MinChargingRate { get; set; }

        [JsonProperty("startSchedule", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public DateTimeOffset? StartSchedule { get; set; }
    }

    public partial class SetChargingProfileChargingSchedulePeriod
    {
        [JsonProperty("limit", Required = Required.Always)]
        public double Limit { get; set; }

        [JsonProperty("numberPhases", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public long? NumberPhases { get; set; }

        [JsonProperty("startPeriod", Required = Required.Always)]
        public long StartPeriod { get; set; }
    }

    public enum SetChargingProfileChargingProfileKind { Absolute, Recurring, Relative };

    public enum ChargingProfilePurpose { ChargePointMaxProfile, TxDefaultProfile, TxProfile };

    public enum SetChargingProfileChargingRateUnit { A, W };

    public enum SetChargingProfileRecurrencyKind { Daily, Weekly };