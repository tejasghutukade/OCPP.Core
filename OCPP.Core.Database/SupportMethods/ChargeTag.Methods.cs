using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace OCPP.Core.Database;

public partial class ChargeTag
{
    /// <summary>
    /// Return a valid ChargeTagObject if its valid
    /// Else return null
    /// </summary>
    public static ChargeTag IsValid(OcppCoreContext dbContext,string? chargeTagId)
    {
        var emptyChargetag = new ChargeTag();
        emptyChargetag.TagStatus = "Invalid";
        emptyChargetag.Id = -1;
        if (string.IsNullOrEmpty(chargeTagId))
        {
            
            return emptyChargetag;
        }
        
        var chargeTag = dbContext.ChargeTags.FirstOrDefault(x => x.TagId == chargeTagId);
        if (chargeTag != null)
        {
            if(chargeTag.ExpiryDate < DateTime.UtcNow)
            {
                chargeTag.TagStatus = "Expired";
            }
            return chargeTag;
        }
        
        
        return emptyChargetag;


    }
    public static ChargeTag IsValid(OcppCoreContext dbContext,int tagId)
    {
        var emptyChargetag = new ChargeTag();
        emptyChargetag.TagStatus = "Invalid";
        emptyChargetag.Id = -1;
        if (tagId > 0)
        {
            
            return emptyChargetag;
        }
        
        var chargeTag = dbContext.ChargeTags.FirstOrDefault(x => x.Id == tagId);
        if (chargeTag != null)
        {
            if(chargeTag.ExpiryDate < DateTime.UtcNow)
            {
                chargeTag.TagStatus = "Expired";
            }
            return chargeTag;
        }
        
        
        return emptyChargetag;


    }

    public ChargeTagStatus GetChargeTagStatus()
    {
        return TagStatus switch
        {
            "Accepted" => ChargeTagStatus.Accepted,
            "Invalid" => ChargeTagStatus.Invalid,
            "Blocked" => ChargeTagStatus.Blocked,
            "Expired" => ChargeTagStatus.Expired,
            "ConcurrentTx" => ChargeTagStatus.ConcurrentTx,
            _ => ChargeTagStatus.Invalid
        };
    }
}