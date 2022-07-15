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
using OCPP.Core.Library.Messages_OCPP16.OICP;

namespace OCPP.Core.Library
{
    public partial class ControllerOcpp16
    {
        private async Task<string?> HandleStopTransaction(OcppMessage msgIn, OcppMessage msgOut)
        {
            string? errorCode = null;
            var stopTransactionResponse = new StopTransactionResponse();

            try
            {
                if (msgIn.JsonPayload != null)
                {
                    var stopTransactionRequest =
                        JsonConvert.DeserializeObject<StopTransactionRequest>(msgIn.JsonPayload);
                    if (stopTransactionRequest == null)
                    {
                        errorCode = "InvalidPayload, Transaction cannot be started";
                        return errorCode;
                    }
                    else
                    {
                        var tagId = CleanChargeTagId(stopTransactionRequest.IdTag, Logger);

                        var transactionId = stopTransactionRequest.TransactionId;

                        var chargeTag = ChargeTag.IsValid(DbContext, tagId);
                        var cpTagAccess = CpTagAccess.IsValid(DbContext, chargeTag.Id, ChargePointStatus.Id);


                        switch (cpTagAccess.GetChargeTagStatus())
                        {
                            case ChargeTagStatus.ConcurrentTx:
                            case ChargeTagStatus.Accepted:
                                // Check if the transaction is already started
                                // If it is, return the transaction id
                                stopTransactionResponse= await CompleteTransaction(transactionId, stopTransactionRequest, chargeTag,
                                    cpTagAccess);
                                break;
                            case ChargeTagStatus.Blocked:
                            case ChargeTagStatus.Expired:
                            case ChargeTagStatus.Invalid:
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
                        msgOut.JsonPayload = JsonConvert.SerializeObject(stopTransactionResponse);
                        WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, stopTransactionResponse.IdTagInfo.Status.ToString(), errorCode);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in HandleStopTransaction");
                errorCode = "Internal Server Error";
            }

            
            return errorCode;
        }


        private async Task<StopTransactionResponse> CompleteTransaction(int transactionId,StopTransactionRequest stopTransactionRequest,ChargeTag chargeTag, CpTagAccess cpTagAccess)
        {
            var transaction = Queryable.LastOrDefault<Transaction>(DbContext.Transactions, x=>x.TransactionId == transactionId);
            var stopTransactionResponse = new StopTransactionResponse();
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
