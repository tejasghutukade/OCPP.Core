using System;
using System.Collections.Generic;

namespace OCPP.Core.Database
{
    public partial class ChargingProfile
    {
        public int Id { get; set; }
        public uint ChargePointId { get; set; }
        public int ConnectorId { get; set; }
        public int ChargingProfileId { get; set; }
        public int TransactionId { get; set; }
        public string ChargingProfilePurpose { get; set; } = null!;
        public string ChargingProfileKind { get; set; } = null!;
        public string RecurrencyKind { get; set; } = null!;
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public string ChargingSchedule { get; set; } = null!;
        public string? Status { get; set; }
        public virtual ChargePoint ChargePoint { get; set; } = null!;
    }
}
