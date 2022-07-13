namespace OCPP.Core.Database;

public partial class CpTagAccess
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="db">OccpCoreContrext Object</param>
    /// <param name="tagId">Internal Tag Id</param>
    /// <param name="chargePointId">Internal ChargePointId</param>
    /// <returns>If Vaid return CpTagAccess Object or else return null</returns>
    public static CpTagAccess IsValid(OcppCoreContext db,int tagId,uint chargePointId)
    {
        var emptycptagaccess = new CpTagAccess();
        emptycptagaccess.CpTagStatus = "Invalid";
        if(tagId <=0 || chargePointId <=0)
        {
            return emptycptagaccess;
        }
        var cptagaccess = db.CpTagAccesses.LastOrDefault(x => x.ChargePointId == chargePointId && x.TagId == tagId);
        
        if(cptagaccess !=null) return emptycptagaccess;
        
        return emptycptagaccess;
    }
    
    public ChargeTagStatus GetChargeTagStatus()
    {
        return CpTagStatus switch
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