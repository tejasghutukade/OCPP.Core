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
using Newtonsoft.Json;
using OCPP.Core.Database;
using OCPP.Core.Library.Messages_OCPP16;

namespace OCPP.Core.Library
{
    public partial class ControllerOcpp16
    {
        public async Task<string?> HandleAuthorize(OcppMessage msgIn, OcppMessage msgOut)
        {
            string? errorCode = null;
            AuthorizeResponse authorizeResponse = new AuthorizeResponse();
            authorizeResponse.IdTagInfo.Status = IdTagInfoStatus.Invalid;
            bool error = false;
            string? idTag = null;
            try
            {
                Logger.Verbose("Processing authorize request...");
                AuthorizeRequest? authorizeRequest = JsonConvert.DeserializeObject<AuthorizeRequest>(msgIn.JsonPayload);
                Logger.Verbose("Authorize => Message deserialized");
                idTag = CleanChargeTagId(authorizeRequest?.IdTag, Logger);
                
                
                IdTagInfoStatus chargeTagStatus;
                
                ChargeTag ct = ChargeTag.IsValid(DbContext,idTag);

                if (ct.GetChargeTagStatus() != ChargeTagStatus.Invalid)
                {
                    CpTagAccess cptagaccess = CpTagAccess.IsValid(DbContext, ct.Id, ChargePointStatus.Id);
                    if (cptagaccess.GetChargeTagStatus() != ChargeTagStatus.Invalid)
                    {
                        authorizeResponse.IdTagInfo.ParentIdTag = ct.ParentTagId.ToString()?? string.Empty;
                        authorizeResponse.IdTagInfo.ExpiryDate = cptagaccess.Expiry?? DateTimeOffset.UtcNow.AddMinutes(5);   // default: 5 minutes
                        authorizeResponse.IdTagInfo.Status = Enum.TryParse<IdTagInfoStatus>(cptagaccess.GetChargeTagStatus().ToString(), out chargeTagStatus) ? chargeTagStatus : IdTagInfoStatus.Invalid;
                        msgOut.JsonPayload = JsonConvert.SerializeObject(authorizeResponse);
                    }
                }


                authorizeResponse.IdTagInfo.ParentIdTag = string.Empty;
                authorizeResponse.IdTagInfo.ExpiryDate = DateTimeOffset.UtcNow.AddMinutes(-5);   // default: 5 minutes
                authorizeResponse.IdTagInfo.Status = IdTagInfoStatus.Invalid;
                msgOut.JsonPayload = JsonConvert.SerializeObject(authorizeResponse);
                return errorCode = "Charge Tag is either not valid or not registered or not authorized to access this Charge Point";


            }
            catch (Exception exp)
            {
                Logger.Error(exp, "Authorize => Exception: {Message}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus.Id, null,msgIn.Action, $"'{idTag}'=>{authorizeResponse.IdTagInfo?.Status}", errorCode);
            return errorCode;
        }
        
    }
}
