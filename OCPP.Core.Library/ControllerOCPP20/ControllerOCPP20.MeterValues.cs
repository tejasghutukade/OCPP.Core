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

using OCPP.Core.Library.Messages_OCPP20;

namespace OCPP.Core.Library
{
    public partial class ControllerOcpp20
    {
        public string? HandleMeterValues(OcppMessage msgIn, OcppMessage msgOut)
        {
            string? errorCode = null;
            MeterValuesResponse meterValuesResponse = new MeterValuesResponse();

            meterValuesResponse.CustomData = new CustomDataType();
            meterValuesResponse.CustomData.VendorId = VendorId;

            int connectorId = -1;
            string msgMeterValue = string.Empty;

            try
            {
                Logger.Verbose("Processing meter values...");
                MeterValuesRequest meterValueRequest = JsonConvert.DeserializeObject<MeterValuesRequest>(msgIn.JsonPayload);
                Logger.Verbose("MeterValues => Message deserialized");

                connectorId = meterValueRequest.EvseId;

                if (ChargePointStatus != null)
                {
                    // Known charge station => extract meter values with correct scale
                    double currentChargeKw = -1;
                    double meterKwh = -1;
                    DateTimeOffset? meterTime = null;
                    double stateOfCharge = -1;
                    GetMeterValues(meterValueRequest.MeterValue, out meterKwh, out currentChargeKw, out stateOfCharge, out meterTime);

                    // write charging/meter data in chargepoint status
                    if (connectorId > 0)
                    {
                        msgMeterValue = $"Meter (kWh): {meterKwh} | Charge (kW): {currentChargeKw} | SoC (%): {stateOfCharge}";

                        if (meterKwh >= 0)
                        {
                            UpdateConnectorStatus(connectorId, null, null, meterKwh, meterTime);
                        }

                        if (currentChargeKw >= 0 || meterKwh >= 0 || stateOfCharge >= 0)
                        {
                            if (ChargePointStatus.OnlineConnectors.ContainsKey(connectorId))
                            {
                                OnlineConnectorStatus ocs = ChargePointStatus.OnlineConnectors[connectorId];
                                if (currentChargeKw >= 0) ocs.ChargeRateKw = currentChargeKw;
                                if (meterKwh >= 0) ocs.MeterKwh = meterKwh;
                                if (stateOfCharge >= 0) ocs.SoC = stateOfCharge;
                            }
                            else
                            {
                                OnlineConnectorStatus ocs = new OnlineConnectorStatus();
                                if (currentChargeKw >= 0) ocs.ChargeRateKw = currentChargeKw;
                                if (meterKwh >= 0) ocs.MeterKwh = meterKwh;
                                if (stateOfCharge >= 0) ocs.SoC = stateOfCharge;
                                if (ChargePointStatus.OnlineConnectors.TryAdd(connectorId, ocs))
                                {
                                    Logger.Verbose("MeterValues => Set OnlineConnectorStatus for ChargePoint={0} / Connector={1} / Values: {2}", ChargePointStatus?.Id, connectorId, msgMeterValue);
                                }
                                else
                                {
                                    Logger.Error("MeterValues => Error adding new OnlineConnectorStatus for ChargePoint={0} / Connector={1} / Values: {2}", ChargePointStatus?.Id, connectorId, msgMeterValue);
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Unknown charge station
                    errorCode = ErrorCodes.GenericError;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(meterValuesResponse);
                Logger.Verbose("MeterValues => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.Error(exp, "MeterValues => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.InternalError;
            }

            WriteMessageLog(ChargePointStatus.Id, connectorId, msgIn.Action, msgMeterValue, errorCode);
            return errorCode;
        }
    }
}
