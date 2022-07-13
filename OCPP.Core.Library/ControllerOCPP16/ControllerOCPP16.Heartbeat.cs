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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Library.Messages_OCPP16;

namespace OCPP.Core.Library
{
    public partial class ControllerOcpp16
    {
        public async Task<string?> HandleHeartBeat(OcppMessage msgIn, OcppMessage msgOut)
        {
            string? errorCode = null;

            Logger.Verbose("Processing heartbeat...");
            HeartbeatResponse heartbeatResponse = new HeartbeatResponse();
            heartbeatResponse.CurrentTime = DateTimeOffset.UtcNow;
            
            var chargePoint = DbContext.ChargePoints.FirstOrDefault(x => x.ChargePointId.Equals(ChargePointStatus.ExtId));
            if (chargePoint != null)
            {
                chargePoint.CurrentTime = DateTime.UtcNow;
                DbContext.ChargePoints.Update(chargePoint);
                await DbContext.SaveChangesAsync();
            }

            msgOut.JsonPayload = JsonConvert.SerializeObject(heartbeatResponse);
            Logger.Verbose("Heartbeat => Response serialized");

            WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, null, errorCode);
            return errorCode;
        }
    }
}
