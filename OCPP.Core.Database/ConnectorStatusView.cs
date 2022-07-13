using System;
using System.Collections.Generic;

namespace OCPP.Core.Database
{
    public partial class ConnectorStatusView
    {
        public uint ChargePointId { get; set; }
        public int ConnectorId { get; set; }
        public string? ConnectorName { get; set; }
        public string? LastStatus { get; set; }
        public DateTime? LastStatusTime { get; set; }
        public float? LastMeter { get; set; }
        public DateTime? LastMeterTime { get; set; }
        public int? TransactionId { get; set; }
        public int? StartTagId { get; set; }
        public DateTime? StartTime { get; set; }
        public float? MeterStart { get; set; }
        public string? StartResult { get; set; }
        public int? StopTagId { get; set; }
        public DateTime? StopTime { get; set; }
        public float? MeterStop { get; set; }
        public string? StopReason { get; set; }
    }
}
