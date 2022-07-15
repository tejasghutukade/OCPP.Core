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
using OCPP.Core.Library.Messages_OCPP16;
using OCPP.Core.Library.Messages_OCPP16.OICP;

namespace OCPP.Core.Library
{
    public partial class ControllerOcpp16
    {
        private Task<string?> HandleAuthorize(OcppMessage msgIn, OcppMessage msgOut)
        {
            string? errorCode = null;
            var authorizeResponse = new AuthorizeResponse
            {
                IdTagInfo =
                {
                    Status = IdTagInfoStatus.Invalid
                }
            };
            string? idTag = null;
            try
            {
                Logger.Verbose("Processing authorize request...");
                if (msgIn.JsonPayload != null)
                {
                    var authorizeRequest = JsonConvert.DeserializeObject<AuthorizeRequest>(msgIn.JsonPayload);
                    Logger.Verbose("Authorize => Message deserialized");
                    idTag = ControllerBase.CleanChargeTagId(authorizeRequest?.IdTag, Logger);
                }


                var ct = ChargeTag.IsValid(DbContext,idTag);

                if (ct.GetChargeTagStatus() != ChargeTagStatus.Invalid)
                {
                    var cpTagAccess = CpTagAccess.IsValid(DbContext, ct.Id, ChargePointStatus.Id);
                    if (cpTagAccess.GetChargeTagStatus() != ChargeTagStatus.Invalid)
                    {
                        authorizeResponse.IdTagInfo.ParentIdTag = ct.ParentTagId.ToString()?? string.Empty;
                        authorizeResponse.IdTagInfo.ExpiryDate = cpTagAccess.Expiry?? DateTimeOffset.UtcNow.AddMinutes(5);   // default: 5 minutes
                        authorizeResponse.IdTagInfo.Status = Enum.TryParse<IdTagInfoStatus>(cpTagAccess.GetChargeTagStatus().ToString(), out var chargeTagStatus) ? chargeTagStatus : IdTagInfoStatus.Invalid;
                        msgOut.JsonPayload = JsonConvert.SerializeObject(authorizeResponse);
                    }
                }


                authorizeResponse.IdTagInfo.ParentIdTag = string.Empty;
                authorizeResponse.IdTagInfo.ExpiryDate = DateTimeOffset.UtcNow.AddMinutes(-5);   // default: 5 minutes
                authorizeResponse.IdTagInfo.Status = IdTagInfoStatus.Invalid;
                msgOut.JsonPayload = JsonConvert.SerializeObject(authorizeResponse);
                return Task.FromResult(errorCode = "Charge Tag is either not valid or not registered or not authorized to access this Charge Point")!;


            }
            catch (Exception exp)
            {
                Logger.Error(exp, "Authorize => Exception: {Message}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus.Id, null,msgIn.Action, $"'{idTag}'=>{authorizeResponse.IdTagInfo?.Status}", errorCode);
            return Task.FromResult(errorCode)!;
        }
        
    }
}
