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
        public ActionResult FilterPending()
        {
            var payments = db.Payments.Include(p => p.Branch1).Include(p => p.Document).Include(p => p.Shareholder).Include(p => p.Shareholder1).Include(p => p.Subscribtion).Include(p => p.User).Include(p => p.User1).Where(a => a.PaymentAuthorizationStatus != "Approved");
            return View(payments.ToList());
        }
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


        }
    
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

                int userId = Convert.ToInt32(Session["ID"]);
                int branchId = Convert.ToInt32(Session["Branch"]);

                // Handle document creation
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {

                    Document document = new Document
                    {

                        DocOwner = "Shareholder",
                        DocType = "Payment Slip",
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
                        // Assign the Document ID to the PaymentSlip property of the payment
                        var formattedDateTime = payment.CreationDate.HasValue
                            ? payment.CreationDate.Value.ToString("MM/dd/yyyy hh:mm:ss tt")
                            : "No Date";


                        payment.CreationDate = DateTime.UtcNow;
                        payment.CreatedBy = userId;
                        payment.PaymentAuthorizationStatus = "pending";
                        payment.Branch = branchId;
                        payment.PaymentSlip = documentId;



                        // Save the payment
                        db.Payments.Add(payment);
                        db.SaveChanges();

                        //string branchName = db.Branches.FirstOrDefault(b => b.ID == payment.Branch)?.BranchName ?? "Unknown Branch";

                        // Call RecordLog method with null-safe value for CreatedBy
                        AuditLogsController auditLogsController = new AuditLogsController();
                        auditLogsController.RecordLog("Registration", payment.PayID, "Payment", payment.CreatedBy ?? 0, Session["BranchName"].ToString());

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

            // Find the payment record
            Payment payment = db.Payments.Find(id);
            if (payment == null)
            {
                return HttpNotFound();
            }

            // Retrieve Shareholders for the dropdown and pre-select the existing one
            var subscriptions = db.Subscribtions
       .Where(s => s.ShID == payment.ShID) // Replace with your actual logic to get subscriptions
       .ToList();
            // Populate Shareholders for dropdown
            ViewBag.Shareholders1 = db.Shareholders
                .Select(s => new SelectListItem
                {
                    Value = s.ShID.ToString(),
                    Text = s.FullNameEng // Adjust this according to your model
                }).ToList();
            var viewModel = new Payment
            {
                SubID = payment.SubID,
                ReferenceNum = payment.ReferenceNum,
                Remark = payment.Remark,
              
                PaidAmount = payment.PaidAmount,
                PaymentDate = payment.PaymentDate,
                ShID = payment.ShID, // Current selected Shareholder ID
               
            };
            // Pass subscriptions to the view
            ViewBag.Subscriptions = subscriptions;

            return View(viewModel);
        }

        // POST: Payments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, [Bind(Include = "PayID,ShID,SubID,PaymentMode,PaidAmount,BlockedAmount,ReferenceNum,PaymentSlip,PaymentDate,PaymentTransferFrom,CreatedBy,CreationDate,PaymentAuthorizationStatus,PaymentAuthorizer,AuthorizationDate,Remark")] Payment payment, HttpPostedFileBase uploadedFile, int[] selectedSubscriptions, string cashAmount, string cpoAmount, string dividendAmount, string chequeAmount, string AccountAmount)
        {
            if (ModelState.IsValid)
            {
                // Retrieve the existing payment record
                var existingPayment = db.Payments.Find(id);
                if (existingPayment == null)
                {
                    return HttpNotFound();
                }

                // Update existing payment properties
                existingPayment.ShID = payment.ShID;
                existingPayment.SubID = payment.SubID;
                existingPayment.PaidAmount = payment.PaidAmount;
                existingPayment.BlockedAmount = payment.BlockedAmount;
                existingPayment.ReferenceNum = payment.ReferenceNum;
                existingPayment.PaymentDate = payment.PaymentDate;
                existingPayment.PaymentTransferFrom = payment.PaymentTransferFrom;
                existingPayment.Remark = payment.Remark;

                // Handle updating payment modes
                var paymentModes = new List<string>();
                if (!string.IsNullOrEmpty(cashAmount)) paymentModes.Add($"Cash = {cashAmount}");
                if (!string.IsNullOrEmpty(AccountAmount)) paymentModes.Add($"Account = {AccountAmount}");
                if (!string.IsNullOrEmpty(cpoAmount)) paymentModes.Add($"CPO = {cpoAmount}");
                if (!string.IsNullOrEmpty(dividendAmount)) paymentModes.Add($"Dividend = {dividendAmount}");
                if (!string.IsNullOrEmpty(chequeAmount)) paymentModes.Add($"Cheque = {chequeAmount}");
                existingPayment.PaymentMode = string.Join(", ", paymentModes);

                // Update user and branch details
                int userId = Convert.ToInt32(Session["ID"]);
                existingPayment.CreatedBy = userId;
                existingPayment.Branch = Convert.ToInt32(Session["Branch"]);
                existingPayment.CreationDate = DateTime.Now;
                existingPayment.PaymentAuthorizationStatus = "pending";

                // Handle document creation (if file uploaded)
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    Document document = new Document
                    {
                        DocOwner = "Shareholder",
                        DocType = "Payment Slip",
                        ShID = existingPayment.ShID,
                        CreatedBy = userId,
                        DocAuthorizationStatus = "Pending",
                        CreatedDate = DateTime.Now
                    };

                    DocumentsController documentsController = new DocumentsController();
                    documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

                    int documentId = documentsController.Create(document, uploadedFile);
                    if (documentId > 0)
                    {
                        existingPayment.PaymentSlip = documentId;
                    }
                    else
                    {
                        ModelState.AddModelError("", "Document could not be created. Please try again.");
                    }
                }

                // Save the updated payment
                db.Entry(existingPayment).State = EntityState.Modified;
                db.SaveChanges();

                // Log the update
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("Edit", existingPayment.PayID, "Payment", existingPayment.CreatedBy ?? 0, Session["BranchName"].ToString());

                return RedirectToAction("Index");
            }

            // Re-populate dropdowns if validation fails
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
            ViewBag.PaymentSlip = new SelectList(db.Documents, "DocID", "DocName", payment.PaymentSlip);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "FullNameEng", payment.ShID);
            ViewBag.PaymentTransferFrom = new SelectList(db.Shareholders, "ShID", "FullNameEng", payment.PaymentTransferFrom);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "UnpaidSubscription", payment.SubID);
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
            //string branchName = db.Branches.FirstOrDefault(b => b.ID == payment.Branch)?.BranchName ?? "Unknown Branch";

            // Call RecordLog method with null-safe value for CreatedBy
            AuditLogsController auditLogsController = new AuditLogsController();
            auditLogsController.RecordLog("Edit", payment.PayID, "Payment", payment.CreatedBy ?? 0, Session["BranchName"].ToString());

            db.Payments.Remove(payment);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Authorize(int? id, string action)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Payment paymets = db.Payments.Find(id);
            if (paymets == null)
            {
                return HttpNotFound();
            }
            return View(paymets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        public ActionResult Authorize(int id, string action)
        {
            Payment payment = db.Payments.Find(id);
            if (payment == null)
            {
                return HttpNotFound();
            }


            if (action == "approve")
            {
                payment.PaymentAuthorizationStatus = "Approved";

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
                        //db.Payments.Add(paymets);
                        db.SaveChanges();
                        //string branchName = db.Branches.FirstOrDefault(b => b.ID == payment.Branch)?.BranchName ?? "Unknown Branch";

                        // Call RecordLog method with null-safe value for CreatedBy
                        AuditLogsController auditLogsController = new AuditLogsController();
                        auditLogsController.RecordLog("Approval", payment.PayID, "Payment", payment.CreatedBy ?? 0, Session["BranchName"].ToString());


                        return RedirectToAction("Index");
                    }

                }
            }
            else if (action == "reject")
            {
               //string branchName = db.Branches.FirstOrDefault(b => b.ID == payment.Branch)?.BranchName ?? "Unknown Branch";

                // Call RecordLog method with null-safe value for CreatedBy
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("Rejection", payment.PayID, "Payment", payment.CreatedBy ?? 0, Session["BranchName"].ToString());
                payment.PaymentAuthorizationStatus = "Rejected";              
            }


            db.Entry(payment).Property(u => u.PaymentAuthorizationStatus).IsModified = true;
            db.SaveChanges();

            return RedirectToAction("FilterPending");
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
