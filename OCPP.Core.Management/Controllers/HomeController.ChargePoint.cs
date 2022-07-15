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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCPP.Core.Database;
using OCPP.Core.Management.Models;

namespace OCPP.Core.Management.Controllers
{
    public partial class HomeController : BaseController
    {
        [Authorize]
        public IActionResult ChargePoint(string id, ChargePointViewModel cpvm)
        {
            try
            {
                if (User != null && !User.IsInRole(Constants.AdminRoleName))
                {
                    Logger.Warning("ChargePoint: Request by non-administrator: {0}", User?.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                cpvm.CurrentId = id;

                using (var dbContext = _dbContext)
                {
                    Logger.Verbose("ChargePoint: Loading charge points...");
                    List<ChargePoint> dbChargePoints = dbContext.ChargePoints.ToList<ChargePoint>();
                    Logger.Information("ChargePoint: Found {0} charge points", dbChargePoints.Count);

                    ChargePoint currentChargePoint = null;
                    if (!string.IsNullOrEmpty(id))
                    {
                        foreach (ChargePoint cp in dbChargePoints)
                        {
                            if (cp.ChargePointId.ToString().Equals(id, StringComparison.InvariantCultureIgnoreCase))
                            {
                                currentChargePoint = cp;
                                Logger.Verbose("ChargePoint: Current charge point: {0} / {1}", cp.ChargePointId, cp.Name);
                                break;
                            }
                        }
                    }

                    if (Request.Method == "POST")
                    {
                        string errorMsg = null;

                        if (id == "@")
                        {
                            Logger.Verbose("ChargePoint: Creating new charge point...");

                            // Create new tag
                            if (string.IsNullOrWhiteSpace(cpvm.ChargePointId))
                            {
                                errorMsg = _localizer["ChargePointIdRequired"].Value;
                                Logger.Information("ChargePoint: New => no charge point ID entered");
                            }

                            if (string.IsNullOrEmpty(errorMsg))
                            {
                                // check if duplicate
                                foreach (ChargePoint cp in dbChargePoints)
                                {
                                    if (cp.ChargePointId.ToString().Equals(cpvm.ChargePointId, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        // id already exists
                                        errorMsg = _localizer["ChargePointIdExists"].Value;
                                        Logger.Information("ChargePoint: New => charge point ID already exists: {0}", cpvm.ChargePointId);
                                        break;
                                    }
                                }
                            }

                            if (string.IsNullOrEmpty(errorMsg))
                            {
                                // Save tag in OCPP.Core.DB
                                ChargePoint newChargePoint = new ChargePoint();
                                newChargePoint.ChargePointId = cpvm.ChargePointId;
                                newChargePoint.Name = cpvm.Name;
                                newChargePoint.Comment = cpvm.Comment;
                                newChargePoint.Username = cpvm.Username;
                                newChargePoint.Password = cpvm.Password;
                                newChargePoint.ClientCertThumb = cpvm.ClientCertThumb;
                                dbContext.ChargePoints.Add(newChargePoint);
                                dbContext.SaveChanges();
                                Logger.Information("ChargePoint: New => charge point saved: {0} / {1}", cpvm.ChargePointId, cpvm.Name);
                            }
                            else
                            {
                                ViewBag.ErrorMsg = errorMsg;
                                return View("ChargePointDetail", cpvm);
                            }
                        }
                        else if (currentChargePoint.ChargePointId.ToString() == id)
                        {
                            // Save existing charge point
                            Logger.Verbose("ChargePoint: Saving charge point '{0}'", id);
                            currentChargePoint.Name = cpvm.Name;
                            currentChargePoint.Comment = cpvm.Comment;
                            currentChargePoint.Username = cpvm.Username;
                            currentChargePoint.Password = cpvm.Password;
                            currentChargePoint.ClientCertThumb = cpvm.ClientCertThumb;

                            dbContext.SaveChanges();
                            Logger.Information("ChargePoint: Edit => charge point saved: {0} / {1}", cpvm.ChargePointId, cpvm.Name);
                        }

                        return RedirectToAction("ChargePoint", new { Id = "" });
                    }
                    else
                    {
                        // Display charge point
                        cpvm = new ChargePointViewModel();
                        cpvm.ChargePoints = dbChargePoints;
                        cpvm.CurrentId = id;

                        if (currentChargePoint!= null)
                        {
                            cpvm = new ChargePointViewModel();
                            cpvm.ChargePointId = currentChargePoint.ChargePointId.ToString();
                            cpvm.Name = currentChargePoint.Name;
                            cpvm.Comment = currentChargePoint.Comment;
                            cpvm.Username = currentChargePoint.Username;
                            cpvm.Password = currentChargePoint.Password;
                            cpvm.ClientCertThumb = currentChargePoint.ClientCertThumb;
                        }

                        string viewName = (!string.IsNullOrEmpty(cpvm.ChargePointId) || id == "@") ? "ChargePointDetail" : "ChargePointList";
                        return View(viewName, cpvm);
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp, "ChargePoint: Error loading charge points from database");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }
    }
}
