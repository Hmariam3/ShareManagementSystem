using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Shareholder_Management_System.Controllers;
using Shareholder_Management_System.Models;

namespace Share_Management_System.Controllers
{
    public class BlockedsController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        // GET: Blockeds
        public ActionResult Index()
        {
            var blockeds = db.Blockeds.Include(b => b.Document).Include(b => b.Payment).Include(b => b.Shareholder).Include(b => b.User).Include(b => b.User1);
            return View(blockeds.ToList());
        }

        // GET: Blockeds/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Blocked blocked = db.Blockeds.Find(id);
            if (blocked == null)
            {
                return HttpNotFound();
            }
            return View(blocked);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReleaseAmount(int id, decimal releaseAmount)
        {
            // Retrieve the Blocked record by id
            var blocked = db.Blockeds.Find(id);
            if (blocked == null)
            {
                return HttpNotFound();
            }

            // Retrieve the associated Payment record
            var payment = db.Payments.FirstOrDefault(p => p.PayID == blocked.PayID);
            if (payment == null)
            {
                return HttpNotFound();
            }

            // Validate the release amount
            if (releaseAmount <= 0 || releaseAmount > blocked.BlockedAmount)
            {
                ModelState.AddModelError("", "Invalid release amount.");
                return View("Details", blocked);
            }
            else
            {
                // Update the Blocked table
                blocked.BlockedAmount -= releaseAmount;

                // Update the Payment table
                payment.PaidAmount += releaseAmount;
                payment.BlockedAmount -= releaseAmount;

                // Save changes to the database
                db.Entry(blocked).State = EntityState.Modified;
                db.Entry(payment).State = EntityState.Modified;
                db.SaveChanges();
            }
            // Redirect to the details page or another page
            return RedirectToAction("Details", new { id = blocked.BlockID });
        }



        // GET: Blockeds/Create
        public ActionResult Create()
        {
            PopulateDropdowns();
            return View();
        }

        // POST: Blockeds/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Blocked blocked, HttpPostedFileBase uploadedFile)
        {
            PopulateDropdowns(blocked);

            if (ModelState.IsValid)
            {
                var payment = db.Payments.Find(blocked.PayID);

                if (payment == null)
                {
                    ModelState.AddModelError("Payment", "Payment not found.");
                    return View(blocked);
                }

                if (payment.PaymentAuthorizationStatus == "Pending")
                {
                    ModelState.AddModelError("PaymentAuthorizationStatus", "The payment is in the pending status.");
                    return View(blocked);
                }

                if (payment.PaidAmount <= 0)
                {
                    ModelState.AddModelError("PaidAmount", "The paid amount should be greater than zero.");
                    return View(blocked);
                }

                if (payment.PaidAmount < blocked.BlockedAmount)
                {
                    ModelState.AddModelError("BlockedAmount", "The blocking amount should be less than the paid amount.");
                    return View(blocked);
                }
                int userId = Convert.ToInt32(Session["ID"]);
                int branchId = Convert.ToInt32(Session["Branch"]);
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {

                    Document document = new Document
                    {

                        DocOwner = "Shareholder",
                        DocType = "Blocking Document",
                        ShID = payment.ShID,
                        CreatedBy = userId,
                        DocAuthorizationStatus = "Pending",
                        CreatedDate = DateTime.Now,
                    };

                    // Instantiate the DocumentsController to save the document
                    DocumentsController documentsController = new DocumentsController();
                    documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

                    int documentId = documentsController.Create(document, uploadedFile);

                    if (documentId > 0)
                    {
                        // Update blocked amount and save
                        decimal currentBlockedAmount = payment.BlockedAmount ?? 0;
                        decimal? newBlockedAmount = currentBlockedAmount + blocked.BlockedAmount;

                        blocked.BlockedBy = userId; // Placeholder for BlockedBy
                        //blocked.BlockedAuthorizer = 3; // Placeholder for BlockedAuthorizer
                        blocked.BlockingDoc = documentId; // Placeholder for BlockingDoc
                        blocked.BlockedAuthorizationStatus = "Pending";
                        blocked.AuthorizationDate = DateTime.Now;

                        db.Blockeds.Add(blocked);
                        db.SaveChanges();

                        payment.BlockedAmount = newBlockedAmount;
                        payment.PaidAmount -= blocked.BlockedAmount;
                        db.Entry(payment).State = EntityState.Modified;
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                }



                return RedirectToAction("Index");
            }

            return View(blocked);
        }

        // Helper method to populate dropdowns
        private void PopulateDropdowns(Blocked blocked = null)
        {
            ViewBag.BlockingDoc = new SelectList(db.Documents, "DocID", "DocName", blocked?.BlockingDoc);
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode", blocked?.PayID);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", blocked?.ShID);
            ViewBag.BlockedBy = new SelectList(db.Users, "UID", "FullName", blocked?.BlockedBy);
            ViewBag.BlockedAuthorizer = new SelectList(db.Users, "UID", "FullName", blocked?.BlockedAuthorizer);
        }

        // GET: Blockeds/GetPaymentsByShareholderId
        public JsonResult GetPaymentsByShareholderId(int shId)
        {
            var payments = db.Payments
                             .Where(p => p.ShID == shId)
                             .Select(p => new
                             {
                                 PayID = p.PayID,
                                 PaymentMode = p.PaymentMode,
                                 PaidAmount = p.PaidAmount,
                                 BlockedAmount = p.BlockedAmount,
                                 PaymentAuthorizationStatus = p.PaymentAuthorizationStatus
                             })
                             .ToList();
            return Json(payments, JsonRequestBehavior.AllowGet);
        }



        // GET: Blockeds/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Blocked blocked = db.Blockeds.Find(id);
            if (blocked == null)
            {
                return HttpNotFound();
            }
            ViewBag.BlockingDoc = new SelectList(db.Documents, "DocID", "DocName", blocked.BlockingDoc);
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode", blocked.PayID);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", blocked.ShID);
            ViewBag.BlockedBy = new SelectList(db.Users, "UID", "FullName", blocked.BlockedBy);
            ViewBag.BlockedAuthorizer = new SelectList(db.Users, "UID", "FullName", blocked.BlockedAuthorizer);
            return View(blocked);
        }

        // POST: Blockeds/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "BlockID,ShID,PayID,BlockedAmount,BlockingOrgan,BlockedBy,DateBlocked,BlockingDoc,RefNum,Reason,BlockedAuthorizationStatus,BlockedAuthorizer,AuthorizationDate,Remark")] Blocked blocked)
        {
            if (ModelState.IsValid)
            {
                db.Entry(blocked).State = EntityState.Modified;
                db.Entry(blocked).Property(x => x.BlockedAuthorizer).IsModified = false;
                db.Entry(blocked).Property(x => x.BlockedAuthorizationStatus).IsModified = false;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.BlockingDoc = new SelectList(db.Documents, "DocID", "DocName", blocked.BlockingDoc);
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode", blocked.PayID);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", blocked.ShID);
            ViewBag.BlockedBy = new SelectList(db.Users, "UID", "FullName", blocked.BlockedBy);
            ViewBag.BlockedAuthorizer = new SelectList(db.Users, "UID", "FullName", blocked.BlockedAuthorizer);
            return View(blocked);
        }

        // GET: Blockeds/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Blocked blocked = db.Blockeds.Find(id);
            if (blocked == null)
            {
                return HttpNotFound();
            }
            return View(blocked);
        }

        // POST: Blockeds/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Blocked blocked = db.Blockeds.Find(id);
            db.Blockeds.Remove(blocked);
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

