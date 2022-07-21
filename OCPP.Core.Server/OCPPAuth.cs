using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using OCPP.Core.Database;

namespace OCPP.Core.Server;

public class OcppAuth
{
    private readonly ILogger<OcppAuth> _logger;
    private readonly OcppCoreContext _dbContext;

    public OcppAuth(ILogger<OcppAuth> logger, OcppCoreContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
        
    }
    
    public ChargePoint? Authenticate(string chargepointIdentifier, string username, string password,X509Certificate2? certificate)
    {
        _logger.LogDebug("Charge Point Identifier {ChargepointIdentifier} ",chargepointIdentifier);
        var chargePoint = _dbContext.ChargePoints.FirstOrDefault(x => x.ChargePointId == chargepointIdentifier);
        _logger.LogDebug("For testing Authentication is disable");
        return chargePoint;
        if (chargePoint == null)
        {
            return null;
        }
        if (chargePoint.Username != username || chargePoint.Password != password)
        {
            return null;
        }

        if (certificate == null) return chargePoint;
        return !certificate.Thumbprint.Equals(chargePoint.ClientCertThumb, StringComparison.InvariantCultureIgnoreCase) ? null : chargePoint;
    }
}