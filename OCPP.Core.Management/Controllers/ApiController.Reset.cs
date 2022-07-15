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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;
using OCPP.Core.Database;
using Serilog;

namespace OCPP.Core.Management.Controllers
{
    public partial class ApiController
    {
        private readonly IStringLocalizer<ApiController> _localizer;
        private readonly OcppCoreContext _dbContext;
        public ApiController(
            UserManager userManager,
            IStringLocalizer<ApiController> localizer,
            ILogger loggerFactory,
            IConfiguration config,OcppCoreContext dbContext) : base(userManager, loggerFactory, config)
        {
            _localizer = localizer;
            Logger = loggerFactory;
            _dbContext = dbContext;
        }

        [Authorize]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Reset(string id)
        {
            if (!User.IsInRole(Constants.AdminRoleName))
            {
                Logger.Warning("Reset: Request by non-administrator: {UserIdentity}", User.Identity?.Name);
                return StatusCode((int)HttpStatusCode.Unauthorized);
            }

            int httpStatuscode = (int)HttpStatusCode.OK;
            string resultContent = string.Empty;

            Logger.Verbose("Reset: Request to restart chargepoint '{Id}'", id);
            if (!string.IsNullOrEmpty(id))
            {
                try
                {
                    using (var dbContext = _dbContext)
                    {
                        ChargePoint? chargePoint = dbContext.ChargePoints.Find(id);
                        if (chargePoint != null)
                        {
                            string serverApiUrl = Config.GetValue<string>("ServerApiUrl");
                            string apiKeyConfig = Config.GetValue<string>("ApiKey");
                            if (!string.IsNullOrEmpty(serverApiUrl))
                            {
                                try
                                {
                                    using (var httpClient = new HttpClient())
                                    {
                                        if (!serverApiUrl.EndsWith('/'))
                                        {
                                            serverApiUrl += "/";
                                        }
                                        
                                        Uri uri = new Uri(serverApiUrl);
                                        uri = new Uri(uri, $"Reset/{id}");
                                        httpClient.Timeout = new TimeSpan(0, 0, 4); // use short timeout

                                        // API-Key authentication?
                                        if (!string.IsNullOrWhiteSpace(apiKeyConfig))
                                        {
                                            httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKeyConfig);
                                        }
                                        else
                                        {
                                            Logger.Warning("Reset: No API-Key configured!");
                                        }

                                        HttpResponseMessage response = await httpClient.GetAsync(uri);
                                        if (response.StatusCode == HttpStatusCode.OK)
                                        {
                                            string jsonResult = await response.Content.ReadAsStringAsync();
                                            if (!string.IsNullOrEmpty(jsonResult))
                                            {
                                                try
                                                {
                                                    dynamic? jsonObject = JsonConvert.DeserializeObject(jsonResult);
                                                    Logger.Information("Reset: Result of API request is '{JsonResult}'", jsonResult);
                                                    string status = jsonObject?.status ?? string.Empty;
                                                    switch (status)
                                                    {
                                                        case "Accepted":
                                                            resultContent = _localizer["ResetAccepted"];
                                                            break;
                                                        case "Rejected":
                                                            resultContent = _localizer["ResetRejected"];
                                                            break;
                                                        case "Scheduled":
                                                            resultContent = _localizer["ResetScheduled"];
                                                            break;
                                                        default:
                                                            resultContent = string.Format(_localizer["ResetUnknownStatus"], status);
                                                            break;
                                                    }
                                                }
                                                catch (Exception exp)
                                                {
                                                    Logger.Error(exp, "Reset: Error in JSON result => {ErrorMessage}", exp.Message);
                                                    httpStatuscode = (int)HttpStatusCode.OK;
                                                    resultContent = _localizer["ResetError"];
                                                }
                                            }
                                            else
                                            {
                                                Logger.Error("Reset: Result of API request is empty");
                                                httpStatuscode = (int)HttpStatusCode.OK;
                                                resultContent = _localizer["ResetError"];
                                            }
                                        }
                                        else if (response.StatusCode == HttpStatusCode.NotFound)
                                        {
                                            // Chargepoint offline
                                            httpStatuscode = (int)HttpStatusCode.OK;
                                            resultContent = _localizer["ResetOffline"];
                                        }
                                        else
                                        {
                                            Logger.Error("Reset: Result of API  request => httpStatus={StatusCode}", response.StatusCode);
                                            httpStatuscode = (int)HttpStatusCode.OK;
                                            resultContent = _localizer["ResetError"];
                                        }
                                    }
                                }
                                catch (Exception exp)
                                {
                                    Logger.Error(exp, "Reset: Error in API request => {ErrorMessage}", exp.Message);
                                    httpStatuscode = (int)HttpStatusCode.OK;
                                    resultContent = _localizer["ResetError"];
                                }
                            }
                        }
                        else
                        {
                            Logger.Warning("Reset: Error loading charge point '{Id}' from database", id);
                            httpStatuscode = (int)HttpStatusCode.OK;
                            resultContent = _localizer["UnknownChargepoint"];
                        }
                    }
                }
                catch (Exception exp)
                {
                    Logger.Error(exp, "Reset: Error loading charge point from database");
                    httpStatuscode = (int)HttpStatusCode.OK;
                    resultContent = _localizer["ResetError"];
                }
            }

            return StatusCode(httpStatuscode, resultContent);
        }
    }
}
