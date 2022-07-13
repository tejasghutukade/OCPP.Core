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
using Microsoft.AspNetCore.Http;
using Serilog;
using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Library.Messages_OCPP16;

namespace OCPP.Core.Library
{
    public partial class ControllerOcpp16
    {
        public async Task HandleReset(OcppMessage msgIn, OcppMessage msgOut)
        {
            Logger.Information("Reset answer: ChargePointId={0} / MsgType={1} / ErrCode={2}", ChargePointStatus.Id, msgIn.MessageType, msgIn.ErrorCode);

            try
            {
                ResetResponse? resetResponse = JsonConvert.DeserializeObject<ResetResponse>(msgIn.JsonPayload);
                
                WriteMessageLog(ChargePointStatus.Id, null, msgOut.Action, resetResponse?.Status.ToString()??"", msgIn.ErrorCode);

                if (resetResponse == null) return;
        
                var sendRequest = DbContext.SendRequests.Where(x => x.Uid == msgIn.UniqueId).FirstOrDefault();
                if (sendRequest == null) return ;


                switch (resetResponse.Status)
                {
                    case ResetResponseStatus.Accepted:
                        sendRequest.Status = nameof(SendRequestStatus.Completed);
                        break;
                    case ResetResponseStatus.Rejected:
                        default:
                        sendRequest.Status = nameof(SendRequestStatus.Failed);
                        break;
                    
                }
                
                DbContext.SendRequests.Update(sendRequest);
                await DbContext.SaveChangesAsync();

                
                if (msgOut.TaskCompletionSource != null)
                {
                    // Set API response as TaskCompletion-result
                    string apiResult = "{\"status\": " + JsonConvert.ToString(resetResponse.Status.ToString()) + "}";
                    Logger.Verbose("HandleReset => API response: {0}" , apiResult);

                    msgOut.TaskCompletionSource.SetResult(apiResult);
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp, "HandleReset => Exception: {0}", exp.Message);
            }
        }
    }
}
