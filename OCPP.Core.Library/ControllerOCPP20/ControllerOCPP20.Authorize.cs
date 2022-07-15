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
        public string? HandleAuthorize(OcppMessage msgIn, OcppMessage msgOut)
        {
            string? errorCode = null;
            AuthorizeResponse authorizeResponse = new AuthorizeResponse();

            string? idTag = null;
            try
            {
                Logger.Verbose("Processing authorize request...");
                AuthorizeRequest authorizeRequest = JsonConvert.DeserializeObject<AuthorizeRequest>(msgIn.JsonPayload);
                Logger.Verbose("Authorize => Message deserialized");
                idTag = ControllerBase.CleanChargeTagId(authorizeRequest.IdToken?.IdToken, Logger);

                authorizeResponse.CustomData = new CustomDataType();
                authorizeResponse.CustomData.VendorId = Library.ControllerOcpp20.VendorId;

                authorizeResponse.IdTokenInfo = new IdTokenInfoType();
                authorizeResponse.IdTokenInfo.CustomData = new CustomDataType();
                authorizeResponse.IdTokenInfo.CustomData.VendorId = Library.ControllerOcpp20.VendorId;
                authorizeResponse.IdTokenInfo.GroupIdToken = new IdTokenType();
                authorizeResponse.IdTokenInfo.GroupIdToken.CustomData = new CustomDataType();
                authorizeResponse.IdTokenInfo.GroupIdToken.CustomData.VendorId = Library.ControllerOcpp20.VendorId;
                authorizeResponse.IdTokenInfo.GroupIdToken.IdToken = string.Empty;

                try
                {
                    using (var dbContext = DbContext)
                    {
                        ChargeTag ct = dbContext.Find<ChargeTag>(idTag);
                        if (ct != null)
                        {
                            if (!string.IsNullOrEmpty(ct.ParentTagId.ToString()))
                            {
                                authorizeResponse.IdTokenInfo.GroupIdToken.IdToken = ct.ParentTagId.ToString();
                            }

                            if (Enum.TryParse(ct.TagStatus, out ChargeTagStatus status) && status.Equals(ChargeTagStatus.Blocked))
                            {
                                authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Blocked;
                            }
                            else if (ct.ExpiryDate.HasValue && ct.ExpiryDate.Value < DateTime.Now)
                            {
                                authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Expired;
                            }
                            else
                            {
                                authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Accepted;
                            }
                        }
                        else
                        {
                            authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
                        }

                        Logger.Information("Authorize => Status: {0}", authorizeResponse.IdTokenInfo.Status);
                    }
                }
                catch (Exception exp)
                {
                    Logger.Error(exp, "Authorize => Exception reading charge tag ({0}): {1}", idTag, exp.Message);
                    authorizeResponse.IdTokenInfo.Status = AuthorizationStatusEnumType.Invalid;
                }

                msgOut.JsonPayload = JsonConvert.SerializeObject(authorizeResponse);
                Logger.Verbose("Authorize => Response serialized");
            }
            catch (Exception exp)
            {
                Logger.Error(exp, "Authorize => Exception: {0}", exp.Message);
                errorCode = ErrorCodes.FormationViolation;
            }

            WriteMessageLog(ChargePointStatus.Id, null, msgIn.Action, $"'{idTag}'=>{authorizeResponse.IdTokenInfo?.Status}", errorCode);
            return errorCode;
        }
    }
}
