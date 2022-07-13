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
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;
using OCPP.Core.Database;
using OCPP.Core.Library.Messages_OCPP20;

namespace OCPP.Core.Library
{
    public partial class ControllerOcpp20 : ControllerBase
    {
        public const string VendorId = "dallmann consulting GmbH";

        /// <summary>
        /// Constructor
        /// </summary>
        public ControllerOcpp20(IConfiguration config, ILogger logger,OcppCoreContext dbContext) :
            base(config, logger, dbContext)
        {
            Logger = logger;
        }

        /// <summary>
        /// Processes the charge point message and returns the answer message
        /// </summary>
        public async Task<OcppMessage> ProcessRequest(OcppMessage msgIn)
        {
            OcppMessage msgOut = new OcppMessage();
            msgOut.MessageType = "3";
            msgOut.UniqueId = msgIn.UniqueId;

            string? errorCode = null;

            if (msgIn.MessageType == "2")
            {
                switch (msgIn.Action)
                {
                    case "BootNotification":
                        errorCode = HandleBootNotification(msgIn, msgOut);
                        break;

                    case "Heartbeat":
                        errorCode = HandleHeartBeat(msgIn, msgOut);
                        break;

                    case "Authorize":
                        errorCode = HandleAuthorize(msgIn, msgOut);
                        break;

                    case "TransactionEvent":
                        errorCode = HandleTransactionEvent(msgIn, msgOut);
                        break;

                    case "MeterValues":
                        errorCode = HandleMeterValues(msgIn, msgOut);
                        break;

                    case "StatusNotification":
                        errorCode = await HandleStatusNotification(msgIn, msgOut);
                        break;

                    case "DataTransfer":
                        errorCode = HandleDataTransfer(msgIn, msgOut);
                        break;

                    case "LogStatusNotification":
                        errorCode = HandleLogStatusNotification(msgIn, msgOut);
                        break;

                    case "FirmwareStatusNotification":
                        errorCode = HandleFirmwareStatusNotification(msgIn, msgOut);
                        break;

                    case "ClearedChargingLimit":
                        errorCode = HandleClearedChargingLimit(msgIn, msgOut);
                        break;

                    case "NotifyChargingLimit":
                        errorCode = HandleNotifyChargingLimit(msgIn, msgOut);
                        break;

                    case "NotifyEVChargingSchedule":
                        errorCode = HandleNotifyEvChargingSchedule(msgIn, msgOut);
                        break;

                    default:
                        errorCode = ErrorCodes.NotSupported;
                        WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, msgIn.JsonPayload, errorCode);
                        break;
                }
            }
            else
            {
                Logger.Error("ControllerOCPP20 => Protocol error: wrong message type", msgIn.MessageType);
                errorCode = ErrorCodes.ProtocolError;
            }

            if (!string.IsNullOrEmpty(errorCode))
            {
                // Inavlid message type => return type "4" (CALLERROR)
                msgOut.MessageType = "4";
                msgOut.ErrorCode = errorCode;
                Logger.Debug("ControllerOCPP20 => Return error code messge: ErrorCode={0}", errorCode);
            }

            return msgOut;
        }

        /// <summary>
        /// Processes the charge point message and returns the answer message
        /// </summary>
        public void ProcessAnswer(OcppMessage msgIn, OcppMessage msgOut)
        {
            // The response (msgIn) has no action => check action in original request (msgOut)
            switch (msgOut.Action)
            {
                case "Reset":
                    HandleReset(msgIn, msgOut);
                    break;

                case "UnlockConnector":
                    HandleUnlockConnector(msgIn, msgOut);
                    break;

                default:
                    WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, msgIn.JsonPayload, "Unknown answer");
                    break;
            }
        }

        /// <summary>
        /// Helper function for writing a log entry in database
        /// </summary>
        private bool WriteMessageLog(uint chargePointId, int? connectorId, string message, string result, string? errorCode)
        {
            try
            {
                int dbMessageLog = Configuration.GetValue<int>("DbMessageLog", 0);
                if (dbMessageLog > 0 && chargePointId > 0)
                {
                    bool doLog = (dbMessageLog > 1 ||
                                    (message != "BootNotification" &&
                                     message != "Heartbeat" &&
                                     message != "DataTransfer" &&
                                     message != "StatusNotification"));

                    if (doLog)
                    {
                        using (var dbContext = DbContext)
                        {
                            MessageLog msgLog = new MessageLog();
                            msgLog.ChargePointId = chargePointId ;
                            msgLog.ConnectorId = connectorId;
                            msgLog.LogTime = DateTime.UtcNow;
                            msgLog.Message = message;
                            msgLog.Result = result;
                            msgLog.ErrorCode = errorCode;
                            dbContext.MessageLogs.Add(msgLog);
                            Logger.Verbose("MessageLog => Writing entry '{0}'", message);
                            dbContext.SaveChanges();
                        }
                        return true;
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp, "MessageLog => Error writing entry '{0}'", message);
            }
            return false;
        }
    }
}
