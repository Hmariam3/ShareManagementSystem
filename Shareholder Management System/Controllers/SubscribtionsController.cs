using OfficeOpenXml;
using OfficeOpenXml.Style;
using Shareholder_Management_System.Controllers;
using Shareholder_Management_System.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;


namespace Share.Controllers
{
    public class SubscribtionsController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        // GET: Subscribtions

        // GET: Subscribtions

        public ActionResult Filter()
        {

            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "FullNameEng");

            var subscribtions = db.Subscribtions
                .Include(s => s.Shareholder)
                .Include(s => s.Shareholder1)
                .Include(s => s.User)
                .Include(s => s.User1)
                .ToList();

            return View(subscribtions);
        }

        [HttpPost]
        public ActionResult FiltereData(int? ShareId)
        {
            var subscriptions = db.Subscribtions
                .Include(s => s.Shareholder)
                .Include(s => s.Shareholder1)
                .Include(s => s.User)
                .Include(s => s.User1)
                .AsQueryable(); // Use IQueryable for dynamic filtering

            if (ShareId.HasValue)
            {
                subscriptions = subscriptions.Where(s => s.ShID == ShareId && s.SubAuthorizationStatus == "Approved" && s.UnpaidSubscription > 0 && s.PaymentDueDate >= DateTime.Now);
            }
            else
            {
                subscriptions = subscriptions.Where(s => s.ShID == ShareId && s.SubAuthorizationStatus == "Approved" && s.UnpaidSubscription > 0);
            }

            return PartialView("_SubscriptionsList", subscriptions.ToList());
        }


        // For Filtering and Adding Subscriptions 


        public ActionResult Addonsubscriptions()
        {

            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "FullNameEng");

            var subscribtions = db.Subscribtions
                .Include(s => s.Shareholder)
                .Include(s => s.Shareholder1)
                .Include(s => s.User)
                .Include(s => s.User1)
                .ToList();

            return View(subscribtions);
        }

        //[HttpPost]
        //public ActionResult Addonsubscriptions(int? ShareId)
        //{
        //    var subscriptions = db.Subscribtions
        //        .Include(s => s.Shareholder)
        //        .Include(s => s.Shareholder1)
        //        .Include(s => s.User)
        //        .Include(s => s.User1)
        //        .AsQueryable(); // Use IQueryable for dynamic filtering

        //    if (ShareId.HasValue)
        //    {
        //        subscriptions = subscriptions.Where(s => s.ShID == ShareId && s.SubAuthorizationStatus == "Approved"/* && s.UnpaidSubscription > 0 && s.PaymentDueDate >= DateTime.Now*/);
        //    }
        //    else
        //    {
        //        subscriptions = subscriptions.Where(s => s.ShID == ShareId && s.SubAuthorizationStatus == "Approved" /*&& s.UnpaidSubscription > 0*/);
        //    }

        //    return PartialView("_AddonsubscriptionsList", subscriptions.ToList());
        //}

   [HttpPost]
public ActionResult Addonsubscriptions(int? ShareId, int? SubId)
{
    var subscriptions = db.Subscribtions
        .Include(s => s.Shareholder)
        .Include(s => s.Shareholder1)
        .Include(s => s.User)
        .Include(s => s.User1)
        .AsQueryable();

    if (ShareId.HasValue)
    {
        subscriptions = subscriptions.Where(s => s.ShID == ShareId);
    }

    if (SubId.HasValue)
    {
        subscriptions = subscriptions.Where(s => s.SubID == SubId);
    }

    subscriptions = subscriptions.Where(s => s.SubAuthorizationStatus == "Approved");

    return PartialView("_AddonsubscriptionsList", subscriptions.ToList());
}






        public ActionResult PendingSubscription()
        {
            var subscribtions = db.Subscribtions.Include(s => s.Shareholder).Include(s => s.Shareholder1).Include(s => s.User).Include(s => s.User1).Where(a => a.SubAuthorizationStatus != "Approved" && a.PaymentDueDate >= DateTime.Now);
            return View(subscribtions.ToList());
        }

        public ActionResult Index()
        {
            var subscribtions = db.Subscribtions.Include(s => s.Shareholder).Include(s => s.Shareholder1).Include(s => s.User).Include(s => s.User1);
            return View(subscribtions.ToList());
        }

        // GET: Subscribtions/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Subscribtion subscribtion = db.Subscribtions.Find(id);
            if (subscribtion == null)
            {
                return HttpNotFound();
            }
            return View(subscribtion);
        }

        // GET: Subscribtions/Create
        //public ActionResult Create()
        //{
        //    ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "FullNameEng");
        //    ViewBag.SubTransferFrom = new SelectList(db.Shareholders, "ShID", "FullNameEng");
        //    ViewBag.SubAuthorizer = new SelectList(db.Users, "UID", "FullName");
        //    ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");
        //    return View();
        //}

        public ActionResult Create()
        {

            //ViewBag.ShID = new SelectList(db.Shareholders.Where(s => s.AuthorizationStatus == "Approved"), "ShID", "FullNameEng");
            //ViewBag.ShID = new SelectList(db.Shareholders.Select(s => new { ShID = s.ShID, DisplayName = s.FullNameEng + " / " + s.ShareID }), "ShID", "DisplayName");

            ViewBag.ShID = new SelectList(db.Shareholders.Where(s => s.AuthorizationStatus == "Approved" && s.Status.Equals("Active")).Select(s => new { ShID = s.ShID, DisplayName = s.FullNameEng + " / " + s.ShareID }), "ShID", "DisplayName");
            ViewBag.SubTransferFrom = new SelectList(db.Shareholders.Where(s => s.AuthorizationStatus == "Approved"), "ShID", "FullNameEng");
            ViewBag.SubAuthorizer = new SelectList(db.Users, "UID", "FullName");
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "SubID,ShID,SubNumShares,Premium,SubAmount,PaidSubscription,UnpaidSubscription,SubTransferFrom,PaymentDueDate,SubStatus,CreatedBy,SubDate,SubAuthorizationStatus,SubAuthorizer,AuthorizedDate,Remark,Branch")] Subscribtion subscribtion)
        {
            int Createdby = Convert.ToInt32(Session["ID"]);
            int branchIDD = Convert.ToInt32(Session["Branch"]);

            if (subscribtion.PaymentDueDate < DateTime.Today)
            {
                ModelState.AddModelError("PaymentDueDate", "The Payment Due Date cannot be in the past.");
            }
            if (ModelState.IsValid)
            {
                db.Subscribtions.Add(subscribtion);

                subscribtion.SubDate = DateTime.Now;
                subscribtion.PaidSubscription = 0;
                subscribtion.SubStatus = "UnPaid";
                subscribtion.SubAuthorizationStatus = "Pending";
                subscribtion.CreatedBy = Createdby;
                subscribtion.Branch = branchIDD;
                subscribtion.SubAuthorizer = null;
                //subscribtion.CreatedBy = Session["Username"];

                subscribtion.SubAmount = subscribtion.SubNumShares * 100;

                subscribtion.UnpaidSubscription = subscribtion.SubAmount;

                //subscribtion.SubAuthorizer = Session["subusername"];

                db.SaveChanges();
                TempData["SuccessMessage"] = "Subscription created successfully!";

                AuditLogsController auditLogsController = new AuditLogsController();

                auditLogsController.RecordLog("Registration", subscribtion.SubID, "Subscribtion", subscribtion.CreatedBy ?? 0, Session["BranchName"].ToString());


                return RedirectToAction("Create");
            }

            ViewBag.ShID = new SelectList(db.Shareholders.Where(s => s.AuthorizationStatus == "Approved" && s.Status.Equals("Active")).Select(s => new { ShID = s.ShID, DisplayName = s.FullNameEng + " / " + s.ShareID }), "ShID", "DisplayName");
            ViewBag.SubTransferFrom = new SelectList(db.Shareholders, "ShID", "ShareID", subscribtion.SubTransferFrom);
            ViewBag.SubAuthorizer = new SelectList(db.Users, "UID", "FullName", subscribtion.SubAuthorizer);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", subscribtion.CreatedBy);
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchName", subscribtion.Branch);
            return View(subscribtion);
        }



        /// =======================================================================================================

        public ActionResult AddSubscriprion()
        {
            //ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "FullNameEng");
            // ViewBag.SubTransferFrom = new SelectList(db.Shareholders, "ShID", "FullNameEng");

            ViewBag.ShID = new SelectList(db.Shareholders.Where(s => s.AuthorizationStatus == "Approved" && s.Status.Equals("Active")).Select(s => new { ShID = s.ShID, DisplayName = s.FullNameEng + " / " + s.ShareID }), "ShID", "DisplayName");
            ViewBag.SubTransferFrom = new SelectList(db.Shareholders.Where(s => s.AuthorizationStatus == "Approved"), "ShID", "FullNameEng");
            ViewBag.SubAuthorizer = new SelectList(db.Users, "UID", "FullName");
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");

            ViewBag.SubID = new SelectList(Enumerable.Empty<SelectListItem>(), "SubID", "SubID");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddSubscriprion([Bind(Include = "SubID,ShID,SubNumShares,Premium,SubAmount,PaidSubscription,UnpaidSubscription,SubTransferFrom,PaymentDueDate,SubStatus,CreatedBy,SubDate,SubAuthorizationStatus,SubAuthorizer,AuthorizedDate,Remark")] Subscribtion subscription)
        {
            int userdata = Convert.ToInt32(Session["ID"]);

            if (ModelState.IsValid)
            {
                // Fetch the existing subscription for the given ShID and SubID
                var existingSubscription = db.Subscribtions
                    .FirstOrDefault(s => s.ShID == subscription.ShID && s.SubID == subscription.SubID);

                if (existingSubscription != null)
                {
                    // Update the existing subscription's values
                    existingSubscription.SubNumShares += subscription.SubNumShares; // Add new shares to the existing number of shares
                    existingSubscription.Premium = subscription.Premium;
                    existingSubscription.SubAmount = existingSubscription.SubNumShares * 100; // Recalculate the total amount

                    existingSubscription.UnpaidSubscription = existingSubscription.SubAmount - existingSubscription.PaidSubscription; // Recalculate unpaid subscription
                    existingSubscription.SubTransferFrom = subscription.SubTransferFrom; // Update the transfer from field
                    existingSubscription.PaymentDueDate = subscription.PaymentDueDate; // Update the payment due date
                    existingSubscription.Remark = subscription.Remark; // Update any remarks

                    // Set other fields
                    existingSubscription.SubDate = DateTime.Now; // Update the date
                    //existingSubscription.SubStatus = "UnPaid"; // Update the status (or retain the existing one if needed)
                    if (existingSubscription.PaidSubscription > 0)
                    {
                        existingSubscription.SubStatus = "Partial Paid"; // Update the status (or retain the existing one if needed)
                    }
                    existingSubscription.SubAuthorizationStatus = "Pending"; // Update the authorization status (or retain the existing one)
                    existingSubscription.AuthorizedDate = null;

                    existingSubscription.CreatedBy = userdata;
                    existingSubscription.SubAuthorizer = null;

                    // Mark the subscription as modified and save the changes
                    db.Entry(existingSubscription).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Subscription updated successfully! Total shares now: " + existingSubscription.SubNumShares;
                    AuditLogsController auditLogsController = new AuditLogsController();
                    auditLogsController.RecordLog("Addon", existingSubscription.SubID, "Subscribtion", existingSubscription.CreatedBy ?? 0, Session["BranchName"].ToString());
                    return RedirectToAction("AddSubscriprion");
                }
                else
                {
                    ModelState.AddModelError("", "No subscription found for the given Shareholder and Subscription ID.");
                }
            }

            // If the model state is not valid, reload the view with the previous data
            ViewBag.ShID = new SelectList(db.Shareholders.Where(s => s.AuthorizationStatus == "Approved" && s.Status.Equals("Active")).Select(s => new { ShID = s.ShID, DisplayName = s.FullNameEng + " / " + s.ShareID }), "ShID", "DisplayName");

            ViewBag.SubTransferFrom = new SelectList(db.Shareholders, "ShID", "FullNameEng", subscription.SubTransferFrom);
            ViewBag.SubAuthorizer = new SelectList(db.Users, "UID", "FullName", subscription.SubAuthorizer);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", subscription.CreatedBy);

            return View(subscription);
        }


        //public ActionResult GetSubIDs(int shareholderId)
        //{
        //    var subIDs = db.Subscribtions
        //                   .Where(s => s.ShID == shareholderId
        //                                && s.SubAuthorizationStatus == "Approved"
        //                                && s.UnpaidSubscription >= 0
        //                                //&& s.PaymentDueDate >= DateTime.Now
        //                                && s.PaidSubscription.Value >= 0.25m * s.SubAmount.Value) // Ensure PaidSubscription is at least 25% of SubAmount
        //                   .Select(s => new { s.SubID, s.SubNumShares }) // Customize the fields as necessary
        //                   .ToList();

        //    return Json(subIDs, JsonRequestBehavior.AllowGet);
        //}

        public ActionResult GetSubIDs(int shareholderId)
        {
            var subIDs = db.Subscribtions
                           .Where(s => s.ShID == shareholderId
                                        && s.SubAuthorizationStatus == "Approved"
                                        && s.UnpaidSubscription >= 0
                                        && s.SubNumShares !=0
                                        && s.PaidSubscription.Value >= 0.25m * s.SubAmount.Value) // Ensure PaidSubscription is at least 25% of SubAmount
                                       
                           .Select(s => new
                           {
                               s.SubID,
                               s.SubNumShares
                           }) // Customize the fields as necessary
                           .ToList();

            return Json(subIDs, JsonRequestBehavior.AllowGet);
        }




        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Subscribtion subscribtion = db.Subscribtions.Find(id);
            if (subscribtion == null)
            {
                return HttpNotFound();
            }
            //ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "FullNameEng", subscribtion.ShID);
            ViewBag.ShID = new SelectList(db.Shareholders.Where(s => s.AuthorizationStatus == "Approved").Select(s => new { ShID = s.ShID, DisplayName = s.FullNameEng + " / " + s.ShareID }), "ShID", "DisplayName");
            ViewBag.SubTransferFrom = new SelectList(db.Shareholders, "ShID", "FullNameEng", subscribtion.SubTransferFrom);
            ViewBag.SubAuthorizer = new SelectList(db.Users, "UID", "FullName", subscribtion.SubAuthorizer);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", subscribtion.CreatedBy);
            return View(subscribtion);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "SubID,ShID,SubNumShares,Premium,SubAmount,PaidSubscription,UnpaidSubscription,SubTransferFrom,PaymentDueDate,SubStatus,CreatedBy,SubDate,SubAuthorizationStatus,SubAuthorizer,AuthorizedDate,Remark")] Subscribtion subscribtion)
        {
            // Get the original subscription from the database
            var originalSubscription = db.Subscribtions.AsNoTracking().FirstOrDefault(s => s.SubID == subscribtion.SubID);

            int userdata = Convert.ToInt32(Session["ID"]);
            int branchIDD = Convert.ToInt32(Session["Branch"]);

            if (originalSubscription == null)
            {
                return HttpNotFound();
            }

            // Check if the new PaidSubscription is less than the original one
            if (subscribtion.PaidSubscription < originalSubscription.PaidSubscription)
            {
                ModelState.AddModelError("PaidSubscription", "The paid amount cannot be less than the original paid amount.");
            }

            if (ModelState.IsValid)
            {
                // Modify the subscription
                db.Entry(subscribtion).State = EntityState.Modified;

                //subscribtion.SubDate = DateTime.Now;
                //subscribtion.SubStatus = "UnPaid";

                subscribtion.SubAuthorizationStatus = "Pending";
                subscribtion.SubAmount = subscribtion.SubNumShares * 100;

                if (subscribtion.PaidSubscription == null)
                {
                    subscribtion.UnpaidSubscription = subscribtion.SubAmount;
                }
                else
                {
                    subscribtion.UnpaidSubscription = subscribtion.SubAmount - subscribtion.PaidSubscription;
                }

                subscribtion.CreatedBy = userdata;
                subscribtion.Branch = branchIDD;
                subscribtion.SubAuthorizer = null;
                db.SaveChanges();
                TempData["SuccessMessage"] = "Subscription Updated successfully.";
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("Update", subscribtion.SubID, "Subscribtion", subscribtion.CreatedBy ?? 0, Session["BranchName"].ToString());

                return RedirectToAction("Index");
            }

            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "FullNameEng", subscribtion.ShID);
            ViewBag.SubTransferFrom = new SelectList(db.Shareholders, "ShID", "FullNameEng", subscribtion.SubTransferFrom);
            ViewBag.SubAuthorizer = new SelectList(db.Users, "UID", "FullName", subscribtion.SubAuthorizer);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", subscribtion.CreatedBy);
            return View(subscribtion);
        }

        // GET: Subscribtions/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Subscribtion subscribtion = db.Subscribtions.Find(id);
            if (subscribtion == null)
            {
                return HttpNotFound();
            }
            return View(subscribtion);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Subscribtion subscribtion = db.Subscribtions.Find(id);

            if (subscribtion != null)
            {
                // Check if the subscription has been used in any payment
                bool hasPayments = db.Payments.Any(p => p.SubID == subscribtion.SubID);

                if (hasPayments)
                {
                    // Error message if the subscription is used in a payment
                    TempData["ErrorMessage"] = "Cannot Delete Subscriptions The Payment is Already Started.";
                }
                else if (subscribtion.SubAuthorizationStatus != "Approved" && subscribtion.PaidSubscription == 0)
                {
                    // Delete subscription if it is not approved and not used in payments
                    db.Subscribtions.Remove(subscribtion);
                    db.SaveChanges();

                    // Success message
                    TempData["SuccessMessage"] = "Subscription deleted successfully.";

                }
                else
                {
                    // Error message for approved subscriptions
                    TempData["ErrorMessage"] = "Cannot delete approved subscriptions.";
                }
            }
            else
            {
                // Error message if subscription is not found
                TempData["ErrorMessage"] = "Subscription not found.";
            }

            return RedirectToAction("Index");
        }





        //To Authorize the Subscription
        public ActionResult Authorize(int? id, string action)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Subscribtion subscribtion = db.Subscribtions.Find(id);
            if (subscribtion == null)
            {
                return HttpNotFound();
            }
            return View(subscribtion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public ActionResult Authorize(int id, string action, string remark)
        {
            Subscribtion subscribtion = db.Subscribtions.Find(id);
            int userdata = Convert.ToInt32(Session["ID"]);
            if (subscribtion == null)
            {
                return HttpNotFound();
            }

            //subscribtion.SubAuthorizationStatus = "Approved";
            //subscribtion.AuthorizedDate = DateTime.Now;

            if (action == "approve")
            {
                subscribtion.SubAuthorizationStatus = "Approved";
                TempData["SuccessMessage"] = "Subscription Approved successfully.";
                subscribtion.Remark = "";
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("Approved", subscribtion.SubID, "Subscribtion", subscribtion.CreatedBy ?? 0, Session["BranchName"].ToString());
            }
            else if (action == "reject")
            {
                subscribtion.SubAuthorizationStatus = "Rejected";
                subscribtion.Remark = remark;
                TempData["SuccessMessage"] = "Subscription Rejected successfully.";
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("Rejected", subscribtion.SubID, "Subscribtion", subscribtion.CreatedBy ?? 0, Session["BranchName"].ToString());
            }

            subscribtion.SubAuthorizer = userdata;
            subscribtion.AuthorizedDate = DateTime.Now;

            db.Entry(subscribtion).Property(u => u.SubStatus).IsModified = true;
            db.SaveChanges();

            return RedirectToAction("PendingSubscription");
        }


        //to sumup the data===============================================================================================================================

        public ActionResult Compiledsubs()
        {
            // Fetch the subscriptions, including the necessary related entities (like Shareholder)
            var subscriptions = db.Subscribtions
                .Include(s => s.Shareholder)
                .Where(s => s.SubAuthorizationStatus == "Approved") // Filter only approved subscriptions
                .ToList();

            // Group by ShareholderId and calculate the sums
            var shareholderSummaries = subscriptions
                .GroupBy(s => s.ShID)
                .Select(group => new ShareholderSummaryViewModel
                {
                    ShareholderId = group.Key.ToString(),
                    ShareholderIdd = group.First().Shareholder.ShareID,// Convert ShareholderId to string
                    ShareholderName = group.First().Shareholder.FullNameEng, // Assuming 'FullNameEng' is the correct property for shareholder name
                    TotalNumberOfShares = group.Sum(s => s.SubNumShares ?? 0), // Sum of shares for each shareholder
                    TotalPaidAmount = group.Sum(s => s.PaidSubscription ?? 0), // Sum of paid amounts
                    TotalUnpaidSubscription = group.Sum(s => s.UnpaidSubscription ?? 0) // Sum of unpaid subscriptions
                })
                .ToList();

            // Pass the summaries to the view
            return View(shareholderSummaries);
        }


        //TO EXPORT THE COMPILED SUBSCRIPTIONS 

        public ActionResult ExportCompiledsubsToExcel()
        {
            // Set the license context for EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Fetch the subscriptions, including the necessary related entities (like Shareholder)
            var subscriptions = db.Subscribtions
                .Include(s => s.Shareholder)
                .Where(s => s.SubAuthorizationStatus == "Approved") // Filter only approved subscriptions
                .ToList();

            // Group by ShareholderId and calculate the sums
            var shareholderSummaries = subscriptions
                .GroupBy(s => s.ShID)
                .Select(group => new ShareholderSummaryViewModel
                {
                    ShareholderId = group.Key.ToString(),
                    ShareholderIdd = group.First().Shareholder.ShareID,
                    ShareholderName = group.First().Shareholder.FullNameEng,
                    TotalNumberOfShares = group.Sum(s => s.SubNumShares ?? 0),
                    TotalPaidAmount = group.Sum(s => s.PaidSubscription ?? 0),
                    TotalUnpaidSubscription = group.Sum(s => s.UnpaidSubscription ?? 0)
                })
                .ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Shareholder Summary");

                // Add the company logo
                string imagePath = Server.MapPath("~/assets/images/images.png");
                var picture = worksheet.Drawings.AddPicture("Logo", new FileInfo(imagePath));
                picture.SetPosition(0, 2, 0, 0);
                picture.SetSize(130, 80);

                // Merge and format cells for headers
                ExcelRange headerCell1 = worksheet.Cells["A2:B2"];
                headerCell1.Merge = true;
                headerCell1.Style.Font.Bold = true;
                headerCell1.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell1.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell1.Style.Font.Size = 16;
                headerCell1.Style.Font.Name = "Calibri";
                headerCell1.Style.Font.Color.SetColor(Color.White);
                headerCell1.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell1.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                ExcelRange headerCell5 = worksheet.Cells["A1:B1"];
                headerCell5.Merge = true;
                headerCell5.Style.Font.Bold = true;
                headerCell5.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell5.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell5.Style.Font.Size = 16;
                headerCell5.Style.Font.Name = "Calibri";
                headerCell5.Style.Font.Color.SetColor(Color.White);
                headerCell5.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell5.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                ExcelRange headerCell6 = worksheet.Cells["A3:B3"];
                headerCell6.Merge = true;
                headerCell6.Style.Font.Bold = true;
                headerCell6.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell6.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell6.Style.Font.Size = 16;
                headerCell6.Style.Font.Name = "Calibri";
                headerCell6.Style.Font.Color.SetColor(Color.White);
                headerCell6.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell6.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                ExcelRange headerCell = worksheet.Cells["C1:W1"];
                headerCell.Merge = true;
                headerCell.Value = "Baankii Hojii Gamtaa Oromiyaa";
                headerCell.Style.Font.Bold = true;
                headerCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell.Style.Font.Size = 16;
                headerCell.Style.Font.Name = "Calibri";
                headerCell.Style.Font.Color.SetColor(Color.White);
                headerCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                ExcelRange headerCell3 = worksheet.Cells["C2:W2"];
                headerCell3.Merge = true;
                headerCell3.Value = "Cooperative Bank of Oromia";
                headerCell3.Style.Font.Bold = true;
                headerCell3.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell3.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell3.Style.Font.Size = 16;
                headerCell3.Style.Font.Name = "Calibri";
                headerCell3.Style.Font.Color.SetColor(Color.White);
                headerCell3.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell3.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                ExcelRange headerCell4 = worksheet.Cells["C3:W3"];
                headerCell4.Merge = true;
                headerCell4.Value = "ኦሮሚያ ኅብራት ሥራ ባንክ";
                headerCell4.Style.Font.Bold = true;
                headerCell4.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell4.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell4.Style.Font.Size = 16;
                headerCell4.Style.Font.Name = "Calibri";
                headerCell4.Style.Font.Color.SetColor(Color.White);
                headerCell4.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell4.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                // Add headers for data table

                worksheet.Cells[5, 1].Value = "Shareholder ID";
                worksheet.Cells[5, 2].Value = "Shareholder Name";
                worksheet.Cells[5, 3].Value = "Total Number of Shares";
                worksheet.Cells[5, 4].Value = "Total Paid Amount";
                worksheet.Cells[5, 5].Value = "Total Unpaid Subscription";

                // Apply bold style to header row
                using (var headerCells = worksheet.Cells[5, 1, 5, 5]) // Range A5:E5
                {
                    headerCells.Style.Font.Bold = true;
                    headerCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    headerCells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    headerCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerCells.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(169, 208, 142)); // Light green background
                }

                // Add data
                for (int i = 0; i < shareholderSummaries.Count; i++)
                {
                    var summary = shareholderSummaries[i];
                    worksheet.Cells[i + 6, 1].Value = summary.ShareholderIdd;
                    worksheet.Cells[i + 6, 2].Value = summary.ShareholderName;
                    worksheet.Cells[i + 6, 3].Value = summary.TotalNumberOfShares;
                    worksheet.Cells[i + 6, 4].Value = summary.TotalPaidAmount;
                    worksheet.Cells[i + 6, 4].Style.Numberformat.Format = "#,##0.00";
                    worksheet.Cells[i + 6, 5].Value = summary.TotalUnpaidSubscription;
                    worksheet.Cells[i + 6, 5].Style.Numberformat.Format = "#,##0.00";
                }

                // Adjust column widths
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Generate the Excel file
                var excelData = package.GetAsByteArray();

                // Return the file as a download
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ShareholderSummary.xlsx");
            }
        }

        // Export THe VIEW =================================================================================


        public ActionResult ExportSubscriptionsToExcel()
        {
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            // Fetch subscriptions data
            var subscriptions = db.Subscribtions
                .Include(s => s.Shareholder)
                .Include(s => s.Shareholder1)
                .Include(s => s.User)
                .Include(s => s.User1)
                .ToList();

            using (var package = new ExcelPackage())
            {
                // Create a worksheet
                var worksheet = package.Workbook.Worksheets.Add("Subscriptions");

                // Add the company logo
                string imagePath = Server.MapPath("~/assets/images/images.png");
                var picture = worksheet.Drawings.AddPicture("Logo", new FileInfo(imagePath));
                picture.SetPosition(0, 2, 0, 0); // Set the position (row 1, column A)
                picture.SetSize(130, 80);

                // Merge and format cells for headers
                ExcelRange headerCell1 = worksheet.Cells["A2:B2"];
                headerCell1.Merge = true;
                headerCell1.Style.Font.Bold = true;
                headerCell1.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell1.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell1.Style.Font.Size = 16;
                headerCell1.Style.Font.Name = "Calibri";
                headerCell1.Style.Font.Color.SetColor(Color.White);
                headerCell1.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell1.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                ExcelRange headerCell5 = worksheet.Cells["A1:B1"];
                headerCell5.Merge = true;
                headerCell5.Style.Font.Bold = true;
                headerCell5.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell5.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell5.Style.Font.Size = 16;
                headerCell5.Style.Font.Name = "Calibri";
                headerCell5.Style.Font.Color.SetColor(Color.White);
                headerCell5.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell5.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                ExcelRange headerCell6 = worksheet.Cells["A3:B3"];
                headerCell6.Merge = true;
                headerCell6.Style.Font.Bold = true;
                headerCell6.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell6.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell6.Style.Font.Size = 16;
                headerCell6.Style.Font.Name = "Calibri";
                headerCell6.Style.Font.Color.SetColor(Color.White);
                headerCell6.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell6.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                ExcelRange headerCell = worksheet.Cells["C1:L1"];
                headerCell.Merge = true;
                headerCell.Value = "Baankii Hojii Gamtaa Oromiyaa";
                headerCell.Style.Font.Bold = true;
                headerCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell.Style.Font.Size = 16;
                headerCell.Style.Font.Name = "Calibri";
                headerCell.Style.Font.Color.SetColor(Color.White);
                headerCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                ExcelRange headerCell3 = worksheet.Cells["C2:L2"];
                headerCell3.Merge = true;
                headerCell3.Value = "Cooperative Bank of Oromia";
                headerCell3.Style.Font.Bold = true;
                headerCell3.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell3.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell3.Style.Font.Size = 16;
                headerCell3.Style.Font.Name = "Calibri";
                headerCell3.Style.Font.Color.SetColor(Color.White);
                headerCell3.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell3.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                ExcelRange headerCell4 = worksheet.Cells["C3:L3"];
                headerCell4.Merge = true;
                headerCell4.Value = "ኦሮሚያ ኅብራት ሥራ ባንክ";
                headerCell4.Style.Font.Bold = true;
                headerCell4.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell4.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell4.Style.Font.Size = 16;
                headerCell4.Style.Font.Name = "Calibri";
                headerCell4.Style.Font.Color.SetColor(Color.White);
                headerCell4.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell4.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                // Define header row (starting from row 5)
                worksheet.Cells[5, 1].Value = "Subscription ID";
                worksheet.Cells[5, 2].Value = "Shareholder ID";
                worksheet.Cells[5, 3].Value = "Shareholder Name";
                worksheet.Cells[5, 4].Value = "Number of Shares";
                worksheet.Cells[5, 5].Value =  "Authorized By";
                worksheet.Cells[5, 6].Value = "Paid Amount";
                worksheet.Cells[5, 7].Value = "Unpaid Amount";
                worksheet.Cells[5, 8].Value = "Due Date";
                worksheet.Cells[5, 9].Value = "Creation Date";
                worksheet.Cells[5, 10].Value = "Last Modified By";
                worksheet.Cells[5, 11].Value = "Authorization Status";
                worksheet.Cells[5, 12].Value = "Branch";

                // Apply bold style to the header row
                using (var headerCells = worksheet.Cells[5, 1, 5, 12])
                {
                    headerCells.Style.Font.Bold = true;
                    headerCells.Style.Font.Size = 12;
                    headerCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    headerCells.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    headerCells.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    headerCells.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                }

                // Add data to the worksheet
                for (int i = 0; i < subscriptions.Count; i++)
                {
                    var sub = subscriptions[i];
                    worksheet.Cells[i + 6, 1].Value = sub.SubID; // Subscription ID
                    worksheet.Cells[i + 6, 2].Value = sub.Shareholder.ShareID; // Shareholder ID
                    worksheet.Cells[i + 6, 3].Value = sub.Shareholder?.FullNameEng ?? "N/A"; // Shareholder Name
                    worksheet.Cells[i + 6, 4].Value = sub.SubNumShares ?? 0; // Number of Shares 
                    worksheet.Cells[i + 6, 5].Value = sub.User?.FullName ?? "Not Approved"; // Authorized By
                    //worksheet.Cells[i + 6, 6].Value = sub.PaidSubscription ?? 0; // Paid Amount
                    //worksheet.Cells[i + 6, 7].Value = sub.UnpaidSubscription ?? 0; // Unpaid Amount
                    // Paid Amount
                    worksheet.Cells[i + 6, 6].Value = sub.PaidSubscription ?? 0;
                    worksheet.Cells[i + 6, 6].Style.Numberformat.Format = "#,##0.00";

                    // Unpaid Amount
                    worksheet.Cells[i + 6, 7].Value = sub.UnpaidSubscription ?? 0;
                    worksheet.Cells[i + 6, 7].Style.Numberformat.Format = "#,##0.00";
                    worksheet.Cells[i + 6, 8].Value = sub.PaymentDueDate?.ToString("yyyy-MM-dd") ?? "Not Given"; // Payment Due Date
                    worksheet.Cells[i + 6, 9].Value = sub.SubDate?.ToString("yyyy-MM-dd") ?? "N/A"; // Creation Date
                    worksheet.Cells[i + 6, 10].Value = sub.User1?.FullName ?? "N/A"; // Last Modified By
                    worksheet.Cells[i + 6, 11].Value = sub.SubAuthorizationStatus ?? "N/A"; // Authorization Status
                    worksheet.Cells[i + 6, 12].Value = sub.Branch1 != null ? sub.Branch1.BranchName : "N/A";// Branch
                }

                // Autofit columns for better readability
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Get the Excel file as a byte array
                var excelData = package.GetAsByteArray();

                // Return the file as a downloadable response
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Subscriptions.xlsx");
            }
        }



        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}

