using System;
using System.Collections.Generic;

namespace OCPP.Core.Database
{
    public partial class Transaction
    {
        public int TransactionId { get; set; }
        public string? Uid { get; set; }
        public uint ChargePointId { get; set; }
        public int ConnectorId { get; set; }
        public string StartTagId { get; set; } = null!;
        public DateTime StartTime { get; set; }
        public float MeterStart { get; set; }
        public string StartResult { get; set; } = null!;
        public string StopTagId { get; set; } = null!;
        public DateTime? StopTime { get; set; }
        public float? MeterStop { get; set; }
        public string StopReason { get; set; } = null!;
        public int? ReservationId { get; set; }
        public string? TransactionData { get; set; }

        public virtual ChargePoint ChargePoint { get; set; } = null!;
    }
}
