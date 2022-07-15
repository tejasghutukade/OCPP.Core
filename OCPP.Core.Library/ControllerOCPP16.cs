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


using Microsoft.Extensions.Configuration;
using OCPP.Core.Database;
using Serilog;
using OCPP.Core.Library.Messages_OCPP16;

namespace OCPP.Core.Library
{
    public partial class ControllerOcpp16 : ControllerBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        
        
        public ControllerOcpp16(IConfiguration config, ILogger logger, OcppCoreContext dbContext) :
            base(config, logger, dbContext)
        {
        }

        /// <summary>
        /// Processes the charge point message and returns the answer message
        /// </summary>
        public async Task<OcppMessage> ProcessRequest(OcppMessage msgIn)
        {
            var msgOut = new OcppMessage
            {
                MessageType = "3",
                UniqueId = msgIn.UniqueId
            };

            string? errorCode = null;

            switch (msgIn.Action)
            {
                case "BootNotification":
                    errorCode = await HandleBootNotification(msgIn, msgOut);
                    break;

                case "Heartbeat":
                    errorCode =await HandleHeartBeat(msgIn, msgOut);
                    break;

                case "Authorize":
                    errorCode = await HandleAuthorize(msgIn, msgOut);
                    break;

                case "StartTransaction":
                    errorCode = await HandleStartTransaction(msgIn, msgOut);
                    break;

                case "StopTransaction":
                    errorCode = await HandleStopTransaction(msgIn, msgOut);
                    break;

                case "MeterValues":
                    errorCode = await HandleMeterValues(msgIn, msgOut);
                    break;

                case "StatusNotification":
                    errorCode = await HandleStatusNotification(msgIn, msgOut);
                    break;

                case "DataTransfer":
                    errorCode = HandleDataTransfer(msgIn, msgOut);
                    break;
                default:
                    errorCode = ErrorCodes.NotSupported;
                    WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, msgIn.JsonPayload, errorCode);
                    break;
            }

            if (string.IsNullOrEmpty(errorCode)) return msgOut;
            // Invalid message type => return type "4" (CALL_ERROR)
            msgOut.MessageType = "4";
            msgOut.ErrorCode = errorCode;
            Logger.Debug("ControllerOCPP16 => Return error code messge: ErrorCode={ErrorCode}", errorCode);

            return msgOut;
        }


        /// <summary>
        /// Processes the charge point message and returns the answer message
        /// </summary>
        public async void ProcessAnswer(OcppMessage msgIn, OcppMessage msgOut)
        {
            // The response (msgIn) has no action => check action in original request (msgOut)
            switch (msgOut.Action)
            {
                case "Reset":
                    await HandleReset(msgIn, msgOut);
                    break;
                case "UnlockConnector":
                    await HandleUnlockConnector(msgIn, msgOut);
                    break;
                case "RemoteStartTransaction":
                    await HandleRemoteStartTransaction(msgIn);
                    break;
                case "RemoteStopTransaction":
                    await HandleRemoteStopTransaction(msgIn);
                    break;
                case "CancelReservation":
                    await HandleCancelReservation(msgIn);
                    break;
                case "ChangeAvailability":
                    await HandleChangeAvailability(msgIn);
                    break;
                case "ChangeConfiguration":
                    await HandleChangeConfiguration(msgIn);
                    break;
                case "ClearCache":
                    await HandleClearCache(msgIn);
                    break;
                case "ClearChargingProfile":
                    await HandleClearChargingProfile(msgIn);
                    break;
                case "ReserveNow":
                    await HandleReserveNow(msgIn);
                    break;
                case "SetChargingProfile":
                    await HandleSetChargingProfile(msgIn);
                    break;
                case "TriggerMessage":
                    await HandleTriggerMessage(msgIn);
                    break;
                case "UpdateFirmware":
                    await HandleUpdateFirmware(msgIn);
                    break;
                default:
                    WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, msgIn.JsonPayload, "Unknown answer");
                    break;
            }
        }

        /// <summary>
        /// Fetch the Charging Station Messages and send the messages to the Charge Point
        /// </summary>
        public List<OccMessageSendRequest>?  FetchRequestForChargePoint()
        {
            List<OccMessageSendRequest> messages = new List<OccMessageSendRequest>();
            //ChargePointStatus.
            var requests = DbContext.SendRequests.Where(x => x.Status == nameof(SendRequestStatus.Queued) && x.ChargePointId == ChargePointStatus.Id);
            if (!requests.Any()) return null;

            foreach (var request in requests)
            {
                OcppMessage msgOut = new OcppMessage();
                msgOut.MessageType = "2";
                msgOut.UniqueId = request.Uid;
                msgOut.Action = request.RequestType;
                msgOut.JsonPayload = request.RequestPayload??string.Empty;
                
                OccMessageSendRequest sendRequest = new OccMessageSendRequest(msgOut,request);
                messages.Add(sendRequest);
            }

            return messages;
        }

        public async Task UpdateSendRequestStatus(SendRequest request)
        {
            DbContext.SendRequests.Update(request);
            await DbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Helper function for writing a log entry in database
        /// </summary>
        private bool WriteMessageLog(uint chargePointId, int? connectorId, string message, string? result, string? errorCode)
        {
            try
            {
                var dbMessageLog = Configuration.GetValue<int>("DbMessageLog", 0);
                if (dbMessageLog > 0 && chargePointId > 0)
                {
                    var doLog = (dbMessageLog > 1 ||
                                 (message != "BootNotification" &&
                                  message != "Heartbeat" &&
                                  message != "DataTransfer" &&
                                  message != "StatusNotification"));

                    if (doLog)
                    {
                        using var dbContext = DbContext;
                        var msgLog = new MessageLog
                        {
                            ChargePointId = chargePointId,
                            ConnectorId = connectorId,
                            LogTime = DateTime.UtcNow,
                            Message = message,
                            Result = result??string.Empty,
                            ErrorCode = errorCode
                        };
                        dbContext.MessageLogs.Add(msgLog);
                        Logger.Verbose("MessageLog => Writing entry '{Message}'", message);
                        dbContext.SaveChanges();
                        return true;
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp, "MessageLog => Error writing entry '{Message}'", message);
            }
            return false;
        }
        
        /// <summary>
        /// Check Connection status
        /// </summary>
        public void CheckOcppConnection()
        {
            
        }
    }
}
