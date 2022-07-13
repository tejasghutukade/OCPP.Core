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
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCPP.Core.Database;
using OCPP.Core.Management.Models;

namespace OCPP.Core.Management.Controllers
{
    public partial class HomeController
    {
        [Authorize]
        public IActionResult ChargeTag(string id, ChargeTagViewModel chargeTagViewModel)
        {
            try
            {
                if (!User.IsInRole(Constants.AdminRoleName))
                {
                    Logger.Warning("ChargeTag: Request by non-administrator: {Name}", User.Identity?.Name);
                    TempData["ErrMsgKey"] = "AccessDenied";
                    return RedirectToAction("Error", new { Id = "" });
                }

                ViewBag.DatePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                ViewBag.Language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                chargeTagViewModel.CurrentTagId = id;

                using var dbContext = _dbContext;
                Logger.Verbose("ChargeTag: Loading charge tags...");
                var dbChargeTags = dbContext.ChargeTags.ToList();
                Logger.Information("ChargeTag: Found {Count} charge tags", dbChargeTags.Count);

                ChargeTag? currentChargeTag = null;
                if (!string.IsNullOrEmpty(id))
                {
                    foreach (ChargeTag tag in dbChargeTags)
                    {
                        if (tag.TagId.Equals(id, StringComparison.InvariantCultureIgnoreCase))
                        {
                            currentChargeTag = tag;
                            Logger.Verbose("ChargeTag: Current charge tag: {TagId} / {TagName}", tag.TagId, tag.TagName);
                            break;
                        }
                    }
                }

                if (Request.Method == "POST")
                {
                    string? errorMsg = null;

                    if (id == "@")
                    {
                        Logger.Verbose("ChargeTag: Creating new charge tag...");

                        // Create new tag
                        if (string.IsNullOrWhiteSpace(chargeTagViewModel.TagId))
                        {
                            errorMsg = _localizer["ChargeTagIdRequired"].Value;
                            Logger.Information("ChargeTag: New => no charge tag ID entered");
                        }

                        if (string.IsNullOrEmpty(errorMsg))
                        {
                            // check if duplicate
                            if (dbChargeTags.Any(tag => tag.TagId.Equals(chargeTagViewModel.TagId, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                errorMsg = _localizer["ChargeTagIdExists"].Value;
                                Logger.Information("ChargeTag: New => charge tag ID already exists: {TagId}", chargeTagViewModel.TagId);
                            }
                        }

                        if (string.IsNullOrEmpty(errorMsg))
                        {
                            // Save tag in DB
                            var newTag = new ChargeTag
                            {
                                TagId = chargeTagViewModel.TagId,
                                TagName = chargeTagViewModel.TagName,
                                //newTag.ParentTagId = chargeTagViewModel.ParentTagId;
                                ExpiryDate = chargeTagViewModel.ExpiryDate,
                                TagStatus = chargeTagViewModel.Blocked?nameof(ChargeTagStatus.Blocked):nameof(ChargeTagStatus.Accepted)
                            };
                            dbContext.ChargeTags.Add(newTag);
                            dbContext.SaveChanges();
                            Logger.Information("ChargeTag: New => charge tag saved: {TagId} / {TagName}", chargeTagViewModel.TagId, chargeTagViewModel.TagName);
                        }
                        else
                        {
                            ViewBag.ErrorMsg = errorMsg;
                            return View("ChargeTagDetail", chargeTagViewModel);
                        }
                    }
                    else if (currentChargeTag?.TagId == id)
                    {
                        // Save existing tag
                        currentChargeTag.TagName = chargeTagViewModel.TagName;
                        //currentChargeTag.ParentTagId = chargeTagViewModel.ParentTagId;
                        currentChargeTag.ExpiryDate = chargeTagViewModel.ExpiryDate;
                        currentChargeTag.TagStatus = chargeTagViewModel.Blocked?nameof(ChargeTagStatus.Blocked):nameof(ChargeTagStatus.Accepted);
                        dbContext.SaveChanges();
                        Logger.Information("ChargeTag: Edit => charge tag saved: {TagId} / {TagName}", chargeTagViewModel.TagId, chargeTagViewModel.TagName);
                    }

                    return RedirectToAction("ChargeTag", new { Id = "" });
                }
                else
                {
                    // List all charge tags
                    chargeTagViewModel = new ChargeTagViewModel
                    {
                        ChargeTags = dbChargeTags,
                        CurrentTagId = id
                    };

                    if (currentChargeTag != null)
                    {
                        chargeTagViewModel.TagId = currentChargeTag.TagId;
                        chargeTagViewModel.TagName = currentChargeTag.TagName;
                        // chargeTagViewModel.ParentTagId = currentChargeTag.ParentTagId;
                        chargeTagViewModel.ExpiryDate = currentChargeTag.ExpiryDate;
                        chargeTagViewModel.Blocked = (currentChargeTag.TagStatus != nameof(ChargeTagStatus.Blocked));
                    }

                    var viewName = (!string.IsNullOrEmpty(chargeTagViewModel.TagId) || id=="@") ? "ChargeTagDetail" : "ChargeTagList";
                    
                    return View(viewName, chargeTagViewModel);
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp, "ChargeTag: Error loading charge tags from database");
                TempData["ErrMessage"] = exp.Message;
                return RedirectToAction("Error", new { Id = "" });
            }
        }
    }
}
