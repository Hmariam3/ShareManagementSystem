using Shareholder_Management_System.Controllers;
using Shareholder_Management_System.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
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

        [HttpPost]
        public ActionResult Addonsubscriptions(int? ShareId)
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

            return PartialView("_AddonsubscriptionsList", subscriptions.ToList());
        }




        public ActionResult PendingSubscription()
        {
            var subscribtions = db.Subscribtions.Include(s => s.Shareholder).Include(s => s.Shareholder1).Include(s => s.User).Include(s => s.User1).Where(a => a.SubAuthorizationStatus != "Approved");
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

            ViewBag.ShID = new SelectList(db.Shareholders.Where(s => s.AuthorizationStatus == "Approved").Select(s => new { ShID = s.ShID, DisplayName = s.FullNameEng + " / " + s.ShareID }), "ShID", "DisplayName");
            ViewBag.SubTransferFrom = new SelectList(db.Shareholders.Where(s => s.AuthorizationStatus == "Approved"), "ShID", "FullNameEng");
            ViewBag.SubAuthorizer = new SelectList(db.Users, "UID", "FullName");
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "SubID,ShID,SubNumShares,Premium,SubAmount,PaidSubscription,UnpaidSubscription,SubTransferFrom,PaymentDueDate,SubStatus,CreatedBy,SubDate,SubAuthorizationStatus,SubAuthorizer,AuthorizedDate,Remark")] Subscribtion subscribtion)
        {
            int branch = Convert.ToInt32(Session["ID"]);
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
                subscribtion.CreatedBy = branch;
                subscribtion.SubAuthorizer = null;
                //subscribtion.CreatedBy = Session["Username"];

                subscribtion.SubAmount = subscribtion.SubNumShares * 1000;
                subscribtion.UnpaidSubscription = subscribtion.SubAmount;

                //subscribtion.SubAuthorizer = Session["subusername"];

                db.SaveChanges();
                TempData["SuccessMessage"] = "Subscription created successfully!";

                AuditLogsController auditLogsController = new AuditLogsController();
              
                auditLogsController.RecordLog("Registration", subscribtion.SubID, "Subscribtion", subscribtion.CreatedBy ?? 0, Session["BranchName"].ToString());


                return RedirectToAction("Create");
            }

            ViewBag.ShID = new SelectList(db.Shareholders.Where(s => s.AuthorizationStatus == "Approved").Select(s => new { ShID = s.ShID, DisplayName = s.FullNameEng + " / " + s.ShareID }), "ShID", "DisplayName");
            ViewBag.SubTransferFrom = new SelectList(db.Shareholders, "ShID", "ShareID", subscribtion.SubTransferFrom);
            ViewBag.SubAuthorizer = new SelectList(db.Users, "UID", "FullName", subscribtion.SubAuthorizer);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", subscribtion.CreatedBy);
            return View(subscribtion);
        }



        /// =======================================================================================================

        public ActionResult AddSubscriprion()
        {
            //ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "FullNameEng");
            // ViewBag.SubTransferFrom = new SelectList(db.Shareholders, "ShID", "FullNameEng");
            ViewBag.ShID = new SelectList(db.Shareholders.Where(s => s.AuthorizationStatus == "Approved").Select(s => new { ShID = s.ShID, DisplayName = s.FullNameEng + " / " + s.ShareID }), "ShID", "DisplayName");
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
                    existingSubscription.SubAmount = existingSubscription.SubNumShares * 1000; // Recalculate the total amount

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
                      return RedirectToAction("Create");
                }
                else
                {
                    ModelState.AddModelError("", "No subscription found for the given Shareholder and Subscription ID.");
                }
            }

            // If the model state is not valid, reload the view with the previous data
            ViewBag.ShID = new SelectList(db.Shareholders.Where(s => s.AuthorizationStatus == "Approved").Select(s => new { ShID = s.ShID, DisplayName = s.FullNameEng + " / " + s.ShareID }), "ShID", "DisplayName");
            ViewBag.SubTransferFrom = new SelectList(db.Shareholders, "ShID", "FullNameEng", subscription.SubTransferFrom);
            ViewBag.SubAuthorizer = new SelectList(db.Users, "UID", "FullName", subscription.SubAuthorizer);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", subscription.CreatedBy);

            return View(subscription);
        }


        public ActionResult GetSubIDs(int shareholderId)
        {
            var subIDs = db.Subscribtions
                           .Where(s => s.ShID == shareholderId
                                        && s.SubAuthorizationStatus == "Approved"
                                        && s.UnpaidSubscription > 0
                                        && s.PaymentDueDate >= DateTime.Now
                                        && s.PaidSubscription.Value >= 0.25m * s.SubAmount.Value) // Ensure PaidSubscription is at least 25% of SubAmount
                           .Select(s => new { s.SubID, s.SubNumShares }) // Customize the fields as necessary
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
                subscribtion.SubAmount = subscribtion.SubNumShares * 1000;

                if (subscribtion.PaidSubscription == null)
                {
                    subscribtion.UnpaidSubscription = subscribtion.SubAmount;
                }
                else
                {
                    subscribtion.UnpaidSubscription = subscribtion.SubAmount - subscribtion.PaidSubscription;
                }

                subscribtion.CreatedBy = userdata;
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
                else if (subscribtion.SubAuthorizationStatus != "Approved")
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

        public ActionResult Authorize(int id, string action)
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
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("Approved", subscribtion.SubID, "Subscribtion", subscribtion.CreatedBy ?? 0, Session["BranchName"].ToString());
            }
            else if (action == "reject")
            {
                subscribtion.SubAuthorizationStatus = "Rejected";
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

