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

using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Library.Messages_OCPP20;

namespace OCPP.Core.Library
{
    public partial class ControllerOcpp20
    {
        public string? HandleTransactionEvent(OcppMessage msgIn, OcppMessage msgOut)
        {
            string? errorCode = null;
            TransactionEventResponse transactionEventResponse = new TransactionEventResponse();
            transactionEventResponse.CustomData = new CustomDataType();
            transactionEventResponse.CustomData.VendorId = VendorId;
            transactionEventResponse.IdTokenInfo = new IdTokenInfoType();

            int connectorId = 0;

            try
            {
                Logger.Verbose("TransactionEvent => Processing transactionEvent request...");
                TransactionEventRequest? transactionEventRequest = JsonConvert.DeserializeObject<TransactionEventRequest>(msgIn.JsonPayload);
                Logger.Verbose("TransactionEvent => Message deserialized");

                string? idTag = CleanChargeTagId(transactionEventRequest?.IdToken?.IdToken, Logger);
                connectorId = (true) ? transactionEventRequest.Evse.ConnectorId : 0;


                //  Extract meter values with correct scale
                double currentChargeKw = -1;
                double meterKwh = -1;
                DateTimeOffset? meterTime = null;
                double stateOfCharge = -1;
                GetMeterValues(transactionEventRequest.MeterValue, out meterKwh, out currentChargeKw, out stateOfCharge, out meterTime);

                if (connectorId > 0 && meterKwh >= 0)
                {
                    UpdateConnectorStatus(connectorId, null, null, meterKwh, meterTime);
                }

                if (transactionEventRequest.EventType == TransactionEventEnumType.Started)
                {
                    try
                    {
                        #region Start Transaction
                        using (var dbContext = DbContext)
                        {
                            if (string.IsNullOrWhiteSpace(idTag))
                            {
                                // no RFID-Tag => accept request
                                transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
                                Logger.Information("StartTransaction => no charge tag => accepted");
                            }
                            else
                            {
                                ChargeTag? ct = dbContext.Find<ChargeTag>(idTag);
                                if (ct != null)
                                {
                                    if (Enum.TryParse(ct.TagStatus, out ChargeTagStatus status) && status.Equals(ChargeTagStatus.Blocked))
                                    {
                                        Logger.Information("StartTransaction => Tag '{1}' blocked)", idTag);
                                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Blocked;
                                    }
                                    else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                                    {
                                        Logger.Information("StartTransaction => Tag '{1}' expired)", idTag);
                                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Expired;
                                    }
                                    else
                                    {
                                        Logger.Information("StartTransaction => Tag '{1}' accepted)", idTag);
                                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
                                    }
                                }
                                else
                                {
                                    Logger.Information("StartTransaction => Tag '{1}' unknown)", idTag);
                                    transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Unknown;
                                }
                            }

                            if (transactionEventResponse.IdTokenInfo.Status == AuthorizationStatusEnumType.Accepted)
                            {
                                UpdateConnectorStatus(connectorId, ConnectorStatusEnum.Occupied.ToString(), meterTime, null, null);

                                try
                                {
                                    Logger.Information("StartTransaction => Meter='{0}' (kWh)", meterKwh);

                                    Transaction transaction = new Transaction();
                                    transaction.Uid = transactionEventRequest.TransactionInfo.TransactionId;
                                    transaction.ChargePointId = ChargePointStatus.Id;
                                    transaction.ConnectorId = connectorId;
                                    //transaction.StartTagId = idTag;
                                    transaction.StartTime = transactionEventRequest.Timestamp.UtcDateTime;
                                    transaction.MeterStart = (float) meterKwh;
                                    transaction.StartResult = transactionEventRequest.TriggerReason.ToString();
                                    dbContext.Add<Transaction>(transaction);

                                    dbContext.SaveChanges();
                                }
                                catch (Exception exp)
                                {
                                    Logger.Error(exp, "StartTransaction => Exception writing transaction: chargepoint={0} / tag={1}", ChargePointStatus?.Id, idTag);
                                    errorCode = ErrorCodes.InternalError;
                                }
                            }
                        }
                        #endregion
                    }
                    catch (Exception exp)
                    {
                        Logger.Error(exp, "StartTransaction => Exception: {0}", exp.Message);
                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
                    }
                }
                else if (transactionEventRequest.EventType == TransactionEventEnumType.Updated)
                {
                    try
                    {
                        #region Update Transaction
                        using (var dbContext = DbContext)
                        {
                            Transaction transaction = Queryable.Where<Transaction>(dbContext.Transactions, t => t.Uid == transactionEventRequest.TransactionInfo.TransactionId)
                                .OrderByDescending(t => t.TransactionId)
                                .FirstOrDefault();
                            if (transaction == null ||
                                transaction.ChargePointId != ChargePointStatus.Id ||
                                transaction.StopTime.HasValue)
                            {
                                // unknown transaction id or already stopped transaction
                                // => find latest transaction for the charge point and check if its open
                                Logger.Warning("UpdateTransaction => Unknown or closed transaction uid={0}", transactionEventRequest.TransactionInfo?.TransactionId);
                                // find latest transaction for this charge point
                                transaction = Queryable.Where<Transaction>(dbContext.Transactions, t => t.ChargePointId == ChargePointStatus.Id && t.ConnectorId == connectorId)
                                    .OrderByDescending(t => t.TransactionId)
                                    .FirstOrDefault();

                                if (transaction != null)
                                {
                                    Logger.Verbose("UpdateTransaction => Last transaction id={0} / Start='{1}' / Stop='{2}'", transaction.TransactionId, transaction.StartTime.ToString("O"), transaction?.StopTime?.ToString("O"));
                                    if (transaction.StopTime.HasValue)
                                    {
                                        Logger.Verbose("UpdateTransaction => Last transaction (id={0}) is already closed ", transaction.TransactionId);
                                        transaction = null;
                                    }
                                }
                                else
                                {
                                    Logger.Verbose("UpdateTransaction => Found no transaction for charge point '{0}' and connectorId '{1}'", ChargePointStatus.Id, connectorId);
                                }
                            }

                            if (transaction != null)
                            {
                                // write current meter value in "stop" value
                                if (meterKwh >= 0)
                                {
                                    Logger.Information("UpdateTransaction => Meter='{0}' (kWh)", meterKwh);
                                    transaction.MeterStop = (float?) meterKwh;
                                    dbContext.SaveChanges();
                                }
                            }
                            else
                            {
                                Logger.Error("UpdateTransaction => Unknown transaction: uid='{0}' / chargepoint='{1}' / tag={2}", transactionEventRequest.TransactionInfo?.TransactionId, ChargePointStatus?.Id, idTag);
                                WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, string.Format("UnknownTransaction:UID={0}/Meter={1}", transactionEventRequest.TransactionInfo?.TransactionId, GetMeterValue(transactionEventRequest.MeterValue)), errorCode);
                                errorCode = ErrorCodes.PropertyConstraintViolation;
                            }
                        }
                        #endregion
                    }
                    catch (Exception exp)
                    {
                        Logger.Error(exp, "UpdateTransaction => Exception: {0}", exp.Message);
                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
                    }
                }
                else if (transactionEventRequest.EventType == TransactionEventEnumType.Ended)
                {
                    try
                    {
                        #region End Transaction
                        using (var dbContext =DbContext)
                        {
                            ChargeTag ct = null;

                            if (string.IsNullOrWhiteSpace(idTag))
                            {
                                // no RFID-Tag => accept request
                                transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
                                Logger.Information("EndTransaction => no charge tag => accepted");
                            }
                            else
                            {
                                ct = dbContext.Find<ChargeTag>(idTag);
                                if (ct != null)
                                {
                                    if (Enum.TryParse(ct.TagStatus, out ChargeTagStatus status) && status.Equals(ChargeTagStatus.Blocked))
                                    {
                                        Logger.Information("EndTransaction => Tag '{1}' blocked)", idTag);
                                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Blocked;
                                    }
                                    else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                                    {
                                        Logger.Information("EndTransaction => Tag '{1}' expired)", idTag);
                                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Expired;
                                    }
                                    else
                                    {
                                        Logger.Information("EndTransaction => Tag '{1}' accepted)", idTag);
                                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
                                    }
                                }
                                else
                                {
                                    Logger.Information("EndTransaction => Tag '{1}' unknown)", idTag);
                                    transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Unknown;
                                }
                            }

                            Transaction transaction = Queryable.Where<Transaction>(dbContext.Transactions, t => t.Uid == transactionEventRequest.TransactionInfo.TransactionId)
                                .OrderByDescending(t => t.TransactionId)
                                .FirstOrDefault();
                            if (transaction == null ||
                                transaction.ChargePointId != ChargePointStatus.Id ||
                                transaction.StopTime.HasValue)
                            {
                                // unknown transaction id or already stopped transaction
                                // => find latest transaction for the charge point and check if its open
                                Logger.Warning("EndTransaction => Unknown or closed transaction uid={0}", transactionEventRequest.TransactionInfo?.TransactionId);
                                // find latest transaction for this charge point
                                transaction = Queryable.Where<Transaction>(dbContext.Transactions, t => t.ChargePointId == ChargePointStatus.Id && t.ConnectorId == connectorId)
                                    .OrderByDescending(t => t.TransactionId)
                                    .FirstOrDefault();

                                if (transaction != null)
                                {
                                    Logger.Verbose("EndTransaction => Last transaction id={0} / Start='{1}' / Stop='{2}'", transaction.TransactionId, transaction.StartTime.ToString("O"), transaction?.StopTime?.ToString("O"));
                                    if (transaction.StopTime.HasValue)
                                    {
                                        Logger.Verbose("EndTransaction => Last transaction (id={0}) is already closed ", transaction.TransactionId);
                                        transaction = null;
                                    }
                                }
                                else
                                {
                                    Logger.Verbose("EndTransaction => Found no transaction for charge point '{0}' and connectorId '{1}'", ChargePointStatus.Id, connectorId);
                                }
                            }

                            if (transaction != null)
                            {
                                // check current tag against start tag
                                bool valid = true;
                                if (!string.Equals(transaction.StartTagId.ToString(), idTag, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    // tags are different => same group?
                                    ChargeTag startTag = dbContext.Find<ChargeTag>(transaction.StartTagId);
                                    if (startTag != null)
                                    {
                                        if (!string.Equals(startTag.ParentTagId.ToString(), ct?.ParentTagId.ToString(), StringComparison.InvariantCultureIgnoreCase))
                                        {
                                            Logger.Information("EndTransaction => Start-Tag ('{0}') and End-Tag ('{1}') do not match: Invalid!", transaction.StartTagId, ct?.TagId);
                                            transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
                                            valid = false;
                                        }
                                        else
                                        {
                                            Logger.Information("EndTransaction => Different charge tags but matching group ('{0}')", ct?.ParentTagId);
                                        }
                                    }
                                    else
                                    {
                                        Logger.Error("EndTransaction => Start-Tag not found: '{0}'", transaction.StartTagId);
                                        // assume "valid" and allow to end the transaction
                                    }
                                }

                                if (valid)
                                {
                                    // write current meter value in "stop" value
                                    Logger.Information("EndTransaction => Meter='{0}' (kWh)", meterKwh);

                                    transaction.StopTime = transactionEventRequest.Timestamp.UtcDateTime;
                                    transaction.MeterStop = (float?) meterKwh;
                                    //transaction.StopTagId = idTag;
                                    transaction.StopReason = transactionEventRequest.TriggerReason.ToString();
                                    dbContext.SaveChanges();
                                }
                            }
                            else
                            {
                                Logger.Error("EndTransaction => Unknown transaction: uid='{0}' / chargepoint='{1}' / tag={2}", transactionEventRequest.TransactionInfo?.TransactionId, ChargePointStatus?.Id, idTag);
                                WriteMessageLog(ChargePointStatus.Id, connectorId, msgIn.Action, string.Format("UnknownTransaction:UID={0}/Meter={1}", transactionEventRequest.TransactionInfo?.TransactionId, GetMeterValue(transactionEventRequest.MeterValue)), errorCode);
                                errorCode = ErrorCodes.PropertyConstraintViolation;
                            }
                        }
                        #endregion
                    }
                    catch (Exception exp)
                    {
                        Logger.Error(exp, "EndTransaction => Exception: {0}", exp.Message);
                        transactionEventResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
                    }
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(transactionEventResponse);
                Logger.Verbose("TransactionEvent => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.Error(exp, "TransactionEvent => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus.Id, connectorId, msgIn.Action, transactionEventResponse.IdTokenInfo.Status.ToString(), errorCode);
            return errorCode;
        }


        /// <summary>
        /// Extract main meter value from collection
        /// </summary>
        private double GetMeterValue(ICollection<MeterValueType> meterValues)
        {
            double currentChargeKw = -1;
            double meterKwh = -1;
            DateTimeOffset? meterTime = null;
            double stateOfCharge = -1;
            GetMeterValues(meterValues, out meterKwh, out currentChargeKw, out stateOfCharge, out meterTime);

            return meterKwh;
        }

        /// <summary>
        /// Extract different meter values from collection
        /// </summary>
        private void GetMeterValues(ICollection<MeterValueType> meterValues, out double meterKwh, out double currentChargeKw, out double stateOfCharge, out DateTimeOffset? meterTime)
        {
            currentChargeKw = -1;
            meterKwh = -1;
            meterTime = null;
            stateOfCharge = -1;

            foreach (MeterValueType meterValue in meterValues)
            {
                foreach (SampledValueType sampleValue in meterValue.SampledValue)
                {
                    Logger.Verbose("GetMeterValues => Context={0} / SignedMeterValue={1} / Value={2} / Unit={3} / Location={4} / Measurand={5} / Phase={6}",
                        sampleValue.Context, sampleValue.SignedMeterValue, sampleValue.Value, sampleValue.UnitOfMeasure, sampleValue.Location, sampleValue.Measurand, sampleValue.Phase);

                    if (sampleValue.Measurand == MeasurandEnumType.PowerActiveImport)
                    {
                        // current charging power
                        currentChargeKw = sampleValue.Value;
                        if (sampleValue.UnitOfMeasure?.Unit == "W" ||
                            sampleValue.UnitOfMeasure?.Unit == "VA" ||
                            sampleValue.UnitOfMeasure?.Unit == "var" ||
                            sampleValue.UnitOfMeasure?.Unit == null ||
                            sampleValue.UnitOfMeasure == null)
                        {
                            Logger.Verbose("GetMeterValues => Charging '{0:0.0}' W", currentChargeKw);
                            // convert W => kW
                            currentChargeKw = currentChargeKw / 1000;
                        }
                        else if (sampleValue.UnitOfMeasure?.Unit == "KW" ||
                                sampleValue.UnitOfMeasure?.Unit == "kVA" ||
                                sampleValue.UnitOfMeasure?.Unit == "kvar")
                        {
                            // already kW => OK
                            Logger.Verbose("GetMeterValues => Charging '{0:0.0}' kW", currentChargeKw);
                        }
                        else
                        {
                            Logger.Warning("GetMeterValues => Charging: unexpected unit: '{0}' (Value={1})", sampleValue.UnitOfMeasure?.Unit, sampleValue.Value);
                        }
                    }
                    else if (sampleValue.Measurand == MeasurandEnumType.EnergyActiveImportRegister ||
                             sampleValue.Measurand == MeasurandEnumType.Missing)  // Spec: Default=Energy_Active_Import_Register
                    {
                        // charged amount of energy
                        meterKwh = sampleValue.Value;
                        if (sampleValue.UnitOfMeasure?.Unit == "Wh" ||
                            sampleValue.UnitOfMeasure?.Unit == "VAh" ||
                            sampleValue.UnitOfMeasure?.Unit == "varh" ||
                            (sampleValue.UnitOfMeasure == null || sampleValue.UnitOfMeasure.Unit == null))
                        {
                            Logger.Verbose("GetMeterValues => Value: '{0:0.0}' Wh", meterKwh);
                            // convert Wh => kWh
                            meterKwh = meterKwh / 1000;
                        }
                        else if (sampleValue.UnitOfMeasure?.Unit == "kWh" ||
                                sampleValue.UnitOfMeasure?.Unit == "kVAh" ||
                                sampleValue.UnitOfMeasure?.Unit == "kvarh")
                        {
                            // already kWh => OK
                            Logger.Verbose("GetMeterValues => Value: '{0:0.0}' kWh", meterKwh);
                        }
                        else
                        {
                            Logger.Warning("GetMeterValues => Value: unexpected unit: '{0}' (Value={1})", sampleValue.UnitOfMeasure?.Unit, sampleValue.Value);
                        }
                        meterTime = meterValue.Timestamp;
                    }
                    else if (sampleValue.Measurand == MeasurandEnumType.SoC)
                    {
                        // state of charge (battery status)
                        stateOfCharge = sampleValue.Value;
                        Logger.Verbose("GetMeterValues => SoC: '{0:0.0}'%", stateOfCharge);
                    }
                }
            }
        }
    }
}
