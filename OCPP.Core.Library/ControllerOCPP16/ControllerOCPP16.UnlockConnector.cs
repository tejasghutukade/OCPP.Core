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
using OCPP.Core.Database;
using OCPP.Core.Library.Messages_OCPP16.OICS;

namespace OCPP.Core.Library
{
    public partial class ControllerOcpp16
    {
        public async Task HandleUnlockConnector(OcppMessage msgIn, OcppMessage msgOut)
        {
            Logger.Information("UnlockConnector answer: ChargePointId={0} / MsgType={1} / ErrCode={2}", ChargePointStatus.Id, msgIn.MessageType, msgIn.ErrorCode);

            try
            {
                UnlockConnectorResponse unlockConnectorResponse = JsonConvert.DeserializeObject<UnlockConnectorResponse>(msgIn.JsonPayload);
                Logger.Information("HandleUnlockConnector => Answer status: {0}", unlockConnectorResponse?.Status);
                WriteMessageLog(ChargePointStatus.Id, null, msgOut.Action, unlockConnectorResponse?.Status.ToString(), msgIn.ErrorCode);
                
                if (unlockConnectorResponse == null) return;
        
                var sendRequest = Queryable.Where<SendRequest>(DbContext.SendRequests, x => x.Uid == msgIn.UniqueId).FirstOrDefault();
                if (sendRequest == null) return ;


                switch (unlockConnectorResponse.Status)
                {
                    case UnlockConnectorResponseStatus.Unlocked:
                        sendRequest.Status = nameof(SendRequestStatus.Completed);
                        break;
                    case UnlockConnectorResponseStatus.UnlockFailed:
                    case UnlockConnectorResponseStatus.NotSupported:
                    default:
                        sendRequest.Status = nameof(SendRequestStatus.Failed);
                        break;
                    
                }
                
                DbContext.SendRequests.Update(sendRequest);
                await DbContext.SaveChangesAsync();
                
                if (msgOut.TaskCompletionSource != null)
                {
                    // Set API response as TaskCompletion-result
                    string apiResult = "{\"status\": " + JsonConvert.ToString(unlockConnectorResponse.Status.ToString()) + "}";
                    Logger.Verbose("HandleUnlockConnector => API response: {0}", apiResult);

                    msgOut.TaskCompletionSource.SetResult(apiResult);
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp, "HandleUnlockConnector => Exception: {0}", exp.Message);
            }
        }
    }
}