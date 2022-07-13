using System;
using System.Collections.Generic;

namespace OCPP.Core.Database
{
    public partial class SendRequest
    {
        public int Id { get; set; }
        public string Uid { get; set; } = null!;
        public uint ChargePointId { get; set; }
        public int? ConnectorId { get; set; }
        public int? ChargeTagId { get; set; }
        public string RequestType { get; set; } = null!;
        public string? RequestPayload { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? CreatedDatetime { get; set; }
        public DateTime? UpdatedTimestamp { get; set; }

        public virtual ChargePoint ChargePoint { get; set; } = null!;
        public virtual ChargeTag? ChargeTag { get; set; }
    }
}
