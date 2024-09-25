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
    public class PaymentsController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        // GET: Payments
        public ActionResult Index()
        {
            var payments = db.Payments.Include(p => p.Branch1).Include(p => p.Document).Include(p => p.Shareholder).Include(p => p.Shareholder1).Include(p => p.Subscribtion).Include(p => p.User).Include(p => p.User1);
            return View(payments.ToList());
        }

        // GET: Payments/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Payment payment = db.Payments.Find(id);
            if (payment == null)
            {
                return HttpNotFound();
            }
            return View(payment);
        }

        // GET: Payments/Create
        public ActionResult Create(int? subId)
        {
            var shareholders1 = db.Shareholders.Select(s => new SelectListItem
            {
                Value = s.ShID.ToString(),
                Text = s.FullNameEng

            }).ToList();
            shareholders1.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "Select a Shareholder"
            });

            ViewBag.Shareholders1 = shareholders1;



            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchName");
            ViewBag.PaymentSlip = new SelectList(db.Documents, "DocID", "DocName");
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "FullNameEng");
            ViewBag.PaymentTransferFrom = new SelectList(db.Shareholders, "ShID", "FullNameEng");
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "UnpaidSubscription");
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");
            ViewBag.PaymentAuthorizer = new SelectList(db.Users, "UID", "FullName");
            return View();


            //// Fetch and pass subscription details if subId is provided
            //if (subId.HasValue)
            //{
            //    var subscribtion = db.Subscribtions.Find(subId.Value);
            //    if (subscribtion != null)
            //    {
            //        ViewBag.UnpaidSubscription = subscribtion.UnpaidSubscription;
            //    }
            //}

        }
        //// GET: Payments/GetSubscriptionsByShareholder
        //public JsonResult GetSubscriptionsByShareholder(int shId)
        //{
        //    var subscriptions = db.Subscribtions
        //                          .Where(s => s.ShID == shId && s.SubAuthorizationStatus == "approved" && s.UnpaidSubscription > 00 && s.PaymentDueDate > DateTime.Now)  // Filter by ShID and SubAuthorizationStatus
        //                          .Select(s => new
        //                          {
        //                              s.SubID,
        //                              s.ShID,
        //                              s.SubNumShares,
        //                              s.SubAmount,
        //                              s.PaidSubscription,
        //                              s.UnpaidSubscription,
        //                              s.SubAuthorizationStatus,
        //                              s.SubStatus
        //                          })  // Select all the necessary fields
        //                          .ToList();

        //    return Json(subscriptions, JsonRequestBehavior.AllowGet);  // Return the filtered list as JSON
        //}
        // GET: Payments/GetSubscriptionsByShareholder
        public JsonResult GetSubscriptionsByShareholder(int shId)
        {
            var subscriptions = db.Subscribtions
                                  .Where(s => s.ShID == shId && s.SubAuthorizationStatus == "approved" && s.UnpaidSubscription > 00 && s.PaymentDueDate > DateTime.Now)  // Filter by ShID, SubAuthorizationStatus, UnpaidSubscription >0 and date
                                  .Select(sub => new
                                  {
                                      sub.SubID,
                                      sub.ShID,
                                      sub.SubNumShares,
                                      sub.SubAmount,
                                      sub.PaidSubscription,
                                      sub.UnpaidSubscription,
                                      sub.SubAuthorizationStatus,
                                      sub.SubStatus
                                  }).ToList();

            return Json(subscriptions, JsonRequestBehavior.AllowGet);
        }


        //public JsonResult GetShareholders(string term)
        //{
        //    // Log or inspect the incoming term to ensure it's passed correctly
        //    if (string.IsNullOrWhiteSpace(term))
        //    {
        //        // If no search term, return an empty list
        //        return Json(new List<object>(), JsonRequestBehavior.AllowGet);
        //    }

        //    // Search for shareholders based on the term
        //    var shareholders = db.Shareholders
        //                         .Where(s => s.FullNameEng.Contains(term) || s.PhoneNo.Contains(term))
        //                         .Select(s => new
        //                         {
        //                             Value = s.ShID, // This will be the value in the dropdown
        //                     Text = s.FullNameEng // This will be the displayed name in the dropdown
        //                 })
        //                         .ToList();

        //    // Check if any shareholders were found
        //    if (!shareholders.Any())
        //    {
        //        // Return a message if no shareholders were found
        //        return Json(new { message = "No shareholders found" }, JsonRequestBehavior.AllowGet);
        //    }

        //    // Return the result as JSON
        //    return Json(shareholders, JsonRequestBehavior.AllowGet);
        //}


        // POST: Payments/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "PayID,ShID,SubID,PaymentMode,Branch,PaidAmount,BlockedAmount,ReferenceNum," +
            "PaymentSlip,PaymentDate,PaymentTransferFrom,CreatedBy,CreationDate,PaymentAuthorizationStatus,PaymentAuthorizer," +
            "AuthorizationDate,Remark")] Payment payment, HttpPostedFileBase uploadedFile, int[] selectedSubscriptions,
            string cashAmount, string cpoAmount, string dividendAmount, string chequeAmount, string AccountAmount)
        {



            if (ModelState.IsValid)
            {
                // Prepare a string to hold payment details
                var PaymentMode = new List<string>();

                if (!string.IsNullOrEmpty(cashAmount))
                {
                    PaymentMode.Add($"Cash = {cashAmount}");
                }
                if (!string.IsNullOrEmpty(AccountAmount))
                {
                    PaymentMode.Add($"Account = {AccountAmount}");
                }
                if (!string.IsNullOrEmpty(cpoAmount))
                {
                    PaymentMode.Add($"CPO = {cpoAmount}");
                }
                if (!string.IsNullOrEmpty(dividendAmount))
                {
                    PaymentMode.Add($"Dividend = {dividendAmount}");
                }
                if (!string.IsNullOrEmpty(chequeAmount))
                {
                    PaymentMode.Add($"Cheque = {chequeAmount}");
                }

                // Join all payment details into a single string
                payment.PaymentMode = string.Join(", ", PaymentMode);

                // Handle document creation
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    Document document = new Document
                    {
                        DocOwner = "Shareholder",
                        DocType = "Payment Slip",
                        ShID = payment.ShID,
                        CreatedBy = 2,
                        DocAuthorizationStatus = "Pending",
                        CreatedDate = DateTime.Now,
                    };

                    // Instantiate the DocumentsController to save the document
                    DocumentsController documentsController = new DocumentsController();
                    documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

                    int documentId = documentsController.Create(document, uploadedFile);

                    if (documentId > 0)
                    {
                        // Assign the Document ID to the PaymentSlip property of the payment
                        payment.CreationDate = DateTime.Now;
                        payment.CreatedBy = 2;
                        payment.PaymentAuthorizationStatus = "pending";
                        payment.Branch = 100;
                        payment.PaymentSlip = documentId;

                        // Process the selected subscription ID (SubID is now bound to the payment object)
                        var selectedSubID = payment.SubID;

                        // Retrieve the associated Subscribtion entity
                        var subscribtion = db.Subscribtions.Find(payment.SubID);
                        if (!string.IsNullOrEmpty(subscribtion.ToString()))
                        {

                            if (payment.PaidAmount > subscribtion.UnpaidSubscription)
                            {
                                TempData["Message"] = "Paid Amount cannot be greater than Unpaid Subscription.";
                                return RedirectToAction("Create");
                                //ModelState.AddModelError("PaidAmount", "Paid Amount cannot be greater than Unpaid Subscription.");
                            }
                            else
                            {
                                // Update the PaidSubscription and UnpaidSubscription properties
                                subscribtion.PaidSubscription += payment.PaidAmount; // Increase PaidSubscription
                                subscribtion.UnpaidSubscription -= payment.PaidAmount; // Decrease UnpaidSubscription

                                // Check if the UnpaidSubscription is 0.00 and update SubStatus
                                if (subscribtion.UnpaidSubscription <= 0.00m)
                                {
                                    subscribtion.UnpaidSubscription = 0.00m; // Ensure no negative values
                                    subscribtion.SubStatus = "Fully Paid";
                                }

                                // Mark the subscribtion as modified
                                db.Entry(subscribtion).State = EntityState.Modified;

                                // Save the payment
                                db.Payments.Add(payment);
                                db.SaveChanges();
                                // Call RecordLog method
                                AuditLogsController auditLogsController = new AuditLogsController();
                                //auditLogsController.RecordLog("register", payment.PayID, "Payment", payment.CreatedBy, payment.User.Branch);


                                return RedirectToAction("Index");
                            }

                        }

                    }
                    else
                    {
                        // Handle the error case where the document was not created successfully
                        ModelState.AddModelError("", "Document could not be created. Please try again.");
                    }
                }

                return RedirectToAction("Index");
            }
            var shareholders1 = db.Shareholders.Select(s => new SelectListItem
            {
                Value = s.ShID.ToString(),
                Text = s.FullNameEng
            }).ToList();
            shareholders1.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "Select a Shareholder"
            });

            ViewBag.Shareholders1 = shareholders1;


            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", payment.Branch);
            //ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", payment.ShID);

            // Assuming you have a method that retrieves the list of shareholders
            var shareholders = db.Shareholders.Select(sh => new SelectListItem
            {
                Value = sh.ShID.ToString(),
                Text = sh.FullNameEng
            }).ToList();

            ViewBag.ShID = shareholders;

            ViewBag.PaymentTransferFrom = new SelectList(db.Shareholders, "ShID", "ShareID", payment.PaymentTransferFrom);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus", payment.SubID);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", payment.CreatedBy);
            ViewBag.PaymentAuthorizer = new SelectList(db.Users, "UID", "FullName", payment.PaymentAuthorizer);

            return View(payment);
        }
        // GET: Payments/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Payment payment = db.Payments.Find(id);
            if (payment == null)
            {
                return HttpNotFound();
            }
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", payment.Branch);
            ViewBag.PaymentSlip = new SelectList(db.Documents, "DocID", "DocName", payment.PaymentSlip);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", payment.ShID);
            ViewBag.PaymentTransferFrom = new SelectList(db.Shareholders, "ShID", "ShareID", payment.PaymentTransferFrom);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus", payment.SubID);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", payment.CreatedBy);
            ViewBag.PaymentAuthorizer = new SelectList(db.Users, "UID", "FullName", payment.PaymentAuthorizer);
            return View(payment);
        }

        // POST: Payments/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "PayID,ShID,SubID,PaymentMode,Branch,PaidAmount,BlockedAmount,ReferenceNum,PaymentSlip,PaymentDate,PaymentTransferFrom,CreatedBy,CreationDate,PaymentAuthorizationStatus,PaymentAuthorizer,AuthorizationDate,Remark")] Payment payment)
        {

            if (ModelState.IsValid)
            {
                db.Entry(payment).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", payment.Branch);
            ViewBag.PaymentSlip = new SelectList(db.Documents, "DocID", "DocName", payment.PaymentSlip);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", payment.ShID);
            ViewBag.PaymentTransferFrom = new SelectList(db.Shareholders, "ShID", "ShareID", payment.PaymentTransferFrom);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus", payment.SubID);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", payment.CreatedBy);
            ViewBag.PaymentAuthorizer = new SelectList(db.Users, "UID", "FullName", payment.PaymentAuthorizer);
            return View(payment);
        }

        // GET: Payments/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Payment payment = db.Payments.Find(id);
            if (payment == null)
            {
                return HttpNotFound();
            }
            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Payment payment = db.Payments.Find(id);
            db.Payments.Remove(payment);
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
