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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Library.Messages_OCPP16;

namespace OCPP.Core.Library
{
    public partial class ControllerOcpp16
    {
        public async Task<string?> HandleStartTransaction(OcppMessage msgIn, OcppMessage msgOut)
        {
            string? errorCode = null;
            StartTransactionResponse startTransactionResponse;
            
            int connectorId = -1;

            try
            {
                StartTransactionRequest? str = JsonConvert.DeserializeObject<StartTransactionRequest>(msgIn.JsonPayload);
                if (str == null)
                {
                    errorCode = "InvalidPayload, Transaction cannot be started";
                    return errorCode;
                }
                else
                {
                    //Process Start Trasaction Request
                
                
                    connectorId = str.ConnectorId;
                    var tagId = CleanChargeTagId(str?.IdTag, Logger);

                    //first check if tag is valid and authorized to start a transaction

                    ChargeTag chargeTag = ChargeTag.IsValid(DbContext,tagId);

                    CpTagAccess cpTagAccess = CpTagAccess.IsValid(DbContext, chargeTag.Id, ChargePointStatus.Id);

                    switch (cpTagAccess.GetChargeTagStatus())
                    {
                        case ChargeTagStatus.Accepted:
                            //start new transaction
                            startTransactionResponse = await StartTransaction(connectorId,str,chargeTag,cpTagAccess);
                            break;
                        case ChargeTagStatus.ConcurrentTx:
                            // Check if the transaction is already started
                            // If it is, return the transaction id
                            startTransactionResponse = await ConcurrentTransaction(connectorId,str,chargeTag,cpTagAccess);
                            break;
                        case ChargeTagStatus.Blocked:
                            
                            startTransactionResponse = new StartTransactionResponse
                            {
                                IdTagInfo = new IdTagInfo
                                {
                                    Status = IdTagInfoStatus.Blocked,
                                    ExpiryDate = cpTagAccess.Expiry??DateTime.Now.AddMinutes(5),
                                    ParentIdTag = chargeTag.ParentTagId.ToString()??string.Empty
                                },
                                TransactionId = -1
                            };
                            errorCode = "Blocked Tag! This tag is blocked and cannot be used to start a transaction";
                            break;
                        case ChargeTagStatus.Expired:
                            // Check if the transaction is already started
                            startTransactionResponse = new StartTransactionResponse
                            {
                                IdTagInfo = new IdTagInfo
                                {
                                    Status = IdTagInfoStatus.Expired,
                                    ExpiryDate = cpTagAccess.Expiry??DateTime.Now.AddMinutes(5),
                                    ParentIdTag = chargeTag.ParentTagId.ToString()??string.Empty
                                },
                                TransactionId = -1
                            };
                            errorCode = "Tag Expired! Tag is no longer valid for charging and cannot be used";
                            break;
                        
                        case ChargeTagStatus.Invalid:
                            default:
                            startTransactionResponse = new StartTransactionResponse
                            {
                                IdTagInfo = new IdTagInfo
                                {
                                    Status = IdTagInfoStatus.Expired,
                                    ExpiryDate = cpTagAccess.Expiry??DateTime.Now.AddMinutes(5),
                                    ParentIdTag = chargeTag.ParentTagId.ToString()??string.Empty
                                },
                                TransactionId = -1
                            };
                            errorCode = "Invalid Tag! Either tha tag is not valid or not authorized to start a transaction";
                            break;
                    }

                }
                WriteMessageLog(ChargePointStatus.Id, connectorId, msgIn.Action, startTransactionResponse.IdTagInfo.Status.ToString(), errorCode);
                return string.Empty;
            }
            catch (Exception exp)
            {
                Logger.Error(exp, "StartTransaction => Exception: {Message}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }
            return errorCode;
        }
  
    
        private async Task<StartTransactionResponse> StartTransaction(int connectorId,StartTransactionRequest str ,ChargeTag chargeTag, CpTagAccess cpTagAccess )
        {
            StartTransactionResponse startTransactionResponse = new StartTransactionResponse();
             //Start Transaction
            UpdateConnectorStatus(connectorId, ConnectorStatusEnum.Occupied.ToString(), str.Timestamp, (double)str.MeterStart / 1000, str.Timestamp);
            Transaction transaction = new Transaction();
            transaction.ChargePointId = ChargePointStatus.Id;
            transaction.ConnectorId = connectorId;
            transaction.MeterStart = (float) ((double)str.MeterStart / 1000);;
            transaction.StartTagId = chargeTag.Id;
            transaction.StartTime = str.Timestamp.DateTime;
            
            transaction.TransactionStatus = TransactionStatus.Started.ToString();
            DbContext.Transactions.Add(transaction);
            
            cpTagAccess.CpTagStatus = ChargeTagStatus.ConcurrentTx.ToString();
            cpTagAccess.Timestamp = str.Timestamp.DateTime;
            DbContext.CpTagAccesses.Update(cpTagAccess);
            
            chargeTag.TagStatus = ChargeTagStatus.ConcurrentTx.ToString();
            DbContext.ChargeTags.Update(chargeTag);
            
            await DbContext.SaveChangesAsync();
            
            UpdateConnectorStatus(connectorId, ConnectorStatusEnum.Occupied.ToString(), str.Timestamp, (double)str.MeterStart / 1000, str.Timestamp);
            
            startTransactionResponse.TransactionId = transaction.TransactionId;
            
            var idTagInfo = new IdTagInfo
            {
                Status = IdTagInfoStatus.Accepted,
                ExpiryDate = cpTagAccess.Expiry??DateTime.Now.AddMinutes(5),
                ParentIdTag = chargeTag.ParentTagId.ToString()??string.Empty
            };
            startTransactionResponse.IdTagInfo = idTagInfo;
            
            return startTransactionResponse; 
        }

        private async Task<StartTransactionResponse> ConcurrentTransaction(int connectorId, StartTransactionRequest str, ChargeTag ct,
            CpTagAccess cpTagAccess)
        {
            var currentTx = DbContext.Transactions.LastOrDefault(x=>x.ChargePointId == ChargePointStatus.Id && x.ConnectorId==connectorId && x.StartTagId == ct.Id && x.TransactionStatus == nameof(TransactionStatus.Started));
            if (currentTx != null)
            {
                UpdateConnectorStatus(connectorId, ConnectorStatusEnum.Occupied.ToString(), str.Timestamp, (double)str.MeterStart / 1000, str.Timestamp);
                IdTagInfo idTagInfo = new IdTagInfo
                {
                    Status = IdTagInfoStatus.ConcurrentTx,
                    ExpiryDate = cpTagAccess.Expiry??DateTime.Now.AddMinutes(5),
                    ParentIdTag = ct.ParentTagId.ToString()??string.Empty
                };
                var startTransactionResponse = new StartTransactionResponse
                {
                    IdTagInfo = idTagInfo,
                    TransactionId = currentTx.TransactionId
                };
                return startTransactionResponse;
            }
            else
            {
                currentTx = DbContext.Transactions.LastOrDefault(x=>x.ConnectorId==connectorId && x.ChargePointId == ChargePointStatus.Id);

                if (currentTx != null)
                {
                    if (currentTx.TransactionStatus != nameof(TransactionStatus.Started))
                    {
                        return await StartTransaction(connectorId, str, ct, cpTagAccess);
                    }
                }

                IdTagInfo idTagInfo = new IdTagInfo
                {
                    Status = IdTagInfoStatus.Invalid,
                    ExpiryDate = cpTagAccess.Expiry??DateTime.Now.AddMinutes(5),
                    ParentIdTag = ct.ParentTagId.ToString()??string.Empty
                };
                var startTransactionResponse = new StartTransactionResponse
                {
                    IdTagInfo = idTagInfo,
                    TransactionId = -1
                };
                return startTransactionResponse;
            }
        }
    }
}
