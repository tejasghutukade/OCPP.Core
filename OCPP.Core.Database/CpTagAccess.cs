using System;
using System.Collections.Generic;

namespace OCPP.Core.Database
{
    public partial class CpTagAccess
    {
        public int Id { get; set; }
        public int? TagId { get; set; }
        public uint? ChargePointId { get; set; }
        public DateTime? Expiry { get; set; }
        public string? CpTagStatus { get; set; }
        public DateTime? Timestamp { get; set; }

        public virtual ChargePoint? ChargePoint { get; set; }
        public virtual ChargeTag? Tag { get; set; }
    }
}
