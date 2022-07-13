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
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Serilog;
using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Library.Messages_OCPP16;

namespace OCPP.Core.Library
{
    public partial class ControllerOcpp16
    {
        public async Task<string?> HandleBootNotification(OcppMessage msgIn, OcppMessage msgOut)
        {
            string? errorCode = null;

            try
            {
                Logger.Verbose("Processing boot notification...");
                BootNotificationRequest? bootNotification = JsonConvert.DeserializeObject<BootNotificationRequest>(msgIn.JsonPayload);
                Logger.Verbose("BootNotification => Message deserialized");


                var chargePoint = DbContext.ChargePoints.FirstOrDefault(x => x.ChargePointId.Equals(ChargePointStatus.ExtId));

                if (chargePoint != null && bootNotification !=null)
                {
                    chargePoint.Vendor = bootNotification.ChargePointVendor;
                    chargePoint.Model = bootNotification.ChargePointModel;
                    if(!string.IsNullOrEmpty(bootNotification.Iccid))
                        chargePoint.Iccid = bootNotification.Iccid;
                    if(!string.IsNullOrEmpty(bootNotification.Imsi))
                        chargePoint.Imsi = bootNotification.Imsi;
                    if(!string.IsNullOrEmpty(bootNotification.FirmwareVersion))
                        chargePoint.FirmwareVersion = bootNotification.FirmwareVersion;
                    if(!string.IsNullOrEmpty(bootNotification.MeterType))   
                        chargePoint.MeterType = bootNotification.MeterType;
                    if(!string.IsNullOrEmpty(bootNotification.MeterSerialNumber))   
                        chargePoint.MeterSerialNumber = bootNotification.MeterSerialNumber;
                    if(!string.IsNullOrEmpty(bootNotification.ChargeBoxSerialNumber))
                        chargePoint.CbSerialNumber = bootNotification.ChargeBoxSerialNumber;
                    if(!string.IsNullOrEmpty(bootNotification.ChargePointSerialNumber))
                        chargePoint.CpSerialNumber = bootNotification.ChargePointSerialNumber;
                    
                    chargePoint.CurrentTime = DateTime.UtcNow;
                    
                    DbContext.ChargePoints.Update(chargePoint);
                    var response = await DbContext.SaveChangesAsync();
                    
                    BootNotificationResponse bootNotificationResponse = new BootNotificationResponse();
                    bootNotificationResponse.CurrentTime = DateTimeOffset.UtcNow;
                    bootNotificationResponse.Interval = 300;    // 300 seconds
                    if (response > 0)
                    {
                        // Known charge station => accept
                        bootNotificationResponse.Status = BootNotificationResponseStatus.Accepted;
                    }
                    else
                    {
                        bootNotificationResponse.Status = BootNotificationResponseStatus.Rejected;
                        errorCode = "Unable to update charge point, database error";
                    }

                    msgOut.JsonPayload = JsonConvert.SerializeObject(bootNotificationResponse);
                    Logger.Verbose("BootNotification => Response serialized");
                }
                else
                {
                    errorCode = "Unable to find charge point or Invalid BootNotification";
                }


                
            }
            catch (Exception exp)
            {
                Logger.Error(exp, "BootNotification => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, null, errorCode);
            return errorCode;
        }
    }
}
