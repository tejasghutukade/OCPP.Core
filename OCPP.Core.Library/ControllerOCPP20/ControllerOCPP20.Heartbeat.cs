﻿/*
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

using Newtonsoft.Json;
using OCPP.Core.Library.Messages_OCPP20;

namespace OCPP.Core.Library
{
    public partial class ControllerOcpp20
    {
        public string? HandleHeartBeat(OcppMessage msgIn, OcppMessage msgOut)
        {
            string? errorCode = null;

            Logger.Verbose("Processing heartbeat...");
            HeartbeatResponse heartbeatResponse = new HeartbeatResponse();
            heartbeatResponse.CustomData = new CustomDataType();
            heartbeatResponse.CustomData.VendorId = Library.ControllerOcpp20.VendorId;

            heartbeatResponse.CurrentTime = DateTimeOffset.UtcNow;

            msgOut.JsonPayload = JsonConvert.SerializeObject(heartbeatResponse);
            Logger.Verbose("Heartbeat => Response serialized");

            WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, null, errorCode);
            return errorCode;
        }
    }
}
