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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Serilog;
using OCPP.Core.Database;
using OCPP.Core.Management.Models;

namespace OCPP.Core.Management.Controllers
{
    public partial class HomeController : BaseController
    {
        [Authorize]
        public IActionResult Connector(string id, string connectorId, ConnectorStatusViewModel csvm)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName))
                {
                    Logger.Warning("Connector: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                ViewBag.DatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                ViewBag.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                using (var dbContext = _dbContext)
                {
                    Logger.Verbose("Connector: Loading connectors...");
                    List<ConnectorStatus> dbConnectorStatuses = dbContext.ConnectorStatuses.ToList<ConnectorStatus>();
                    Logger.Information("Connector: Found {0} connectors", dbConnectorStatuses.Count);

                    ConnectorStatus currentConnectorStatus = null;
                    if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(connectorId))
                    {
                        foreach (ConnectorStatus cs in dbConnectorStatuses)
                        {
                            if (cs.ChargePointId.ToString().Equals(id, StringComparison.InvariantCultureIgnoreCase) &&
                                cs.ConnectorId.ToString().Equals(connectorId, StringComparison.InvariantCultureIgnoreCase))
                            {
                                currentConnectorStatus = cs;
                                Logger.Verbose("Connector: Current connector: {0} / {1}", cs.ChargePointId, cs.ConnectorId);
                                break;
                            }
                        }
                    }

                    if (Request.Method == "POST")
                    {
                        if (currentConnectorStatus.ChargePointId.ToString() == id)
                        {
                            // Save connector
                            currentConnectorStatus.ConnectorName = csvm.ConnectorName;
                            dbContext.SaveChanges();
                            Logger.Information("Connector: Edit => Connector saved: {0} / {1} => '{2}'", csvm.ChargePointId, csvm.ConnectorId, csvm.ConnectorName);
                        }

                        return RedirectToAction("Connector", new { Id = "" });
                    }
                    else
                    {
                        // List all charge tags
                        csvm = new ConnectorStatusViewModel();
                        csvm.ConnectorStatuses = dbConnectorStatuses;

                        if (currentConnectorStatus != null)
                        {
                            csvm.ChargePointId = currentConnectorStatus.ChargePointId.ToString();
                            csvm.ConnectorId = currentConnectorStatus.ConnectorId;
                            csvm.ConnectorName = currentConnectorStatus.ConnectorName;
                            csvm.LastStatus = currentConnectorStatus.LastStatus;
                            csvm.LastStatusTime = currentConnectorStatus.LastStatusTime;
                            csvm.LastMeter = currentConnectorStatus.LastMeter;
                            csvm.LastMeterTime = currentConnectorStatus.LastMeterTime;
                        }

                        string viewName = (currentConnectorStatus != null) ? "ConnectorDetail" : "ConnectorList";
                        return View(viewName, csvm);
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp, "Connector: Error loading connectors from database");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }
    }
}
