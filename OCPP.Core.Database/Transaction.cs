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
        public int? StartTagId { get; set; }
        public DateTime StartTime { get; set; }
        public float MeterStart { get; set; }
        public string StartResult { get; set; } = null!;
        public int? StopTagId { get; set; }
        public DateTime? StopTime { get; set; }
        public float? MeterStop { get; set; }
        public string? StopReason { get; set; }
        public int? ReservationId { get; set; }
        public string? TransactionData { get; set; }
        public string TransactionStatus { get; set; } = null!;

        public virtual ChargePoint ChargePoint { get; set; } = null!;
        public virtual ChargeTag? StartTag { get; set; }
        public virtual ChargeTag? StopTag { get; set; }
    }
}
