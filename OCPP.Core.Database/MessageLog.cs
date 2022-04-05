using System;
using System.Collections.Generic;

namespace OCPP.Core.Database
{
    public partial class MessageLog
    {
        public int LogId { get; set; }
        public DateTime LogTime { get; set; }
        public uint ChargePointId { get; set; }
        public int? ConnectorId { get; set; }
        public string Message { get; set; } = null!;
        public string? Result { get; set; }
        public string? ErrorCode { get; set; }
        public string? Direction { get; set; }
    }
}
