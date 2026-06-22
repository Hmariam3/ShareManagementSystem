using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Shareholder_Management_System.Models;

namespace Shareholder_Management_System.Controllers
{
    public class AddOnSubsController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        // GET: AddOnSubs
        // GET: AddOnSubs
        // GET: AddOnSubs
        public ActionResult Index()
        {
            var addOnSubs = db.AddOnSubs
                .Include(a => a.Shareholder)
                .Include(a => a.Subscribtion)
                .Include(a => a.User)
                .Include(a => a.User1)
                .OrderByDescending(a => a.createddate)
                .ToList();   // Keep full entities

            return View(addOnSubs);
        }
        // GET: AddOnSubs/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AddOnSub addOnSub = db.AddOnSubs.Find(id);
            if (addOnSub == null)
            {
                return HttpNotFound();
            }
            return View(addOnSub);
        }

        // GET: AddOnSubs/Create
        public ActionResult Create()
        {
            ViewBag.shareid = new SelectList(db.Shareholders, "ShID", "ShareID");
            ViewBag.subid = new SelectList(db.Subscribtions, "SubID", "SubStatus");
            ViewBag.createdby = new SelectList(db.Users, "UID", "FullName");
            ViewBag.authorizer = new SelectList(db.Users, "UID", "FullName");
            return View();
        }

        // POST: AddOnSubs/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "shareid,subid,No_of_share")] AddOnSub addOnSub)
        {
            int userdata = Convert.ToInt32(Session["ID"]);
            if (ModelState.IsValid)
            {
                // Calculate amount
                addOnSub.amount = addOnSub.No_of_share * 100;

                // Default status
                addOnSub.status = "Pending";

                // Audit fields
                addOnSub.createddate = DateTime.Now;

                // Use whatever session value you currently use in your system
                addOnSub.createdby = userdata;

                // Not yet authorized
                addOnSub.authorizer = null;
                addOnSub.authorizationdate = null;

                db.AddOnSubs.Add(addOnSub);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Addon subscription submitted successfully.";

                return RedirectToAction("Index");
            }

            return View(addOnSub);
        }

        // GET: AddOnSubs/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AddOnSub addOnSub = db.AddOnSubs.Find(id);
            if (addOnSub == null)
            {
                return HttpNotFound();
            }
            ViewBag.shareid = new SelectList(db.Shareholders, "ShID", "ShareID", addOnSub.shareid);
            ViewBag.subid = new SelectList(db.Subscribtions, "SubID", "SubStatus", addOnSub.subid);
            ViewBag.createdby = new SelectList(db.Users, "UID", "FullName", addOnSub.createdby);
            ViewBag.authorizer = new SelectList(db.Users, "UID", "FullName", addOnSub.authorizer);
            return View(addOnSub);
        }

        // POST: AddOnSubs/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "add_id,shareid,subid,No_of_share,amount,status,createddate,createdby,authorizer,authorizationdate")] AddOnSub addOnSub)
        {
            if (ModelState.IsValid)
            {
                db.Entry(addOnSub).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.shareid = new SelectList(db.Shareholders, "ShID", "ShareID", addOnSub.shareid);
            ViewBag.subid = new SelectList(db.Subscribtions, "SubID", "SubStatus", addOnSub.subid);
            ViewBag.createdby = new SelectList(db.Users, "UID", "FullName", addOnSub.createdby);
            ViewBag.authorizer = new SelectList(db.Users, "UID", "FullName", addOnSub.authorizer);
            return View(addOnSub);
        }



        // GET: AddOnSubs/Authorize
        public ActionResult Authorize()
        {

            var pendingAddOnSubs = db.AddOnSubs
                .Include(a => a.Shareholder)
                .Include(a => a.Subscribtion)
                .Include(a => a.User)
                .Include(a => a.User1)
                .Where(a => a.status == "Pending")           // Only show Pending records
                .OrderByDescending(a => a.createddate)
                .ToList();

            return View(pendingAddOnSubs);
        }

        // GET: AddOnSubs/Authorize_Details/5
        public ActionResult Authorize_Details(int id)
        {
            var addOnSub = db.AddOnSubs
                    .AsNoTracking()   // ← Add this
                    .Include(a => a.Shareholder)
                    .Include(a => a.Subscribtion)
                    .Include(a => a.User)
                    .Include(a => a.User1)
                    .FirstOrDefault(a => a.add_id == id);

            if (addOnSub == null)
            {
                TempData["ErrorMessage"] = "Record not found.";
                return RedirectToAction("Authorize");
            }

            // Only allow authorization if status is Pending
            if (addOnSub.status != "Pending")
            {
                TempData["ErrorMessage"] = "This record has already been authorized.";
                return RedirectToAction("Authorize");
            }

            return View(addOnSub);
        }

        // POST: AddOnSubs/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            if (Session["ID"] == null)
            {
                TempData["ErrorMessage"] = "Session expired. Please login again.";
                return RedirectToAction("Login", "Account"); // Adjust route if needed
            }

            // Load AddOnSub with subid
            var addOnSub = db.AddOnSubs.Find(id);

            if (addOnSub == null)
            {
                TempData["ErrorMessage"] = "AddOnSub record not found.";
                return RedirectToAction("Authorize");
            }

            if (addOnSub.status != "Pending")
            {
                TempData["ErrorMessage"] = "This record has already been processed.";
                return RedirectToAction("Authorize");
            }

            int currentUserId = Convert.ToInt32(Session["ID"]);

            // === Update AddOnSub ===
            addOnSub.status = "Approved";
            addOnSub.authorizationdate = DateTime.Now;
            addOnSub.authorizer = currentUserId;        // Using your column name

            // === Update Subscription Table using subid ===
            if (addOnSub.subid.HasValue)
            {
                var subscription = db.Subscribtions.Find(addOnSub.subid.Value);

                if (subscription != null)
                {
                    // Add No. of Shares to existing SubNumShares
                    subscription.SubNumShares = (subscription.SubNumShares ?? 0) + (addOnSub.No_of_share ?? 0);

                    // Add amount to SubAmount
                    subscription.SubAmount = (subscription.SubAmount ?? 0) + (addOnSub.amount ?? 0);

                    // Add full amount to UnpaidSubscription
                    subscription.UnpaidSubscription = (subscription.UnpaidSubscription ?? 0) + (addOnSub.amount ?? 0);

                    // Change sub status 
                    subscription.SubStatus = "Partial Paid";

                }
                else
                {
                    TempData["ErrorMessage"] = "Related Subscription not found.";
                    return RedirectToAction("Authorize");
                }
            }
            else
            {
                TempData["ErrorMessage"] = "No Subscription ID linked to this AddOnSub.";
                return RedirectToAction("Authorize");
            }

            db.SaveChanges();

            TempData["SuccessMessage"] = "AddOnSub successfully Approved and Subscription updated!";
            return RedirectToAction("Authorize");
        }

        // POST: AddOnSubs/Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int id)
        {
            if (Session["ID"] == null)
            {
                TempData["ErrorMessage"] = "Session expired. Please login again.";
                return RedirectToAction("Login", "Account"); // Adjust route if needed
            }

            var addOnSub = db.AddOnSubs.Find(id);

            if (addOnSub == null)
            {
                TempData["ErrorMessage"] = "Record not found.";
                return RedirectToAction("Authorize");
            }

            if (addOnSub.status != "Pending")
            {
                TempData["ErrorMessage"] = "This record has already been processed.";
                return RedirectToAction("Authorize");
            }

            int currentUserId = Convert.ToInt32(Session["ID"]);
            addOnSub.status = "Rejected";
            addOnSub.authorizationdate = DateTime.Now;
            addOnSub.authorizer = currentUserId;

            db.SaveChanges();

            TempData["ErrorMessage"] = "AddOnSub has been Rejected.";
            return RedirectToAction("Authorize");
        }
        // GET: AddOnSubs/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AddOnSub addOnSub = db.AddOnSubs.Find(id);
            if (addOnSub == null)
            {
                return HttpNotFound();
            }
            return View(addOnSub);
        }

        // POST: AddOnSubs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            AddOnSub addOnSub = db.AddOnSubs.Find(id);
            db.AddOnSubs.Remove(addOnSub);
            db.SaveChanges();
            return RedirectToAction("Index");
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
