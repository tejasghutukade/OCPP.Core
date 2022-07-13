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
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Newtonsoft.Json;

using OCPP.Core.Library.Messages_OCPP20;

namespace OCPP.Core.Library
{
    public partial class ControllerOcpp20
    {
        public string? HandleNotifyEvChargingSchedule(OcppMessage msgIn, OcppMessage msgOut)
        {
            string? errorCode = null;

            Logger.Verbose("Processing NotifyEVChargingSchedule...");
            NotifyEvChargingScheduleResponse notifyEvChargingScheduleResponse = new NotifyEvChargingScheduleResponse();
            notifyEvChargingScheduleResponse.CustomData = new CustomDataType();
            notifyEvChargingScheduleResponse.CustomData.VendorId = VendorId;

            StringBuilder periods = new StringBuilder();
            int connectorId = 0;

            try
            {
                NotifyEvChargingScheduleRequest notifyEvChargingScheduleRequest = JsonConvert.DeserializeObject<NotifyEvChargingScheduleRequest>(msgIn.JsonPayload);
                Logger.Verbose("NotifyEVChargingSchedule => Message deserialized");


                if (ChargePointStatus != null)
                {
                    // Known charge station
                    if (notifyEvChargingScheduleRequest.ChargingSchedule != null)
                    {
                        if (notifyEvChargingScheduleRequest.ChargingSchedule?.ChargingSchedulePeriod != null)
                        {
                            // Concat all periods and write them in messag log...

                            DateTimeOffset timeBase = notifyEvChargingScheduleRequest.TimeBase;
                            foreach (ChargingSchedulePeriodType period in notifyEvChargingScheduleRequest.ChargingSchedule?.ChargingSchedulePeriod)
                            {
                                if (periods.Length > 0)
                                {
                                    periods.Append(" | ");
                                }

                                DateTimeOffset time = timeBase.AddSeconds(period.StartPeriod);
                                periods.Append(string.Format("{0}: {1}{2}", time.ToString("O"), period.Limit, notifyEvChargingScheduleRequest.ChargingSchedule.ChargingRateUnit.ToString()));

                                if (period.NumberPhases > 0)
                                {
                                    periods.Append(string.Format(" ({0} Phases)", period.NumberPhases));
                                }
                            }
                        }
                    }
                    connectorId = notifyEvChargingScheduleRequest.EvseId;
                    Logger.Information("NotifyEVChargingSchedule => {0}", periods.ToString());
                }
                else
                {
                    // Unknown charge station
                    errorCode = ErrorCodes.GenericError;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(notifyEvChargingScheduleResponse);
                Logger.Verbose("NotifyEVChargingSchedule => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.Error(exp, "NotifyEVChargingSchedule => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.InternalError;
            }

            WriteMessageLog(ChargePointStatus.Id, connectorId, msgIn.Action, periods.ToString(), errorCode);
            return errorCode;
        }
    }
}
