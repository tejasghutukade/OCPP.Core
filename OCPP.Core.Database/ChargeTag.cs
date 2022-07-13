using System;
using System.Collections.Generic;

namespace OCPP.Core.Database
{
    public partial class ChargeTag
    {
        public ChargeTag()
        {
            CpTagAccesses = new HashSet<CpTagAccess>();
            InverseParentTag = new HashSet<ChargeTag>();
            SendRequests = new HashSet<SendRequest>();
            TransactionStartTags = new HashSet<Transaction>();
            TransactionStopTags = new HashSet<Transaction>();
        }

        public int Id { get; set; }
        public string TagId { get; set; } = null!;
        public string? TagName { get; set; }
        public int? ParentTagId { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string TagStatus { get; set; } = null!;

        public virtual ChargeTag? ParentTag { get; set; }
        public virtual ICollection<CpTagAccess> CpTagAccesses { get; set; }
        public virtual ICollection<ChargeTag> InverseParentTag { get; set; }
        public virtual ICollection<SendRequest> SendRequests { get; set; }
        public virtual ICollection<Transaction> TransactionStartTags { get; set; }
        public virtual ICollection<Transaction> TransactionStopTags { get; set; }
    }
}
