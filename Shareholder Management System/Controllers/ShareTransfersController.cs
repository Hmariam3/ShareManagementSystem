
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
    public class ShareTransfersController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        //// GET: ShareTransfers
        public ActionResult Index()
        {
            var shareTransfers = db.ShareTransfers.Include(s => s.Shareholder).Include(s => s.Shareholder1).Include(s => s.User).Include(s => s.User1);
            return View(shareTransfers.ToList());
        }

        // GET: ShareTransfers/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ShareTransfer shareTransfer = db.ShareTransfers.Find(id);
            if (shareTransfer == null)
            {
                return HttpNotFound();
            }
            return View(shareTransfer);
        }

        // GET: ShareTransfers/Authorize
        public ActionResult Authorize(int? id)
        {
            var shareTransfers = db.ShareTransfers
                .Include(s => s.Shareholder)
                .Include(s => s.Shareholder1)
                .Include(s => s.User)
                .Include(s => s.User1)
                .Where(s => s.TransferAuthorizationStatus == "Pending"); // Filter for pending status

            return View(shareTransfers.ToList());
        }



        // GET: ShareTransfers Authorize Details
        public ActionResult Authorize_Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ShareTransfer shareTransfer = db.ShareTransfers.Find(id);
            if (shareTransfer == null)
            {
                return HttpNotFound();
            }
            return View(shareTransfer);
        }


        // GET: ShareTransfers/Create
        public ActionResult Create()
        {
            // Fetch shareholders and order alphabetically, but convert to list first
            var shareholders = db.Shareholders
                                 .OrderBy(s => s.FullNameEng).Where(s => s.Status.Equals("Active") && s.AuthorizationStatus.Equals("Approved")) // Sort alphabetically
                                 .ToList() // Convert to list to work with in-memory LINQ
                                 .Select(s => new SelectListItem
                                 {
                                     Value = s.ShID.ToString(),
                                     Text = $"{s.FullNameEng} ({s.ShareID})" // Concatenate name and ID
                                 }).ToList();

            // Insert a default "Select Shareholder" option
            shareholders.Insert(0, new SelectListItem { Value = "", Text = "Select Shareholder" });

            ViewBag.ShareholdersFrom = new SelectList(shareholders, "Value", "Text");
            ViewBag.ShareholdersTo = new SelectList(shareholders, "Value", "Text");

            // Other ViewBag items
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");
            ViewBag.TransferAuthorizer = new SelectList(db.Users, "UID", "FullName");
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchName");

            return View();
        }

        // AJAX Action to update only the "Transfer To" dropdown based on "Transfer From" selection
        public JsonResult UpdateTransferToDropdown(int? transferrorId)
        {
            var shareholders = db.Shareholders
                                 .OrderBy(s => s.FullNameEng).Where(s => s.Status.Equals("Active") && s.AuthorizationStatus.Equals("Approved")) // Sort alphabetically
                                 .ToList() // Convert to list to work with in-memory LINQ
                                 .Select(s => new SelectListItem
                                 {
                                     Value = s.ShID.ToString(),
                                     Text = $"{s.FullNameEng} ({s.ShareID})" // Concatenate name and ID
                                 }).ToList();

            // Filter out the selected "Transfer From" shareholder from "Transfer To" options
            var toList = shareholders.Where(s => s.Value != transferrorId?.ToString()).ToList();

            return Json(toList, JsonRequestBehavior.AllowGet);
        }



        // GET: ShareTransfers/GetShareholderData
        public JsonResult GetShareholderData(int shareholderId)
        {

            // Fetch subscriptions where SubNumShares is not 0 for the selected shareholder
            var subscriptions = db.Subscribtions
                .Where(s => s.ShID == shareholderId && s.SubNumShares != 0 && s.SubAuthorizationStatus == "Approved")
                .Select(s => new
                {
                    s.SubID,
                    s.SubNumShares,
                    s.PaidSubscription,
                    s.UnpaidSubscription
                })
                .ToList();
            // Return only subscriptions
            return Json(new { subscriptions }, JsonRequestBehavior.AllowGet);
        }

        // GET: ShareTransfers/GetPaymentsBySubscription
        public JsonResult GetPaymentsBySubscription(int subscriptionId)
        {

            // Fetch payments where PaidAmount is not 0 and BlockedAmount is null
            var payments = db.Payments
                .Where(p => p.SubID == subscriptionId && p.PaidAmount != 0 && p.PaymentAuthorizationStatus == "Approved")
                .Select(p => new
                {
                    p.PayID,
                    p.PaidAmount,
                    p.BlockedAmount
                })
                .ToList();
            return Json(new { payments }, JsonRequestBehavior.AllowGet);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TransferID,TransferrorShID,TransfareeShID,TransferCategory,NumSharesTransferred,AmountPerShare,PaidAmountForTransfer,TransferReason,DividenedFor,TransferDoc,TransferDate,CreatedBy,CreationDate,TransferAuthorizationStatus,TransferAuthorizer,TransferAuthorizationDate,Remark,Branch")] ShareTransfer shareTransfer, string selectedSubscriptionIds, string selectedPaymentIds, string TransferType, HttpPostedFileBase uploadedFile)
        {
            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        int UserID = Convert.ToInt32(Session["ID"]);
                        int BranchId = Convert.ToInt32(Session["Branch"]);

                        // Handle document creation
                        if (uploadedFile != null && uploadedFile.ContentLength > 0)
                        {

                            Document document = new Document
                            {

                                DocOwner = "Shareholder",
                                DocType = "Transfer Document",
                                ShID = shareTransfer.TransferrorShID,
                                CreatedBy = UserID,
                                DocAuthorizationStatus = "Pending",
                                CreatedDate = DateTime.Now,
                            };

                            // Instantiate the DocumentsController to save the document
                            DocumentsController documentsController = new DocumentsController();
                            documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

                            int documentId = documentsController.Create(document, uploadedFile, shareTransfer.TransfareeShID);

                            if (documentId > 0)
                            {
                                // Save comma-separated SubIDs and PayIDs to the respective fields
                                shareTransfer.SubID = selectedSubscriptionIds; // Store subscription IDs
                                shareTransfer.PayID = selectedPaymentIds; // Store payment IDs
                                shareTransfer.TransferType = TransferType; // store transfer type                                                                
                                shareTransfer.TransferAuthorizationStatus = "Pending";
                                shareTransfer.CreatedBy = UserID;
                                shareTransfer.CreationDate = DateTime.Now;
                                shareTransfer.Branch = BranchId;
                                shareTransfer.TransferDoc = documentId;
                                db.ShareTransfers.Add(shareTransfer);
                                db.SaveChanges();


                                // Call RecordLog method with null-safe value for CreatedBy
                                AuditLogsController auditLogsController = new AuditLogsController();
                                auditLogsController.RecordLog("Registration", shareTransfer.TransferID, "Transfer", shareTransfer.CreatedBy ?? 0, Session["BranchName"].ToString());

                                // Commit the transaction
                                transaction.Commit();

                                TempData["SuccessMessage"] = "Transfer submitted successfully!";
                                return RedirectToAction("Create");

                            }
                            else
                            {
                                // Handle the error case where the document was not created successfully
                                ModelState.AddModelError("", "Document could not be created. Please try again.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "Error saving the share transfer: " + ex.Message);
                    }
                }
            }
            // Repopulate ViewBags in case of failure
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode", shareTransfer.PayID);
            ViewBag.TransferrorShID = new SelectList(db.Shareholders, "ShID", "FullNameEng", shareTransfer.TransferrorShID);
            ViewBag.TransfareeShID = new SelectList(db.Shareholders, "ShID", "FullNameEng", shareTransfer.TransfareeShID);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus", shareTransfer.SubID);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", shareTransfer.CreatedBy);
            ViewBag.TransferAuthorizer = new SelectList(db.Users, "UID", "FullName", shareTransfer.TransferAuthorizer);
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchName", shareTransfer.Branch);

            return View(shareTransfer);
        }



        // Approval Status
        [HttpPost]
        public ActionResult Approve(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int AuthorizerId = Convert.ToInt32(Session["ID"]);
                    var shareTransfer = db.ShareTransfers.Find(id);
                    if (shareTransfer == null)
                    {
                        return HttpNotFound();
                    }

                    // Change status to Approved
                    shareTransfer.TransferAuthorizer = AuthorizerId;
                    shareTransfer.TransferAuthorizationStatus = "Approved";
                    shareTransfer.TransferAuthorizationDate = DateTime.Now;
                    db.Entry(shareTransfer).State = EntityState.Modified;
                    db.SaveChanges();


                    // Perform different logic based on transferCategory and chooseType
                    if (shareTransfer.TransferCategory == "Subscription" && shareTransfer.TransferType == "PaidUp")
                    {
                        // Logic for paidup subscription transfer
                        // Update the transferring shareholder's subscription
                        var subscriptionIdsArray = shareTransfer.SubID.Split(',');
                        foreach (var subscriptionId in subscriptionIdsArray)
                        {
                            var transferrorSubscription = db.Subscribtions.FirstOrDefault(s => s.SubID.ToString() == subscriptionId && s.ShID == shareTransfer.TransferrorShID);
                            if (transferrorSubscription != null)
                            {
                                transferrorSubscription.SubNumShares -= (int)(transferrorSubscription.PaidSubscription / 100);
                                transferrorSubscription.SubAmount -= transferrorSubscription.SubAmount;
                                transferrorSubscription.PaidSubscription -= transferrorSubscription.PaidSubscription;
                                if (transferrorSubscription.PaidSubscription == 0)
                                {
                                    transferrorSubscription.SubStatus = "UnPaid";
                                }
                                if (transferrorSubscription.SubNumShares == 0)
                                {
                                    transferrorSubscription.SubAuthorizationStatus = "Transfered";
                                }
                                db.Entry(transferrorSubscription).State = EntityState.Modified;
                            }
                        }

                        // Create new Subscription for transferee
                        var newSubscription = new Subscribtion
                        {
                            ShID = shareTransfer.TransfareeShID,
                            SubNumShares = shareTransfer.NumSharesTransferred,
                            SubAmount = shareTransfer.PaidAmountForTransfer,
                            PaidSubscription = shareTransfer.PaidAmountForTransfer,
                            UnpaidSubscription = 0.00M, // Explicitly set UnpaidSubscription to 0.00
                            SubTransferFrom = shareTransfer.TransferrorShID,
                            PaymentDueDate = shareTransfer.TransferDate,
                            AuthorizedDate = DateTime.Now,
                            SubAuthorizer = AuthorizerId,
                            SubStatus = "Fully Paid",
                            CreatedBy = shareTransfer.CreatedBy,
                            SubDate = DateTime.Now,
                            SubAuthorizationStatus = "Approved",
                            Branch = shareTransfer.Branch,
                            Remark = shareTransfer.Remark
                        };
                        db.Subscribtions.Add(newSubscription);
                        db.SaveChanges();

                        // Update payments
                        var paymentIdsArray = shareTransfer.PayID.Split(',');
                        foreach (var paymentId in paymentIdsArray)
                        {
                            var payment = db.Payments.FirstOrDefault(p => p.PayID.ToString() == paymentId && p.ShID == shareTransfer.TransferrorShID);
                            if (payment != null)
                            {
                                var newPayments = new Payment
                                {
                                    ShID = shareTransfer.TransfareeShID,
                                    SubID = newSubscription.SubID,
                                    PaymentMode = payment.PaymentMode,
                                    PaidAmount = payment.PaidAmount,
                                    ReferenceNum = payment.ReferenceNum,
                                    PaymentTransferFrom = shareTransfer.TransferrorShID,
                                    PaymentDate = payment.PaymentDate,
                                    CreatedBy = shareTransfer.CreatedBy,
                                    PaymentAuthorizationStatus = "Approved",
                                    PaymentAuthorizer = AuthorizerId,
                                    AuthorizationDate = DateTime.Now,
                                    TransferedPayID = payment.PayID,
                                    TransferID = shareTransfer.TransferID,
                                    TransferAmount = payment.PaidAmount,
                                    CreationDate = shareTransfer.CreationDate,
                                    Branch = shareTransfer.Branch,
                                    Remark = shareTransfer.Remark

                                };
                                db.Payments.Add(newPayments);
                                payment.PaidAmount -= payment.PaidAmount;
                                db.Entry(payment).State = EntityState.Modified;
                            }
                        }
                        db.SaveChanges();

                        // Update the Document Status
                        var document = db.Documents.FirstOrDefault(d => d.DocID == shareTransfer.TransferDoc);
                        if (document != null)
                        {
                            document.DocAuthorizationStatus = "Approved";
                            document.DocAuthorizationDate = DateTime.Now;
                            document.DocAuthorizer = AuthorizerId;
                            db.Entry(document).State = EntityState.Modified;
                        }
                        db.SaveChanges();
                        TempData["SuccessMessage"] = "PaidUp Transfer Committed Successfully!";
                    }
                    else if (shareTransfer.TransferCategory == "Subscription" && shareTransfer.TransferType == "Right")
                    {
                        // Logic for right subscription transfer
                        // Update the transferring shareholder's subscription
                        var transferrorSubscription = db.Subscribtions.FirstOrDefault(s => s.SubID.ToString() == shareTransfer.SubID && s.ShID == shareTransfer.TransferrorShID);
                        if (transferrorSubscription != null)
                        {
                            transferrorSubscription.SubNumShares -= shareTransfer.NumSharesTransferred;
                            transferrorSubscription.SubAmount -= shareTransfer.PaidAmountForTransfer;
                            transferrorSubscription.UnpaidSubscription -= shareTransfer.PaidAmountForTransfer;
                            if (transferrorSubscription.PaidSubscription == 0)
                            {
                                transferrorSubscription.SubStatus = "UnPaid";
                            }
                            if (transferrorSubscription.SubNumShares == 0)
                            {
                                transferrorSubscription.SubAuthorizationStatus = "Transfered";
                            }
                            db.Entry(transferrorSubscription).State = EntityState.Modified;
                        }

                        // Add a new subscription for the transferee shareholder
                        var newSubscription = new Subscribtion
                        {
                            ShID = shareTransfer.TransfareeShID,
                            SubNumShares = shareTransfer.NumSharesTransferred,
                            SubAmount = shareTransfer.PaidAmountForTransfer,
                            PaidSubscription = shareTransfer.PaidAmountForTransfer,
                            UnpaidSubscription = 0.00M, // Explicitly set UnpaidSubscription to 0.00
                            SubTransferFrom = shareTransfer.TransferrorShID,
                            PaymentDueDate = shareTransfer.TransferDate,
                            AuthorizedDate = DateTime.Now,
                            SubAuthorizer = AuthorizerId,
                            SubStatus = "Fully Paid",
                            CreatedBy = shareTransfer.CreatedBy,
                            SubDate = DateTime.Now,
                            SubAuthorizationStatus = "Approved",
                            Branch = shareTransfer.Branch,
                            Remark = shareTransfer.Remark
                        };
                        db.Subscribtions.Add(newSubscription);
                        db.SaveChanges();

                        // Add a new payment for the transferee shareholder
                        var newPayments = new Payment
                        {
                            ShID = shareTransfer.TransfareeShID,
                            SubID = newSubscription.SubID,
                            PaymentMode = "AccountTransfer",
                            PaidAmount = shareTransfer.PaidAmountForTransfer,
                            ReferenceNum = "RightTransfer",
                            PaymentTransferFrom = shareTransfer.TransferrorShID,
                            PaymentDate = shareTransfer.TransferDate,
                            CreatedBy = shareTransfer.CreatedBy,
                            PaymentAuthorizationStatus = "Approved",
                            PaymentAuthorizer = AuthorizerId,
                            AuthorizationDate = DateTime.Now,
                            TransferID = shareTransfer.TransferID,
                            TransferAmount = shareTransfer.PaidAmountForTransfer,
                            CreationDate = shareTransfer.CreationDate,
                            Branch = shareTransfer.Branch,
                            Remark = shareTransfer.Remark


                        };
                        db.Payments.Add(newPayments);
                        db.SaveChanges();

                        // Update the Document Status
                        var document = db.Documents.FirstOrDefault(d => d.DocID == shareTransfer.TransferDoc);
                        if (document != null)
                        {
                            document.DocAuthorizationStatus = "Approved";
                            document.DocAuthorizationDate = DateTime.Now;
                            document.DocAuthorizer = AuthorizerId;
                            db.Entry(document).State = EntityState.Modified;
                        }
                        db.SaveChanges();
                        TempData["SuccessMessage"] = "Right Transfer Committed Successfully!";
                        //return RedirectToAction("Create");
                    }
                    else if (shareTransfer.TransferCategory == "Payment" && shareTransfer.TransferType == "Full")
                    {

                        // Logic for full payment transfer
                        // Update the transferring shareholder's subscription
                        var transferrorSubscription = db.Subscribtions.FirstOrDefault(s => s.SubID.ToString() == shareTransfer.SubID && s.ShID == shareTransfer.TransferrorShID);
                        if (transferrorSubscription != null)
                        {
                            transferrorSubscription.SubNumShares -= shareTransfer.NumSharesTransferred;
                            transferrorSubscription.SubAmount -= shareTransfer.PaidAmountForTransfer;
                            transferrorSubscription.PaidSubscription -= shareTransfer.PaidAmountForTransfer;
                            if (transferrorSubscription.PaidSubscription == 0)
                            {
                                transferrorSubscription.SubStatus = "UnPaid";
                            }
                            if (transferrorSubscription.SubNumShares == 0)
                            {
                                transferrorSubscription.SubAuthorizationStatus = "Transfered";
                            }
                            db.Entry(transferrorSubscription).State = EntityState.Modified;
                        }

                        // Add a new subscription for the transferee shareholder
                        var newSubscription = new Subscribtion
                        {
                            ShID = shareTransfer.TransfareeShID,
                            SubNumShares = shareTransfer.NumSharesTransferred,
                            SubAmount = shareTransfer.PaidAmountForTransfer,
                            PaidSubscription = shareTransfer.PaidAmountForTransfer,
                            UnpaidSubscription = 0.00M, // Explicitly set UnpaidSubscription to 0.00
                            SubTransferFrom = shareTransfer.TransferrorShID,
                            PaymentDueDate = shareTransfer.TransferDate,
                            AuthorizedDate = DateTime.Now,
                            SubAuthorizer = AuthorizerId,
                            SubStatus = "Fully Paid",
                            CreatedBy = shareTransfer.CreatedBy,
                            SubDate = DateTime.Now,
                            SubAuthorizationStatus = "Approved",
                            Branch = shareTransfer.Branch,
                            Remark = shareTransfer.Remark
                        };
                        db.Subscribtions.Add(newSubscription);
                        db.SaveChanges();

                        // Split payment IDs and update payment records accordingly
                        var paymentIdsArray = shareTransfer.PayID.Split(',');
                        foreach (var paymentId in paymentIdsArray)
                        {
                            // Update the payment for each selected payment ID
                            var payment = db.Payments.FirstOrDefault(p => p.PayID.ToString() == paymentId && p.ShID == shareTransfer.TransferrorShID);
                            if (payment != null)
                            {
                                // Add a new payment for the transferee shareholder
                                var newPayments = new Payment
                                {
                                    ShID = shareTransfer.TransfareeShID,
                                    SubID = newSubscription.SubID,
                                    PaymentMode = payment.PaymentMode,
                                    PaidAmount = payment.PaidAmount,
                                    ReferenceNum = payment.ReferenceNum,
                                    PaymentTransferFrom = shareTransfer.TransferrorShID,
                                    PaymentDate = payment.PaymentDate,
                                    CreatedBy = shareTransfer.CreatedBy,
                                    PaymentAuthorizationStatus = "Approved",
                                    PaymentAuthorizer = AuthorizerId,
                                    AuthorizationDate = DateTime.Now,
                                    TransferedPayID = payment.PayID,
                                    TransferID = shareTransfer.TransferID,
                                    TransferAmount = payment.PaidAmount,
                                    CreationDate = shareTransfer.CreationDate,
                                    Branch = shareTransfer.Branch,
                                    Remark = shareTransfer.Remark

                                };
                                db.Payments.Add(newPayments);
                                payment.PaidAmount -= payment.PaidAmount; // Adjust by the actual paid amount for the payment
                                db.Entry(payment).State = EntityState.Modified;
                            }
                        }

                        db.SaveChanges();


                        // Update the Document Status
                        var document = db.Documents.FirstOrDefault(d => d.DocID == shareTransfer.TransferDoc);
                        if (document != null)
                        {
                            document.DocAuthorizationStatus = "Approved";
                            document.DocAuthorizationDate = DateTime.Now;
                            document.DocAuthorizer = AuthorizerId;
                            db.Entry(document).State = EntityState.Modified;
                        }
                        db.SaveChanges();
                        TempData["SuccessMessage"] = "Full Payment Transfer Committed Successfully!";
                        //return RedirectToAction("Create");
                    }
                    else if (shareTransfer.TransferCategory == "Payment" && shareTransfer.TransferType == "Partial")
                    {
                        // Logic for partial payment transfer
                        // Update the transferring shareholder's subscription
                        var transferrorSubscription = db.Subscribtions.FirstOrDefault(s => s.SubID.ToString() == shareTransfer.SubID && s.ShID == shareTransfer.TransferrorShID);
                        if (transferrorSubscription != null)
                        {
                            transferrorSubscription.SubNumShares -= shareTransfer.NumSharesTransferred;
                            transferrorSubscription.SubAmount -= shareTransfer.PaidAmountForTransfer;
                            transferrorSubscription.PaidSubscription -= shareTransfer.PaidAmountForTransfer;
                            if (transferrorSubscription.PaidSubscription == 0)
                            {
                                transferrorSubscription.SubStatus = "UnPaid";
                            }
                            if (transferrorSubscription.SubNumShares == 0)
                            {
                                transferrorSubscription.SubAuthorizationStatus = "Transfered";
                            }
                            db.Entry(transferrorSubscription).State = EntityState.Modified;
                        }

                        // Add a new subscription for the transferee shareholder
                        var newSubscription = new Subscribtion
                        {
                            ShID = shareTransfer.TransfareeShID,
                            SubNumShares = shareTransfer.NumSharesTransferred,
                            SubAmount = shareTransfer.PaidAmountForTransfer,
                            PaidSubscription = shareTransfer.PaidAmountForTransfer,
                            UnpaidSubscription = 0.00M, // Explicitly set UnpaidSubscription to 0.00
                            SubTransferFrom = shareTransfer.TransferrorShID,
                            PaymentDueDate = shareTransfer.TransferDate,
                            AuthorizedDate = DateTime.Now,
                            SubAuthorizer = AuthorizerId,
                            SubStatus = "Fully Paid",
                            CreatedBy = shareTransfer.CreatedBy,
                            SubDate = DateTime.Now,
                            SubAuthorizationStatus = "Approved",
                            Branch = shareTransfer.Branch,
                            Remark = shareTransfer.Remark

                        };
                        db.Subscribtions.Add(newSubscription);
                        db.SaveChanges();

                        // Update payment records if the transfer type is related to payments
                        var payment = db.Payments.FirstOrDefault(p => p.PayID.ToString() == shareTransfer.PayID && p.ShID == shareTransfer.TransferrorShID);
                        if (payment != null)
                        {
                            // Reduce the PaidAmount for the transferror
                            payment.PaidAmount -= shareTransfer.PaidAmountForTransfer;
                            db.Entry(payment).State = EntityState.Modified;
                        }

                        // Add a new payment for the transferee shareholder
                        var newPayments = new Payment
                        {
                            ShID = shareTransfer.TransfareeShID,
                            SubID = newSubscription.SubID,
                            PaymentMode = payment.PaymentMode,
                            PaidAmount = shareTransfer.PaidAmountForTransfer,
                            ReferenceNum = payment.ReferenceNum,
                            PaymentTransferFrom = shareTransfer.TransferrorShID,
                            PaymentDate = shareTransfer.TransferDate,
                            CreatedBy = shareTransfer.CreatedBy,
                            PaymentAuthorizationStatus = "Approved",
                            PaymentAuthorizer = AuthorizerId,
                            AuthorizationDate = DateTime.Now,
                            TransferedPayID = payment.PayID,
                            TransferID = shareTransfer.TransferID,
                            TransferAmount = shareTransfer.PaidAmountForTransfer,
                            CreationDate = shareTransfer.CreationDate,
                            Branch = shareTransfer.Branch,
                            Remark = shareTransfer.Remark

                        };
                        db.Payments.Add(newPayments);
                        db.SaveChanges();

                        // Update the Document Status
                        var document = db.Documents.FirstOrDefault(d => d.DocID == shareTransfer.TransferDoc);
                        if (document != null)
                        {
                            document.DocAuthorizationStatus = "Approved";
                            document.DocAuthorizationDate = DateTime.Now;
                            document.DocAuthorizer = AuthorizerId;
                            db.Entry(document).State = EntityState.Modified;
                        }
                        db.SaveChanges();
                        TempData["SuccessMessage"] = "Partial Payment Transfer Committed Successfully!";
                        //return RedirectToAction("Create");
                    }
                    else if (shareTransfer.TransferCategory == "Full Transfer" && shareTransfer.TransferType == "Full")
                    {
                        // Logic for Full Share transfer
                        // Update the transferring shareholder's subscription
                        var subscriptionIdsArray = shareTransfer.SubID.Split(',');
                        foreach (var subscriptionId in subscriptionIdsArray)
                        {
                            var transferrorSubscription = db.Subscribtions.FirstOrDefault(s => s.SubID.ToString() == subscriptionId && s.ShID == shareTransfer.TransferrorShID);
                            if (transferrorSubscription != null)
                            {
                                transferrorSubscription.SubNumShares -= (int)(transferrorSubscription.PaidSubscription / 100);
                                transferrorSubscription.SubAmount -= transferrorSubscription.SubAmount;
                                transferrorSubscription.PaidSubscription -= transferrorSubscription.PaidSubscription;
                                if (transferrorSubscription.PaidSubscription == 0)
                                {
                                    transferrorSubscription.SubStatus = "UnPaid";
                                }
                                if (transferrorSubscription.SubNumShares == 0)
                                {
                                    transferrorSubscription.SubAuthorizationStatus = "Transfered";
                                }
                                db.Entry(transferrorSubscription).State = EntityState.Modified;
                            }
                        }

                        // Create new Subscription for transferee
                        var newSubscription = new Subscribtion
                        {
                            ShID = shareTransfer.TransfareeShID,
                            SubNumShares = shareTransfer.NumSharesTransferred,
                            SubAmount = shareTransfer.PaidAmountForTransfer,
                            PaidSubscription = shareTransfer.PaidAmountForTransfer,
                            UnpaidSubscription = 0.00M, // Explicitly set UnpaidSubscription to 0.00
                            SubTransferFrom = shareTransfer.TransferrorShID,
                            PaymentDueDate = shareTransfer.TransferDate,
                            AuthorizedDate = DateTime.Now,
                            SubAuthorizer = AuthorizerId,
                            SubStatus = "Fully Paid",
                            CreatedBy = shareTransfer.CreatedBy,
                            SubDate = DateTime.Now,
                            SubAuthorizationStatus = "Approved",
                            Branch = shareTransfer.Branch,
                            Remark = shareTransfer.Remark

                        };
                        db.Subscribtions.Add(newSubscription);
                        db.SaveChanges();

                        // Update payments
                        var paymentIdsArray = shareTransfer.PayID.Split(',');
                        foreach (var paymentId in paymentIdsArray)
                        {
                            var payment = db.Payments.FirstOrDefault(p => p.PayID.ToString() == paymentId && p.ShID == shareTransfer.TransferrorShID);
                            if (payment != null)
                            {
                                var newPayments = new Payment
                                {
                                    ShID = shareTransfer.TransfareeShID,
                                    SubID = newSubscription.SubID,
                                    PaymentMode = payment.PaymentMode,
                                    PaidAmount = payment.PaidAmount,
                                    ReferenceNum = payment.ReferenceNum,
                                    PaymentTransferFrom = shareTransfer.TransferrorShID,
                                    PaymentDate = payment.PaymentDate,
                                    CreatedBy = shareTransfer.CreatedBy,
                                    PaymentAuthorizationStatus = "Approved",
                                    PaymentAuthorizer = AuthorizerId,
                                    AuthorizationDate = DateTime.Now,
                                    TransferedPayID = payment.PayID,
                                    TransferID = shareTransfer.TransferID,
                                    TransferAmount = payment.PaidAmount,
                                    CreationDate = shareTransfer.CreationDate,
                                    Branch = shareTransfer.Branch,
                                    Remark = shareTransfer.Remark
                                };
                                db.Payments.Add(newPayments);
                                payment.PaidAmount -= payment.PaidAmount;
                                db.Entry(payment).State = EntityState.Modified;
                            }
                        }
                        db.SaveChanges();

                        // Update the Document Status
                        var document = db.Documents.FirstOrDefault(d => d.DocID == shareTransfer.TransferDoc);
                        if (document != null)
                        {
                            document.DocAuthorizationStatus = "Approved";
                            document.DocAuthorizationDate = DateTime.Now;
                            document.DocAuthorizer = AuthorizerId;
                            db.Entry(document).State = EntityState.Modified;
                        }
                        db.SaveChanges();
                        TempData["SuccessMessage"] = "Full Transfer Committed Successfully!";
                    }
                    // Commit the transaction after all updates
                    db.SaveChanges();
                    transaction.Commit();
                    return RedirectToAction("Authorize");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Error approving the share transfer: " + ex.Message);
                }
            }
            return View();
        }

        // Reject Status
        [HttpPost]
        public ActionResult Reject(int id)
        {

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    int AuthorizerId = Convert.ToInt32(Session["ID"]);
                    var shareTransfer = db.ShareTransfers.Find(id);
                    if (shareTransfer == null)
                    {
                        return HttpNotFound();
                    }

                    // Change status to Approved
                    shareTransfer.TransferAuthorizationStatus = "Rejected";
                    shareTransfer.TransferAuthorizationDate = DateTime.Now;
                    shareTransfer.TransferAuthorizer = AuthorizerId; ;
                    db.Entry(shareTransfer).State = EntityState.Modified;
                    db.SaveChanges();

                    // Update the Document Status
                    var document = db.Documents.FirstOrDefault(d => d.DocID == shareTransfer.TransferDoc);
                    if (document != null)
                    {
                        document.DocAuthorizationStatus = "Rejected";
                        document.DocAuthorizationDate = DateTime.Now;
                        document.DocAuthorizer = AuthorizerId;
                        db.Entry(document).State = EntityState.Modified;
                    }
                    db.SaveChanges();

                    transaction.Commit();
                    TempData["ErrorMessage"] = "Transfer Rejected!";
                    return RedirectToAction("Authorize");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Error rejecting the share transfer: " + ex.Message);
                }
            }

            return View();
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            ShareTransfer shareTransfer = db.ShareTransfers.Find(id);
            if (shareTransfer == null)
            {
                return HttpNotFound();
            }

            // Fetch the list of all shareholders for the dropdowns
            var shareholders = db.Shareholders
                                 .OrderBy(s => s.FullNameEng)
                                 .ToList()
                                 .Select(s => new SelectListItem
                                 {
                                     Value = s.ShID.ToString(),
                                     Text = $"{s.FullNameEng} ({s.ShareID})"
                                 }).ToList();

            shareholders.Insert(0, new SelectListItem { Value = "", Text = "Select Shareholder" });
            ViewBag.ShareholdersFrom = new SelectList(shareholders, "Value", "Text");
            ViewBag.ShareholdersTo = new SelectList(shareholders, "Value", "Text");

            if (shareTransfer.TransferDoc.HasValue)
            {
                var document = db.Documents.Find(shareTransfer.TransferDoc);
                if (document != null)
                {
                    ViewBag.ExistingDocumentName = document.DocName;
                    ViewBag.DocumentId = document.DocID;
                }
            }
            else
            {
                ViewBag.ExistingDocumentName = null;
                ViewBag.DocumentId = null;
            }


            // Extract checked SubID and PayID values from ShareTransfer
            ViewBag.CheckedSubscriptions = shareTransfer.SubID.Split(',').ToList();
            ViewBag.CheckedPayments = shareTransfer.PayID.Split(',').ToList();

            return View(shareTransfer);
        }


        // POST: ShareTransfers/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id,
            ShareTransfer shareTransfer,
            string selectedSubscriptionIds,
            string selectedPaymentIds,
            string TransferType,
            HttpPostedFileBase uploadedFile)
        {
            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        int UserID = Convert.ToInt32(Session["ID"]);
                        int BranchId = Convert.ToInt32(Session["Branch"]);
                        var existingShareTransfer = db.ShareTransfers.Find(id);


                        // Update fields related to the transfer             
                        existingShareTransfer.CreatedBy = UserID; // Track modification
                        existingShareTransfer.TransfareeShID = shareTransfer.TransfareeShID;
                        existingShareTransfer.TransferCategory = shareTransfer.TransferCategory;
                        existingShareTransfer.SubID = selectedSubscriptionIds; // Update subscription IDs
                        existingShareTransfer.PayID = selectedPaymentIds; // Update payment IDs
                        existingShareTransfer.TransferType = TransferType; // Update transfer type
                        existingShareTransfer.NumSharesTransferred = shareTransfer.NumSharesTransferred;
                        existingShareTransfer.AmountPerShare = shareTransfer.AmountPerShare;
                        existingShareTransfer.PaidAmountForTransfer = shareTransfer.PaidAmountForTransfer;
                        existingShareTransfer.TransferReason = shareTransfer.TransferReason;
                        existingShareTransfer.DividenedFor = shareTransfer.DividenedFor;
                        existingShareTransfer.TransferDate = shareTransfer.TransferDate;
                        existingShareTransfer.Remark = shareTransfer.Remark;

                        // Handle document creation (if file uploaded)                       
                        if (uploadedFile != null && uploadedFile.ContentLength > 0)
                        {
                            Document document = new Document
                            {
                                DocOwner = "Shareholder",
                                DocType = "Transfer Document",
                                ShID = existingShareTransfer.TransferrorShID,
                                CreatedBy = UserID,
                                DocAuthorizationStatus = "Pending",
                                CreatedDate = DateTime.Now
                            };

                            DocumentsController documentsController = new DocumentsController();
                            documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

                            int documentId = documentsController.Create(document, uploadedFile, shareTransfer.TransfareeShID);
                            if (documentId > 0)
                            {
                                existingShareTransfer.TransferDoc = documentId;
                            }
                            else
                            {
                                ModelState.AddModelError("", "Document could not be created. Please try again.");
                            }
                        }

                        db.Entry(existingShareTransfer).State = EntityState.Modified;
                        db.SaveChanges();

                        // Record the audit log
                        AuditLogsController auditLogsController = new AuditLogsController();
                        auditLogsController.RecordLog("Modification", shareTransfer.TransferID, "Transfer", UserID, Session["BranchName"].ToString());

                        // Commit the transaction
                        transaction.Commit();

                        TempData["SuccessMessage"] = "Transfer updated successfully!";
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", "Error updating the share transfer: " + ex.Message);
                    }
                }
            }

            // Repopulate ViewBags in case of failure
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode", shareTransfer.PayID);
            ViewBag.TransferrorShID = new SelectList(db.Shareholders, "ShID", "FullNameEng", shareTransfer.TransferrorShID);
            ViewBag.TransfareeShID = new SelectList(db.Shareholders, "ShID", "FullNameEng", shareTransfer.TransfareeShID);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus", shareTransfer.SubID);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", shareTransfer.CreatedBy);
            ViewBag.TransferAuthorizer = new SelectList(db.Users, "UID", "FullName", shareTransfer.TransferAuthorizer);
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchName", shareTransfer.Branch);

            return View(shareTransfer);
        }


        // GET: ShareTransfers/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ShareTransfer shareTransfer = db.ShareTransfers.Find(id);
            if (shareTransfer == null)
            {
                return HttpNotFound();
            }
            return View(shareTransfer);
        }

        // POST: ShareTransfers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ShareTransfer shareTransfer = db.ShareTransfers.Find(id);
            db.ShareTransfers.Remove(shareTransfer);
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
