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
            var blockeds = db.Blockeds.Include(b => b.Payment).Include(b => b.Shareholder).Include(b => b.User).Include(b => b.User1);
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
        // GET: Blockeds/Details/5
        public ActionResult AuthorizationDetails(int? id)
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
            return RedirectToAction("Index", new { id = blocked.BlockID });
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
            int Createdby = Convert.ToInt32(Session["ID"]);
            int Approveddby = Convert.ToInt32(Session["ID"]);
            if (ModelState.IsValid)
            {
                var payment = db.Payments.Find(blocked.PayID);

                if (payment == null)
                {
                    ModelState.AddModelError("Payment", "Payment not found.");
                    return View(blocked);
                }
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    Document document = new Document
                    {
                        DocOwner = "Shareholder",
                        DocType = "Blocking Document",
                        ShID = payment.ShID,
                        CreatedBy = Createdby,
                        DocAuthorizationStatus = "Pending",
                        CreatedDate = DateTime.Now,
                    };

                    // Instantiate the DocumentsController to save the document
                    DocumentsController documentsController = new DocumentsController();
                    documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

                    int documentId = documentsController.Create(document, uploadedFile);

                    if (documentId > 0)
                    {
                        decimal currentBlockedAmount = payment.BlockedAmount ?? 0;
                        decimal newBlockedAmount = currentBlockedAmount + (blocked.BlockedAmount ?? 0);

                        blocked.BlockedBy = Createdby; // Placeholder for BlockedBy
                        blocked.BlockedAuthorizer = Approveddby; // Placeholder for BlockedAuthorizer
                        blocked.BlockingDoc = documentId; // Placeholder for BlockingDoc
                        blocked.DateBlocked = DateTime.Now;
                        blocked.BlockedAuthorizationStatus = "Pending";

                        db.Blockeds.Add(blocked);
                        db.SaveChanges();

                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    ModelState.AddModelError("uploadedFile", "You should upload a file.");
                    return View(blocked); // Return the view with the validation error
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
            ViewBag.ShID = new SelectList(db.Shareholders.Select(s => new {
                s.ShID,
                FullNameWithID = s.FullNameEng + " (" + s.ShID + ")"
            }), "ShID", "FullNameWithID", blocked?.ShID);
            ViewBag.BlockedBy = new SelectList(db.Users, "UID", "FullName", blocked?.BlockedBy);
            ViewBag.BlockedAuthorizer = new SelectList(db.Users, "UID", "FullName", blocked?.BlockedAuthorizer);
        }

        // GET: Blockeds/GetPaymentsByShareholderId
        public JsonResult GetPaymentsByShareholderId(int shId)
        {
            var payments = db.Payments
                             .Where(p => p.ShID == shId && p.PaidAmount > 0 && p.PaymentAuthorizationStatus == "Approved")
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
        public ActionResult Edit(Blocked blocked)
        {
            try
            {
                var existingBlocked = db.Blockeds.Find(blocked.BlockID);
                if (existingBlocked == null)
                    return HttpNotFound();

                // Update editable fields
                existingBlocked.BlockedAmount = blocked.BlockedAmount;
                existingBlocked.BlockingOrgan = blocked.BlockingOrgan;
                existingBlocked.RefNum = blocked.RefNum;
                existingBlocked.Reason = blocked.Reason;
                existingBlocked.Remark = blocked.Remark;

                // Set BlockedAuthorizationStatus to "Pending"
                existingBlocked.BlockedAuthorizationStatus = "Pending";

                // Save changes to the database
                db.SaveChanges();

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error saving changes: {ex.Message}");
                return View(blocked);
            }
        }


        // GET: FilterPending
        public ActionResult FilterPending()
        {
            // Fetch all blocked records and filter those with 'Pending' status
            var pendingRecords = db.Blockeds.Where(b => b.BlockedAuthorizationStatus == "Pending").ToList();
            return View(pendingRecords);
        }

        [HttpGet]
        public ActionResult GetBlockDetails(int blockId)
        {
            try
            {
                var blocked = db.Blockeds.Find(blockId);

                if (blocked == null)
                {
                    return Json(new { success = false, message = "Blocked entry not found." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        BlockID = blocked.BlockID,
                        BlockedAmount = blocked.BlockedAmount,
                        PayID = blocked.PayID,
                        // Add any additional fields needed
                    }
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                // Log the exception
                // LogError(ex); // Implement logging as needed

                return Json(new { success = false, message = "An error occurred while retrieving the block details." }, JsonRequestBehavior.AllowGet);
            }
        }


        [HttpPost]
        public ActionResult ConfirmBlockApproval(int BlockID, int PayID, decimal BlockedAmount)
        {
            int Createdby = Convert.ToInt32(Session["ID"]);
            int Approveddby = Convert.ToInt32(Session["ID"]);
            try
            {
                // Find the blocked record by BlockID
                var blocked = db.Blockeds.Find(BlockID);
                if (blocked == null)
                    return Json(new { success = false, message = "Blocked entry not found." });

                // Update BlockedAuthorizationStatus to "Approved"
                blocked.BlockedAuthorizationStatus = "Approved";
                blocked.AuthorizationDate = DateTime.Now;

                // Find the payment record by PayID
                var payment = db.Payments.Find(PayID);
                if (payment == null)
                    return Json(new { success = false, message = "Payment entry not found." });

                // Update PaidAmount and BlockedAmount in the Payment table

                if (payment.BlockedAmount == null)
                {
                    payment.BlockedAmount = BlockedAmount;
                }
                else
                {
                    payment.BlockedAmount += BlockedAmount;
                }
                payment.PaidAmount -= BlockedAmount;

                // Update the Document Status
                var document = db.Documents.FirstOrDefault(d => d.DocID == blocked.BlockingDoc);
                if (document != null)
                {
                    document.DocAuthorizationStatus = "Approved";
                    document.DocAuthorizer = Approveddby;
                    document.DocAuthorizationDate = DateTime.Now;
                }
                    // Save changes to the database
                    db.SaveChanges();



                // Return success response
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Return error message if exception occurs
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult ConfirmBlockReject(int BlockID, string Reason)
        {
            try
            {
                int Createdby = Convert.ToInt32(Session["ID"]);
                int Approveddby = Convert.ToInt32(Session["ID"]);
                // Find the blocked record by BlockID
                var blocked = db.Blockeds.Find(BlockID);
                if (blocked == null)
                    return Json(new { success = false, message = "Blocked entry not found." });
                //blocked.BlockedAmount = 0;
                // Update BlockedAuthorizationStatus to "Approved"
                blocked.BlockedAuthorizationStatus = "Rejected";
                blocked.Remark = Reason;
                blocked.AuthorizationDate = DateTime.Now;


                var document = db.Documents.FirstOrDefault(d => d.DocID == blocked.BlockingDoc);
                if (document != null)
                {
                    document.DocAuthorizationStatus = "Rejected";
                    document.DocAuthorizer = Approveddby;
                    document.DocAuthorizationDate = DateTime.Now;
                }
                // Save changes to the database
                db.SaveChanges();

                // Return success response
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Return error message if exception occurs
                return Json(new { success = false, message = ex.Message });
            }
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

