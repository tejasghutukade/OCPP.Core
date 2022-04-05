using System;
using System.Collections.Generic;

namespace OCPP.Core.Database
{
    public partial class ChargePoint
    {
        public ChargePoint()
        {
            ChargingProfiles = new HashSet<ChargingProfile>();
            ConnectorStatuses = new HashSet<ConnectorStatus>();
            Transactions = new HashSet<Transaction>();
        }

        public uint Id { get; set; }
        public string ChargePointId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Comment { get; set; }
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? ClientCertThumb { get; set; }
        public string? Vendor { get; set; }
        public string? Model { get; set; }
        public string? CpSerialNumber { get; set; }
        public string? CbSerialNumber { get; set; }
        public string? FirmwareVersion { get; set; }
        public string? Iccid { get; set; }
        public string? Imsi { get; set; }
        public string? MeterType { get; set; }
        public string? MeterSerialNumber { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? CurrentTime { get; set; }
        public int Interval { get; set; }

        public virtual ICollection<ChargingProfile> ChargingProfiles { get; set; }
        public virtual ICollection<ConnectorStatus> ConnectorStatuses { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
}
