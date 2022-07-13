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


/*
 http://www.diva-portal.se/smash/get/diva2:838105/FULLTEXT01.pdf

    Measurand values                    Description
    Energy.Active.Import.Register       Energy imported by EV (Wh of kWh)
    Power.Active.Import                 Instantaneous active power imported by EV (W or kW)
    Current.Import                      Instantaneous current flow to EV (A)
    Voltage                             AC RMS supply voltage (V)
    Temperature                         Temperature reading inside the charge point 

 <cs:meterValuesRequest>
   <cs:connectorId>0</cs:connectorId>
   <cs:transactionId>170</cs:transactionId>
   <cs:values>
     <cs:timestamp>2014-12-03T10:52:59.410Z</cs:timestamp>
     <cs:value cs:measurand="Current.Import" cs:unit="Amp">41.384</cs:value>
     <cs:value cs:measurand="Voltage" cs:unit="Volt">226.0</cs:value>
     <cs:value cs:measurand="Power.Active.Import" cs:unit="W">7018</cs:value>
     <cs:value cs:measurand="Energy.Active.Import.Register" cs:unit="Wh">2662</cs:value>
     <cs:value cs:measurand="Temperature" cs:unit="Celsius">24</cs:value>
   </cs:values>
 </cs:meterValuesRequest>
 */

using System;
using System.Globalization;
using System.Threading.Tasks;
using Serilog;
using Newtonsoft.Json;

using OCPP.Core.Library.Messages_OCPP16;

namespace OCPP.Core.Library
{
    public partial class ControllerOcpp16
    {
        public async Task<string> HandleMeterValues(OcppMessage msgIn, OcppMessage msgOut)
        {
            string errorCode = null;
            MeterValuesResponse meterValuesResponse = new MeterValuesResponse();

            int connectorId = -1;
            string msgMeterValue = string.Empty;

            try
            {
                Logger.Verbose("Processing meter values...");
                MeterValuesRequest? meterValueRequest = JsonConvert.DeserializeObject<MeterValuesRequest>(msgIn.JsonPayload);
                Logger.Verbose("MeterValues => Message deserialized");

                if (meterValueRequest == null)
                {
                    errorCode = "Meter value request is null";
                }
                else
                {
                    connectorId = meterValueRequest.ConnectorId;

                    if (ChargePointStatus != null)
                    {
                        // Known charge station => process meter values
                        double currentChargeKw = -1;
                        double meterKwh = -1;
                        DateTimeOffset? meterTime = null;
                        double stateOfCharge = -1;
                        foreach (MeterValue meterValue in meterValueRequest.MeterValue)
                        {
                            foreach (SampledValue sampleValue in meterValue.SampledValue)
                            {
                                Logger.Verbose("MeterValues => Context={0} / Format={1} / Value={2} / Unit={3} / Location={4} / Measurand={5} / Phase={6}",
                                    sampleValue.Context, sampleValue.Format, sampleValue.Value, sampleValue.Unit, sampleValue.Location, sampleValue.Measurand, sampleValue.Phase);

                                if (sampleValue.Measurand == SampledValueMeasurand.PowerActiveImport)
                                {
                                    // current charging power
                                    if (double.TryParse(sampleValue.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out currentChargeKw))
                                    {
                                        if (sampleValue.Unit == SampledValueUnit.W ||
                                            sampleValue.Unit == SampledValueUnit.Va ||
                                            sampleValue.Unit == SampledValueUnit.Var ||
                                            sampleValue.Unit == null)
                                        {
                                            Logger.Verbose("MeterValues => Charging '{0:0.0}' W", currentChargeKw);
                                            // convert W => kW
                                            currentChargeKw = currentChargeKw / 1000;
                                        }
                                        else if (sampleValue.Unit == SampledValueUnit.Kw ||
                                                sampleValue.Unit == SampledValueUnit.Kva ||
                                                sampleValue.Unit == SampledValueUnit.Kvar)
                                        {
                                            // already kW => OK
                                            Logger.Verbose("MeterValues => Charging '{0:0.0}' kW", currentChargeKw);
                                            currentChargeKw = currentChargeKw;
                                        }
                                        else
                                        {
                                            Logger.Warning("MeterValues => Charging: unexpected unit: '{0}' (Value={1})", sampleValue.Unit, sampleValue.Value);
                                        }
                                    }
                                    else
                                    {
                                        Logger.Error("MeterValues => Charging: invalid value '{0}' (Unit={1})", sampleValue.Value, sampleValue.Unit);
                                    }
                                }
                                else if (sampleValue.Measurand == SampledValueMeasurand.EnergyActiveImportRegister ||
                                        sampleValue.Measurand == null)
                                {
                                    // charged amount of energy
                                    if (double.TryParse(sampleValue.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out meterKwh))
                                    {
                                        if (sampleValue.Unit == SampledValueUnit.Wh ||
                                            sampleValue.Unit == SampledValueUnit.Varh ||
                                            sampleValue.Unit == null)
                                        {
                                            Logger.Verbose("MeterValues => Value: '{0:0.0}' Wh", meterKwh);
                                            // convert Wh => kWh
                                            meterKwh = meterKwh / 1000;
                                        }
                                        else if (sampleValue.Unit == SampledValueUnit.KWh ||
                                                sampleValue.Unit == SampledValueUnit.Kvarh)
                                        {
                                            // already kWh => OK
                                            Logger.Verbose("MeterValues => Value: '{0:0.0}' kWh", meterKwh);
                                        }
                                        else
                                        {
                                            Logger.Warning("MeterValues => Value: unexpected unit: '{0}' (Value={1})", sampleValue.Unit, sampleValue.Value);
                                        }
                                        meterTime = meterValue.Timestamp;
                                    }
                                    else
                                    {
                                        Logger.Error("MeterValues => Value: invalid value '{0}' (Unit={1})", sampleValue.Value, sampleValue.Unit);
                                    }
                                }
                                else if (sampleValue.Measurand == SampledValueMeasurand.SoC)
                                {
                                    // state of charge (battery status)
                                    if (double.TryParse(sampleValue.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out stateOfCharge))
                                    {
                                        Logger.Verbose("MeterValues => SoC: '{0:0.0}'%", stateOfCharge);
                                    }
                                    else
                                    {
                                        Logger.Error("MeterValues => invalid value '{0}' (SoC)", sampleValue.Value);
                                    }
                                }
                            }
                        }

                        // write charging/meter data in chargepoint status
                        if (connectorId > 0)
                        {
                            msgMeterValue = $"Meter (kWh): {meterKwh} | Charge (kW): {currentChargeKw} | SoC (%): {stateOfCharge}";

                            if (meterKwh >= 0)
                            {
                               await UpdateConnectorStatus(connectorId, null, null, meterKwh, meterTime);
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
