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
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using OCPP.Core.Database;
using Newtonsoft.Json;

using OCPP.Core.Library.Messages_OCPP16;

namespace OCPP.Core.Library
{
    public partial class ControllerOcpp16
    {
        public async Task<string?> HandleStopTransaction(OcppMessage msgIn, OcppMessage msgOut)
        {
            string? errorCode = null;
            var stopTransactionResponse = new StopTransactionResponse();

            try
            {

                StopTransactionRequest? stopTransactionRequest =
                    JsonConvert.DeserializeObject<StopTransactionRequest>(msgIn.JsonPayload);
                if (stopTransactionRequest == null)
                {
                    errorCode = "InvalidPayload, Transaction cannot be started";
                    return errorCode;
                }
                else
                {
                    string? tagId = CleanChargeTagId(stopTransactionRequest.IdTag, Logger);

                    int transactionId = stopTransactionRequest.TransactionId;

                    ChargeTag chargeTag = ChargeTag.IsValid(DbContext, tagId);
                    CpTagAccess cpTagAccess = CpTagAccess.IsValid(DbContext, chargeTag.Id, ChargePointStatus.Id);


                    switch (cpTagAccess.GetChargeTagStatus())
                    {
                        case ChargeTagStatus.ConcurrentTx:
                        case ChargeTagStatus.Accepted:
                            // Check if the transaction is already started
                            // If it is, return the transaction id
                            stopTransactionResponse= await CompleteTransaction(transactionId, stopTransactionRequest, chargeTag,
                                cpTagAccess);

                            break;
                        default:
                            stopTransactionResponse.IdTagInfo = new IdTagInfo()
                            {
                                Status = Enum.TryParse<IdTagInfoStatus>(cpTagAccess.CpTagStatus, out var idTagInfoStatus)
                                    ? idTagInfoStatus
                                    : IdTagInfoStatus.Invalid,
                                ExpiryDate = cpTagAccess.Expiry ?? DateTime.Now.AddMinutes(-5),
                                ParentIdTag = chargeTag.ParentTagId.ToString() ?? string.Empty

                            };
                            errorCode =
                                "Invalid Blocked or Expired Tag! Either tha tag is not valid or not authorized to start a transaction";
                            break;
                    }
                }

                
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in HandleStopTransaction");
                errorCode = "Internal Server Error";
            }

            WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, stopTransactionResponse.IdTagInfo.Status.ToString(), errorCode);
            return errorCode;
        }


        private async Task<StopTransactionResponse> CompleteTransaction(int transactionId,StopTransactionRequest stopTransactionRequest,ChargeTag chargeTag, CpTagAccess cpTagAccess)
        {
            var transaction = DbContext.Transactions.LastOrDefault(x=>x.TransactionId == transactionId);
            StopTransactionResponse stopTransactionResponse = new StopTransactionResponse();
            if (transaction != null)
            {
                transaction.MeterStop = (float) (double) stopTransactionRequest.MeterStop / 1000;
                transaction.StopTime = stopTransactionRequest.Timestamp.DateTime;
                transaction.TransactionStatus = nameof(TransactionStatus.Completed);
                transaction.StopReason = nameof(stopTransactionRequest.Reason);
                transaction.TransactionData = JsonConvert.SerializeObject(stopTransactionRequest.TransactionData);
                DbContext.Transactions.Update(transaction);
                            
                cpTagAccess.CpTagStatus = nameof(ChargeTagStatus.Accepted);
                DbContext.CpTagAccesses.Update(cpTagAccess);
                chargeTag.TagStatus = nameof(ChargeTagStatus.Accepted);
                DbContext.ChargeTags.Update(chargeTag);
                await DbContext.SaveChangesAsync();
                
                stopTransactionResponse.IdTagInfo = new IdTagInfo()
                {
                    Status = IdTagInfoStatus.Accepted,
                    ExpiryDate = cpTagAccess.Expiry??DateTime.Now.AddMinutes(5),
                    ParentIdTag = chargeTag.ParentTagId.ToString()??string.Empty
                    
                };
                return stopTransactionResponse;
            }
            else
            {
                stopTransactionResponse.IdTagInfo = new IdTagInfo()
                {
                    Status = IdTagInfoStatus.Invalid,
                    ExpiryDate = cpTagAccess.Expiry??DateTime.Now.AddMinutes(-5),
                    ParentIdTag = chargeTag.ParentTagId.ToString()??string.Empty
                    
                };
                return stopTransactionResponse;
                
            }
        }
    }
}
