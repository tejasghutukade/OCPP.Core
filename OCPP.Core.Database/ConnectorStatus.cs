using System;
using System.Collections.Generic;

namespace OCPP.Core.Database
{
    public partial class ConnectorStatus
    {
        public int Id { get; set; }
        public uint ChargePointId { get; set; }
        public int ConnectorId { get; set; }
        public string? ConnectorName { get; set; }
        public string? LastStatus { get; set; }
        public DateTime? LastStatusTime { get; set; }
        public float? LastMeter { get; set; }
        public DateTime? LastMeterTime { get; set; }
        public string? ErrorCode { get; set; }
        public string? Info { get; set; }
        public string? VendorId { get; set; }
        public string? VendorErrorCode { get; set; }

        public virtual ChargePoint ChargePoint { get; set; } = null!;
    }
}
