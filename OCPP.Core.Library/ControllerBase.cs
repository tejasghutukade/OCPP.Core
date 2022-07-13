/*
 * OCPP.Core - https://github.com/dallmann-consulting/OCPP.Core
 * Copyright (C) 2020-2021 dallmann consulting GmbH.
 * All Rights Reserved.
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using OCPP.Core.Database;

namespace OCPP.Core.Library
{
    public partial class ControllerBase
    {
        /// <summary>
        /// Configuration context for reading app settings
        /// </summary>
        protected IConfiguration Configuration { get; set; }

        /// <summary>
        /// Chargepoint status
        /// </summary>
        protected ChargePointStatus ChargePointStatus { get; set; }

        /// <summary>
        /// ILogger object
        /// </summary>
        protected ILogger Logger { get; set; }
            
        /// <summary>
        /// DBContext object
        /// </summary>
        protected OcppCoreContext DbContext { get; set; }

        /// <summary>
        /// Connected Chargepoint class for the current session
        ///</summary>
        protected ChargePoint ConnectedChargePoint { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ControllerBase(IConfiguration config, ILogger logger,OcppCoreContext dbContext)
        {
            Configuration = config;
            DbContext = dbContext;
            Logger = logger;
        }

        public void SetChargePointStatus(ChargePointStatus status)
        {
            ChargePointStatus = status;
            var cp = DbContext.ChargePoints.FirstOrDefault(x=>x.ChargePointId.Equals(status.ExtId));

            ConnectedChargePoint = cp ?? throw new Exception("Chargepoint not found even though it was authorized");
        }
        /// <summary>
        /// Helper function for creating and updating the ConnectorStatus in then database
        /// </summary>
        protected async Task<bool> UpdateConnectorStatus(int connectorId, string status, DateTimeOffset? statusTime, double? meter, DateTimeOffset? meterTime)
        {
            try
            {
                using (var dbContext = DbContext)
                {
                    ConnectorStatus? connectorStatus = dbContext.Find<ConnectorStatus>(ChargePointStatus.Id, connectorId);
                    if (connectorStatus == null)
                    {
                        // no matching entry => create connector status
                        connectorStatus = new ConnectorStatus();
                        connectorStatus.ChargePointId = ChargePointStatus.Id;
                        connectorStatus.ConnectorId = connectorId;
                        
                        Logger.Verbose("UpdateConnectorStatus => Creating new DB-ConnectorStatus: ID={ChargePointId} / Connector={ConnectorId}", connectorStatus.ChargePointId, connectorStatus.ConnectorId);
                        dbContext.Add<ConnectorStatus>(connectorStatus);
                    }

                    if (!string.IsNullOrEmpty(status))
                    {
                        connectorStatus.LastStatus = status;
                        connectorStatus.LastStatusTime = ((statusTime.HasValue) ? statusTime.Value : DateTimeOffset.UtcNow).DateTime;
                    }

                    if (meter.HasValue)
                    {
                        connectorStatus.LastMeter = (float?) meter.Value;
                        connectorStatus.LastMeterTime = ((meterTime.HasValue) ? meterTime.Value : DateTimeOffset.UtcNow).DateTime;
                    }
                    await dbContext.SaveChangesAsync();
                    Logger.Information("UpdateConnectorStatus => Save ConnectorStatus: ID={0} / Connector={1} / Status={2} / Meter={3}", connectorStatus.ChargePointId, connectorId, status, meter);
                    return true;
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp, "UpdateConnectorStatus => Exception writing connector status (ID={0} / Connector={1}): {2}", ChargePointStatus?.Id, connectorId, exp.Message);
            }

            return false;
        }

        /// <summary>
        /// Clean charge tag Id from possible suffix ("..._abc")
        /// </summary>
        protected static string CleanChargeTagId(string? rawChargeTagId, ILogger logger)
        {
            var idTag = rawChargeTagId??string.Empty;

            // KEBA adds the serial to the idTag ("<idTag>_<serial>") => cut off suffix
            if (string.IsNullOrWhiteSpace(rawChargeTagId)) return idTag;
            var sep = rawChargeTagId.IndexOf('_');
            if (sep < 0) return idTag;
            idTag = rawChargeTagId.Substring(0, sep);
            
            return idTag;
        }

        protected static DateTimeOffset MaxExpiryDate
        {
            get
            {
                return new DateTime(2199, 12, 31);
            }
        }
    }
}
