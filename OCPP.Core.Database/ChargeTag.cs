using System;
using System.Collections.Generic;

namespace OCPP.Core.Database
{
    public partial class ChargeTag
    {
        public ChargeTag()
        {
            InverseParentTag = new HashSet<ChargeTag>();
        }

        public int Id { get; set; }
        public string TagId { get; set; } = null!;
        public string? TagName { get; set; }
        public int? ParentTagId { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string TagStatus { get; set; } = null!;

        public virtual ChargeTag? ParentTag { get; set; }
        public virtual ICollection<ChargeTag> InverseParentTag { get; set; }
    }
}
