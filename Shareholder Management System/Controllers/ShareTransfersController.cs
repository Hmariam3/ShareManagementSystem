
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Shareholder_Management_System.Models;

namespace Shareholder_Management_System.Controllers
{
    public class ShareTransfersController : BaseController
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
            // No need to load shareholders here since dropdowns will use AJAX
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");
            ViewBag.TransferAuthorizer = new SelectList(db.Users, "UID", "FullName");
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchName");

            return View();
        }

        // AJAX Action to search shareholders
        //[HttpGet]
        //public ActionResult SearchShareholders(string term, int? transferrorId)
        //{
        //    try
        //    {
        //        var shareholders = db.Shareholders
        //            .Where(s => s.Status.Equals("Active") && s.AuthorizationStatus.Equals("Approved"))
        //            .Where(s => string.IsNullOrEmpty(term) ||
        //                        s.FullNameEng.ToLower().Contains(term.ToLower()) || // Changed to Contains for broader search
        //                        s.ShareID.ToLower().Contains(term.ToLower()))
        //            .Where(s => !transferrorId.HasValue || s.ShID != transferrorId.Value) // Exclude transferrorId
        //            .OrderBy(s => s.FullNameEng) // Sort alphabetically
        //            .Select(s => new
        //            {
        //                id = s.ShID,
        //                text = s.FullNameEng + " (" + s.ShareID + ")",
        //                shareID = s.ShareID
        //            })
        //            .Take(500) // Limit results for performance
        //            .ToList();

        //        return Json(new { results = shareholders }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
        //    }
        //}


        [HttpGet]
        public JsonResult SearchShareholders(string term, int? transferrorId, int page = 1)
        {
            try
            {
                const int pageSize = 10;
                var query = db.Shareholders
                    .Where(s => s.Status == "Active" && s.AuthorizationStatus == "Approved");

                if (!string.IsNullOrEmpty(term))
                {
                    var searchTerm = term.ToLower().Trim();
                    query = query.Where(s =>
                        (s.FullNameEng != null && s.FullNameEng.ToLower().Contains(searchTerm)) ||
                        (s.ShareID != null && s.ShareID.ToLower().Contains(searchTerm)));
                }

                if (transferrorId.HasValue)
                {
                    query = query.Where(s => s.ShID != transferrorId.Value);
                }

                var total = query.Count();
                var shareholders = query
                    .OrderBy(s => s.FullNameEng)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new
                    {
                        id = s.ShID,
                        text = s.FullNameEng + " ( " + s.ShareID + " ) "
                    })
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"SearchShareholders: term={term}, transferrorId={transferrorId}, page={page}, results={shareholders.Count}, total={total}");

                return Json(new
                {
                    items = shareholders,
                    hasMore = total > page * pageSize
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SearchShareholders Error: {ex.Message}, StackTrace: {ex.StackTrace}, Term: {term}, TransferrorId: {transferrorId}, Page: {page}");
                return Json(new { error = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        //// GET: ShareTransfers/Create
        //public ActionResult Create()
        //{
        //    // Fetch shareholders and order alphabetically, but convert to list first
        //    var shareholders = db.Shareholders
        //                         .OrderBy(s => s.FullNameEng).Where(s => s.Status.Equals("Active") && s.AuthorizationStatus.Equals("Approved")) // Sort alphabetically
        //                         .ToList() // Convert to list to work with in-memory LINQ
        //                         .Select(s => new SelectListItem
        //                         {
        //                             Value = s.ShID.ToString(),
        //                             Text = $"{s.FullNameEng} ({s.ShareID})" // Concatenate name and ID
        //                         }).ToList();

        //    // Insert a default "Select Shareholder" option
        //    shareholders.Insert(0, new SelectListItem { Value = "", Text = "Select Shareholder" });

        //    ViewBag.ShareholdersFrom = new SelectList(shareholders, "Value", "Text");
        //    ViewBag.ShareholdersTo = new SelectList(shareholders, "Value", "Text");

        //    // Other ViewBag items
        //    ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");
        //    ViewBag.TransferAuthorizer = new SelectList(db.Users, "UID", "FullName");
        //    ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchName");

        //    return View();
        //}

        // AJAX Action to update only the "Transfer To" dropdown based on "Transfer From" selection
        //public JsonResult UpdateTransferToDropdown(int? transferrorId)
        //{
        //    var shareholders = db.Shareholders
        //                         .OrderBy(s => s.FullNameEng).Where(s => s.Status.Equals("Active") && s.AuthorizationStatus.Equals("Approved")) // Sort alphabetically
        //                         .ToList() // Convert to list to work with in-memory LINQ
        //                         .Select(s => new SelectListItem
        //                         {
        //                             Value = s.ShID.ToString(),
        //                             Text = $"{s.FullNameEng} ({s.ShareID})" // Concatenate name and ID
        //                         }).ToList();

        //    // Filter out the selected "Transfer From" shareholder from "Transfer To" options
        //    var toList = shareholders.Where(s => s.Value != transferrorId?.ToString()).ToList();

        //    return Json(toList, JsonRequestBehavior.AllowGet);
        //}



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

        // GET: Certificate/Get Last Certifcate Number
        private int GetLastCertificateNumber()
        {
            try
            {
                var certNums = db.Certificates
                    .Where(c => c.CertNum != null && c.CertAuthorizationStatus == "Approved")
                    .Select(c => c.CertNum)
                    .ToList();

                if (!certNums.Any())
                {
                    System.Diagnostics.Debug.WriteLine("No certificates found, returning 0");
                    return 0;
                }

                int maxCertNum = 0;
                foreach (var certNum in certNums)
                {
                    if (int.TryParse(certNum, out int parsedNum))
                    {
                        if (parsedNum > maxCertNum)
                        {
                            maxCertNum = parsedNum;
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Invalid CertNum found: {certNum}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Last certificate number: {maxCertNum}");
                return maxCertNum;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetLastCertificateNumber: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                return 0;
            }
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

                        Trace.Write(shareTransfer);

                        var transferCategory = shareTransfer.TransferCategory;
                        // Ensure selectedPaymentIds or selectedSubscriptionIds is not null or empty
                        if ((!string.IsNullOrEmpty(selectedPaymentIds) || !string.IsNullOrEmpty(selectedSubscriptionIds)) && !string.IsNullOrEmpty(transferCategory) && !string.IsNullOrEmpty(TransferType))
                        {
                            // CASE 1: Check PayID for specific Transfer Categories and Types
                            if ((transferCategory == "Subscription" && TransferType == "PaidUp") ||
                                (transferCategory == "Payment" && TransferType == "Full") ||
                                (transferCategory == "Payment" && TransferType == "Partial") ||
                                (transferCategory == "FullTransfer" && TransferType == "Full"))
                            {
                                if (!string.IsNullOrEmpty(selectedPaymentIds))
                                {
                                    // Convert selectedPaymentIds into a List<int>
                                    var newPaymentIds = selectedPaymentIds
                                        .Split(',')
                                        .Select(pid => pid.Trim())
                                        .Where(pid => int.TryParse(pid, out _))
                                        .Select(int.Parse)
                                        .ToList();

                                    // Fetch PayID strings from ShareTransfers where Authorization Status is "Pending"
                                    var pendingPayIdStrings = db.ShareTransfers
                                        .Where(t => t.TransferAuthorizationStatus == "Pending" && !string.IsNullOrEmpty(t.PayID))
                                        .Select(t => t.PayID)
                                        .ToList(); // Fetch data first to process in-memory

                                    // Fetch PayID strings from Certificate 
                                    var existingcertificateIds = db.Certificates
                                        .Select(c => c.PaymentIDs)
                                        .ToList();

                                    // Process PayID values in-memory
                                    var existingPendingPaymentIds = pendingPayIdStrings
                                        .SelectMany(payIds => payIds.Split(',')
                                                                    .Select(pid => pid.Trim())
                                                                    .Where(pid => int.TryParse(pid, out _))
                                                                    .Select(int.Parse))
                                        .Distinct()
                                        .ToList();

                                    // Process certificate PayID values in-memory
                                    var existingCertificatePaymentIds = existingcertificateIds
                                        .SelectMany(payIds => payIds.Split(',')
                                                                    .Select(pid => pid.Trim())
                                                                    .Where(pid => int.TryParse(pid, out _))
                                                                    .Select(int.Parse))
                                        .Distinct()
                                        .ToList();

                                    // Check if any of the new payment IDs already exist in pending transfers and certificate table
                                    var duplicatePayments = newPaymentIds.Intersect(existingPendingPaymentIds).ToList();
                                    var existedcertificate = newPaymentIds.Intersect(existingCertificatePaymentIds).ToList();

                                    if (duplicatePayments.Any())
                                    {
                                        TempData["ErrorMessage"] = "The following Payment IDs are already in a pending transfer:<br><br>• " +
                                                                    string.Join("<br>• ", duplicatePayments) +
                                                                    "<br><br>Please take action before submitting the transfer.";
                                        return RedirectToAction("Create");
                                    }
                                    if (existedcertificate.Any())
                                    {
                                        TempData["ErrorMessage"] = "Certificate Issued for the following Payment IDs:<br><br>• " +
                                                                    string.Join("<br>• ", existedcertificate) +
                                                                    "<br><br>Please Contact the Administrators.";
                                        return RedirectToAction("Create");
                                    }
                                }
                            }

                            // CASE 2: Check SubID for Subscription - Right
                            if (transferCategory == "Subscription" && TransferType == "Right")
                            {
                                if (!string.IsNullOrEmpty(selectedSubscriptionIds))
                                {
                                    // Convert selectedSubscriptionIds into a List<int>
                                    var newSubscriptionIds = selectedSubscriptionIds
                                        .Split(',')
                                        .Select(subid => subid.Trim())
                                        .Where(subid => int.TryParse(subid, out _))
                                        .Select(int.Parse)
                                        .ToList();

                                    // Fetch SubIDs from ShareTransfers where Authorization Status is "Pending"
                                    var pendingSubIdStrings = db.ShareTransfers
                                        .Where(t => t.TransferAuthorizationStatus == "Pending" && string.IsNullOrEmpty(t.PayID) && !string.IsNullOrEmpty(t.SubID))
                                        .Select(t => t.SubID)
                                        .ToList(); // Fetch data first to process in-memory

                                    // Process SubID values in-memory
                                    var existingPendingSubIds = pendingSubIdStrings
                                        .SelectMany(subIds => subIds.Split(',')
                                                                    .Select(subid => subid.Trim())
                                                                    .Where(subid => int.TryParse(subid, out _))
                                                                    .Select(int.Parse))
                                        .Distinct()
                                        .ToList();

                                    // Check if any of the new SubIDs already exist in pending transfers
                                    var duplicateSubscriptions = newSubscriptionIds.Intersect(existingPendingSubIds).ToList();

                                    if (duplicateSubscriptions.Any())
                                    {
                                        TempData["ErrorMessage"] = "The following Subscription IDs are already in a Pending Right transfer:<br><br>• " +
                                                                   string.Join("<br>• ", duplicateSubscriptions) +
                                                                   "<br><br>Please take action before submitting the transfer.";

                                        return RedirectToAction("Create");
                                    }
                                }
                            }
                        }

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

                        // Call RecordLog method with null-safe value for CreatedBy
                        AuditLogsController auditLogsController = new AuditLogsController();
                        auditLogsController.RecordLog("Approved", shareTransfer.TransferID, "PaidUp Subscription Transfer", shareTransfer.CreatedBy ?? 0, Session["BranchName"].ToString());

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
                            PaidSubscription = 0.00M, // Explicitly set paidSubscription to 0.00
                            UnpaidSubscription = shareTransfer.PaidAmountForTransfer,
                            SubTransferFrom = shareTransfer.TransferrorShID,
                            PaymentDueDate = transferrorSubscription.PaymentDueDate,
                            AuthorizedDate = DateTime.Now,
                            SubAuthorizer = AuthorizerId,
                            SubStatus = "Unpaid",
                            CreatedBy = shareTransfer.CreatedBy,
                            SubDate = DateTime.Now,
                            SubAuthorizationStatus = "Approved",
                            Branch = shareTransfer.Branch,
                            Remark = shareTransfer.Remark
                        };
                        db.Subscribtions.Add(newSubscription);
                        db.SaveChanges();

                        //// Add a new payment for the transferee shareholder
                        //var newPayments = new Payment
                        //{
                        //    ShID = shareTransfer.TransfareeShID,
                        //    SubID = newSubscription.SubID,
                        //    PaymentMode = "AccountTransfer",
                        //    PaidAmount = shareTransfer.PaidAmountForTransfer,
                        //    ReferenceNum = "RightTransfer",
                        //    PaymentTransferFrom = shareTransfer.TransferrorShID,
                        //    PaymentDate = shareTransfer.TransferDate,
                        //    CreatedBy = shareTransfer.CreatedBy,
                        //    PaymentAuthorizationStatus = "Approved",
                        //    PaymentAuthorizer = AuthorizerId,
                        //    AuthorizationDate = DateTime.Now,
                        //    TransferID = shareTransfer.TransferID,
                        //    TransferAmount = shareTransfer.PaidAmountForTransfer,
                        //    CreationDate = shareTransfer.CreationDate,
                        //    Branch = shareTransfer.Branch,
                        //    Remark = shareTransfer.Remark


                        //};
                        //db.Payments.Add(newPayments);
                        //db.SaveChanges();

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

                        // Call RecordLog method with null-safe value for CreatedBy
                        AuditLogsController auditLogsController = new AuditLogsController();
                        auditLogsController.RecordLog("Approved", shareTransfer.TransferID, "Right Subscription Transfer", shareTransfer.CreatedBy ?? 0, Session["BranchName"].ToString());


                        TempData["SuccessMessage"] = "Right Transfer Committed Successfully!";
                        //return RedirectToAction("Create");
                    }
                    else if (shareTransfer.TransferCategory == "Payment" && shareTransfer.TransferType == "Full")
                    {
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
                        var validCertificatePayments = new List<string>(); // Store payments that have certificates
                        decimal? totalPayment = 0;
                        foreach (var payments in paymentIdsArray)
                        {
                            totalPayment += db.Payments.FirstOrDefault(p => p.PayID.ToString() == payments).PaidAmount;
                        }
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

                                //// Check if this payment has a certificate
                                //var transferorCert = db.Certificates.FirstOrDefault(c => c.ShID == shareTransfer.TransferrorShID && c.PaymentIDs.Contains(paymentId));
                                //if (transferorCert != null)
                                //{
                                //    validCertificatePayments.Add(paymentId); // Store valid payment IDs
                                //}


                            }
                            db.SaveChanges();

                            // Proceed with certificate transfer **only for valid payment IDs**
                            //if (validCertificatePayments.Count > 0)
                            //{
                            //    foreach (var paymentId2 in validCertificatePayments)
                            //    {
                            //        var transferorCert = db.Certificates.FirstOrDefault(c => c.ShID == shareTransfer.TransferrorShID && c.PaymentIDs.Contains(paymentId) && c.BeginingSerial != 0 && c.EndingSerial != 0);
                            //        if (transferorCert != null)
                            //        {
                            //            int sharesToTransfer = (int)(shareTransfer.PaidAmountForTransfer / 100); // Example: 5000 / 100 = 50 shares
                            //            decimal? totalShares = totalPayment / 100;
                            //            if (totalShares < sharesToTransfer)
                            //            {
                            //                ModelState.AddModelError("", "Not enough shares to transfer");
                            //                return View(shareTransfer);
                            //            }

                            //            // Calculate new serial numbers
                            //            int? transfereeBeginning = transferorCert.BeginingSerial;    // e.g., 51
                            //            int? transfereeEnding = transferorCert.EndingSerial;               // e.g., 100

                            //            // Update Transferor's Certificate
                            //            transferorCert.BeginingSerial = 0;
                            //            transferorCert.EndingSerial = 0;    // Mark as fully transferred
                            //            transferorCert.Remark = $"Transferred shares {transfereeBeginning}-{transfereeEnding} " +
                            //                                    $"to ShID {shareTransfer.TransfareeShID}";
                            //            transferorCert.TotalPaidupAmount -= transferorCert.TotalPaidupAmount;



                            //            db.Entry(transferorCert).State = EntityState.Modified;

                            //            // Create Transferee's Certificate
                            //            var lastCertNum = GetLastCertificateNumber();
                            //            var transfereeCert = new Certificate
                            //            {
                            //                ShID = (int)shareTransfer.TransfareeShID,
                            //                PaymentIDs = paymentId,
                            //                BeginingSerial = transfereeBeginning,  // 51
                            //                EndingSerial = transfereeEnding,      // 100
                            //                CertNum = (lastCertNum + 1).ToString(),
                            //                CreatedBy = (int)shareTransfer.CreatedBy,
                            //                CreatedDate = (DateTime)shareTransfer.CreationDate,
                            //                DeliveryStatus = false,
                            //                CertAuthorizationStatus = "Approved",
                            //                CertGenerationDate = (DateTime)shareTransfer.CreationDate,
                            //                CertAuthorizer = AuthorizerId,
                            //                TotalPaidupAmount = ((transfereeEnding - transfereeBeginning) + 1) * 100,
                            //            };
                            //            db.Certificates.Add(transfereeCert);
                            //            db.SaveChanges();
                            //        }
                            //    }
                            //}
                            //else
                            //{
                            //    // Skip certificate transfer logic if no payment IDs have certificates
                            //    Console.WriteLine("No valid certificates found for the selected payments. Skipping certificate transfer.");
                            //}
                        }
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

                        // Call RecordLog method with null-safe value for CreatedBy
                        AuditLogsController auditLogsController = new AuditLogsController();
                        auditLogsController.RecordLog("Approved", shareTransfer.TransferID, "Full Payment Transfer", shareTransfer.CreatedBy ?? 0, Session["BranchName"].ToString());


                        TempData["SuccessMessage"] = "Full Payment Transfer Committed Successfully!";
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
                            PaymentDate = payment.PaymentDate,
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


                        // Certificate Transfer
                        // Check if a certificate exists for the given PayID before proceeding
                        //var transferorCert = db.Certificates.FirstOrDefault(c => c.ShID == shareTransfer.TransferrorShID && c.PaymentIDs.Contains(shareTransfer.PayID));

                        //if (transferorCert != null) // Proceed only if a certificate exists
                        //{
                        //    int sharesToTransfer = (int)(shareTransfer.PaidAmountForTransfer / 100); // Example: 5000 / 100 = 50 shares

                        //    int? totalShares = transferorCert.EndingSerial - transferorCert.BeginingSerial + 1;
                        //    if (totalShares < sharesToTransfer)
                        //    {
                        //        ModelState.AddModelError("", "Not enough shares to transfer");
                        //        return View(shareTransfer);
                        //    }

                        //    // Calculate new serial numbers
                        //    int? originalBeginning = transferorCert.BeginingSerial; // e.g., 1
                        //    int? originalEnding = transferorCert.EndingSerial;      // e.g., 100
                        //    int? newTransferorEnding = originalEnding - sharesToTransfer; // e.g., 100 - 50 = 50
                        //    int? transfereeBeginning = newTransferorEnding + 1;    // e.g., 51
                        //    int? transfereeEnding = originalEnding;                // e.g., 100

                        //    // Update Transferor's Certificate
                        //    transferorCert.EndingSerial = newTransferorEnding;    // Change from 100 to 50
                        //    transferorCert.Remark = $"Transferred shares {transfereeBeginning}-{transfereeEnding} " +
                        //                            $"to ShID {shareTransfer.TransfareeShID}";
                        //    transferorCert.TotalPaidupAmount -= (int)shareTransfer.PaidAmountForTransfer;
                        //    db.Entry(transferorCert).State = EntityState.Modified;

                        //    // Create Transferee's Certificate
                        //    var lastCertNum = GetLastCertificateNumber();
                        //    var transfereeCert = new Certificate
                        //    {
                        //        ShID = (int)shareTransfer.TransfareeShID,
                        //        PaymentIDs = shareTransfer.PayID,
                        //        BeginingSerial = transfereeBeginning,  // 51
                        //        EndingSerial = transfereeEnding,      // 100
                        //        CertNum = (lastCertNum + 1).ToString(),
                        //        CreatedBy = (int)shareTransfer.CreatedBy,
                        //        CreatedDate = (DateTime)shareTransfer.CreationDate,
                        //        DeliveryStatus = false,
                        //        CertAuthorizationStatus = "Approved",
                        //        CertGenerationDate = (DateTime)shareTransfer.CreationDate,
                        //        CertAuthorizer = AuthorizerId,
                        //        TotalPaidupAmount = ((transfereeEnding - transfereeBeginning) + 1) * 100,
                        //    };
                        //    db.Certificates.Add(transfereeCert);
                        //    db.SaveChanges();
                        //}
                        //else
                        //{
                        //    // Skip certificate transfer logic if no certificate is found for the PayID
                        //    Console.WriteLine("No certificate found for this PayID. Skipping certificate transfer.");
                        //}


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

                        // Call RecordLog method with null-safe value for CreatedBy
                        AuditLogsController auditLogsController = new AuditLogsController();
                        auditLogsController.RecordLog("Approved", shareTransfer.TransferID, "Partial Payment Transfer", shareTransfer.CreatedBy ?? 0, Session["BranchName"].ToString());


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

                        // Call RecordLog method with null-safe value for CreatedBy
                        AuditLogsController auditLogsController = new AuditLogsController();
                        auditLogsController.RecordLog("Approved", shareTransfer.TransferID, "Full Transfer", shareTransfer.CreatedBy ?? 0, Session["BranchName"].ToString());

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
                    // Call RecordLog method with null-safe value for CreatedBy
                    AuditLogsController auditLogsController = new AuditLogsController();
                    auditLogsController.RecordLog("Rejected", shareTransfer.TransferID, "Transfer", shareTransfer.CreatedBy ?? 0, Session["BranchName"].ToString());

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

            // Set selected values for the dropdowns
            if (shareTransfer.TransferrorShID.HasValue)
            {
                var transferror = db.Shareholders.Find(shareTransfer.TransferrorShID);
                if (transferror != null)
                {
                    ViewBag.SelectedTransferror = new SelectListItem
                    {
                        Value = transferror.ShID.ToString(),
                        Text = $"{transferror.FullNameEng} ({transferror.ShareID})"
                    };
                }
            }

            if (shareTransfer.TransfareeShID.HasValue)
            {
                var transferee = db.Shareholders.Find(shareTransfer.TransfareeShID);
                if (transferee != null)
                {
                    ViewBag.SelectedTransferee = new SelectListItem
                    {
                        Value = transferee.ShID.ToString(),
                        Text = $"{transferee.FullNameEng} ({transferee.ShareID})"
                    };
                }
            }

            if (shareTransfer.TransferDoc.HasValue)
            {
                var document = db.Documents.Find(shareTransfer.TransferDoc);
                if (document != null)
                {
                    ViewBag.ExistingDocumentName = document.DocName;
                    ViewBag.DocumentId = document.DocID;
                }
            }

            // Extract checked SubID and PayID values from ShareTransfer
            ViewBag.CheckedSubscriptions = shareTransfer.SubID?.Split(',').ToList() ?? new List<string>();
            ViewBag.CheckedPayments = shareTransfer.PayID?.Split(',').ToList() ?? new List<string>();

            return View(shareTransfer);
        }

        [HttpGet]
        public JsonResult GetShareholdersforEdit(string searchTerm)
        {
            var shareholders = db.Shareholders
                .Where(s => string.IsNullOrEmpty(searchTerm) ||
                            s.FullNameEng.ToLower().Contains(searchTerm.ToLower()) ||
                            s.ShareID.ToLower().Contains(searchTerm.ToLower()))
                .OrderBy(s => s.FullNameEng)
                .Take(10) // Limit results to avoid performance issues
                .Select(s => new
                {
                    Value = s.ShID.ToString(),
                    FullNameEng = s.FullNameEng,
                    ShareID = s.ShareID
                })
                .ToList() // Materialize the query
                .Select(s => new SelectListItem
                {
                    Value = s.Value,
                    Text = string.Format("{0} ({1})", s.FullNameEng, s.ShareID) // Format in memory
                })
                .ToList();

            return Json(shareholders, JsonRequestBehavior.AllowGet);
        }

        //public ActionResult Edit(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }

        //    ShareTransfer shareTransfer = db.ShareTransfers.Find(id);
        //    if (shareTransfer == null)
        //    {
        //        return HttpNotFound();
        //    }

        //    // Fetch the list of all shareholders for the dropdowns
        //    var shareholders = db.Shareholders
        //                         .OrderBy(s => s.FullNameEng)
        //                         .ToList()
        //                         .Select(s => new SelectListItem
        //                         {
        //                             Value = s.ShID.ToString(),
        //                             Text = $"{s.FullNameEng} ({s.ShareID})"
        //                         }).ToList();

        //    shareholders.Insert(0, new SelectListItem { Value = "", Text = "Select Shareholder" });
        //    ViewBag.ShareholdersFrom = new SelectList(shareholders, "Value", "Text");
        //    ViewBag.ShareholdersTo = new SelectList(shareholders, "Value", "Text");

        //    if (shareTransfer.TransferDoc.HasValue)
        //    {
        //        var document = db.Documents.Find(shareTransfer.TransferDoc);
        //        if (document != null)
        //        {
        //            ViewBag.ExistingDocumentName = document.DocName;
        //            ViewBag.DocumentId = document.DocID;
        //        }
        //    }
        //    else
        //    {
        //        ViewBag.ExistingDocumentName = null;
        //        ViewBag.DocumentId = null;
        //    }


        //    // Extract checked SubID and PayID values from ShareTransfer
        //    ViewBag.CheckedSubscriptions = shareTransfer.SubID.Split(',').ToList();
        //    ViewBag.CheckedPayments = shareTransfer.PayID.Split(',').ToList();

        //    return View(shareTransfer);
        //}


        // POST: ShareTransfers/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, ShareTransfer shareTransfer, string selectedSubscriptionIds, string selectedPaymentIds, string TransferType, HttpPostedFileBase uploadedFile)
        {
            // Validate TransfareeShID
            if (!shareTransfer.TransfareeShID.HasValue)
            {
                ModelState.AddModelError("TransfareeShID", "Transfer To is required.");
            }

            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Validate session values
                        if (Session["ID"] == null || Session["Branch"] == null)
                        {
                            ModelState.AddModelError("", "User session has expired. Please log in again.");
                            return View(shareTransfer);
                        }
                        int userId = Convert.ToInt32(Session["ID"]);
                        int branchId = Convert.ToInt32(Session["Branch"]);

                        var existingShareTransfer = db.ShareTransfers.Find(id);
                        if (existingShareTransfer == null)
                        {
                            return HttpNotFound();
                        }

                        string transferCategory = shareTransfer.TransferCategory;

                        // Case 1: Validate duplicate Payment IDs
                        if ((transferCategory == "Subscription" && TransferType == "PaidUp") ||
                            (transferCategory == "Payment" && TransferType == "Full") ||
                            (transferCategory == "Payment" && TransferType == "Partial") ||
                            (transferCategory == "FullTransfer" && TransferType == "Full"))
                        {
                            if (!string.IsNullOrEmpty(selectedPaymentIds))
                            {
                                var newPaymentIds = selectedPaymentIds.Split(',')
                                    .Select(pid => pid.Trim())
                                    .Where(pid => int.TryParse(pid, out _))
                                    .Select(int.Parse)
                                    .ToList();

                                var pendingPayIdStrings = db.ShareTransfers
                                    .Where(t => t.TransferAuthorizationStatus == "Pending" && t.TransferID != id && !string.IsNullOrEmpty(t.PayID))
                                    .Select(t => t.PayID)
                                    .ToList();

                                var existingPendingPaymentIds = pendingPayIdStrings
                                    .SelectMany(payIds => payIds.Split(',')
                                        .Select(pid => pid.Trim())
                                        .Where(pid => int.TryParse(pid, out _))
                                        .Select(int.Parse))
                                    .Distinct()
                                    .ToList();

                                var duplicatePayments = newPaymentIds.Intersect(existingPendingPaymentIds).ToList();

                                if (duplicatePayments.Any())
                                {
                                    TempData["ErrorMessage"] = "The following Payment IDs are already in a pending transfer:<br><br>• " +
                                                               string.Join("<br>• ", duplicatePayments) +
                                                               "<br><br>Please take action before updating the transfer.";
                                    return RedirectToAction("Edit", new { id });
                                }
                            }
                        }

                        // Case 2: Validate duplicate Subscription IDs
                        if (transferCategory == "Subscription" && TransferType == "Right")
                        {
                            if (!string.IsNullOrEmpty(selectedSubscriptionIds))
                            {
                                var newSubscriptionIds = selectedSubscriptionIds.Split(',')
                                    .Select(subid => subid.Trim())
                                    .Where(subid => int.TryParse(subid, out _))
                                    .Select(int.Parse)
                                    .ToList();

                                var pendingSubIdStrings = db.ShareTransfers
                                    .Where(t => t.TransferAuthorizationStatus == "Pending" && t.TransferID != id && string.IsNullOrEmpty(t.PayID) && !string.IsNullOrEmpty(t.SubID))
                                    .Select(t => t.SubID)
                                    .ToList();

                                var existingPendingSubIds = pendingSubIdStrings
                                    .SelectMany(subIds => subIds.Split(',')
                                        .Select(subid => subid.Trim())
                                        .Where(subid => int.TryParse(subid, out _))
                                        .Select(int.Parse))
                                    .Distinct()
                                    .ToList();

                                var duplicateSubscriptions = newSubscriptionIds.Intersect(existingPendingSubIds).ToList();

                                if (duplicateSubscriptions.Any())
                                {
                                    TempData["ErrorMessage"] = "The following Subscription IDs are already in a Pending Right transfer:<br><br>• " +
                                                               string.Join("<br>• ", duplicateSubscriptions) +
                                                               "<br><br>Please take action before updating the transfer.";
                                    return RedirectToAction("Edit", new { id });
                                }
                            }
                        }

                        // Preserve existing TransferrorShID since it's disabled and not submitted
                        existingShareTransfer.TransferrorShID = existingShareTransfer.TransferrorShID;
                        existingShareTransfer.CreatedBy = userId;
                        existingShareTransfer.TransfareeShID = shareTransfer.TransfareeShID;
                        existingShareTransfer.TransferCategory = shareTransfer.TransferCategory;
                        existingShareTransfer.SubID = selectedSubscriptionIds;
                        existingShareTransfer.PayID = selectedPaymentIds;
                        existingShareTransfer.TransferType = TransferType;
                        existingShareTransfer.NumSharesTransferred = shareTransfer.NumSharesTransferred;
                        existingShareTransfer.AmountPerShare = shareTransfer.AmountPerShare;
                        existingShareTransfer.PaidAmountForTransfer = shareTransfer.PaidAmountForTransfer;
                        existingShareTransfer.TransferReason = shareTransfer.TransferReason;
                        existingShareTransfer.DividenedFor = shareTransfer.DividenedFor;
                        existingShareTransfer.TransferDate = shareTransfer.TransferDate;
                        existingShareTransfer.Remark = shareTransfer.Remark;

                        if (uploadedFile != null && uploadedFile.ContentLength > 0)
                        {
                            Document document = new Document
                            {
                                DocOwner = "Shareholder",
                                DocType = "Transfer Document",
                                ShID = existingShareTransfer.TransferrorShID,
                                CreatedBy = userId,
                                DocAuthorizationStatus = "Pending",
                                CreatedDate = DateTime.Now
                            };

                            DocumentsController documentsController = new DocumentsController();
                            documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

                            try
                            {
                                int documentId = documentsController.Create(document, uploadedFile, shareTransfer.TransfareeShID);
                                if (documentId > 0)
                                {
                                    existingShareTransfer.TransferDoc = documentId;
                                }
                                else
                                {
                                    throw new Exception("Document creation failed.");
                                }
                            }
                            catch (Exception ex)
                            {
                                ModelState.AddModelError("", $"Error creating document: {ex.Message}");
                                throw;
                            }
                        }

                        db.Entry(existingShareTransfer).State = EntityState.Modified;
                        db.SaveChanges();

                        AuditLogsController auditLogsController = new AuditLogsController();
                        auditLogsController.RecordLog("Modification", shareTransfer.TransferID, "Transfer", userId, Session["BranchName"]?.ToString() ?? "Unknown");

                        transaction.Commit();
                        TempData["SuccessMessage"] = "Transfer updated successfully!";
                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        ModelState.AddModelError("", $"Error updating the share transfer: {ex.Message}");
                    }
                }
            }

            // Repopulate ViewBag for error case
            var transfer = db.ShareTransfers.Find(id);
            if (transfer != null)
            {
                if (transfer.TransferrorShID.HasValue)
                {
                    var transferror = db.Shareholders.Find(transfer.TransferrorShID);
                    if (transferror != null)
                    {
                        ViewBag.SelectedTransferror = new SelectListItem
                        {
                            Value = transferror.ShID.ToString(),
                            Text = $"{transferror.FullNameEng} ({transferror.ShareID})"
                        };
                    }
                }

                if (transfer.TransfareeShID.HasValue)
                {
                    var transferee = db.Shareholders.Find(transfer.TransfareeShID);
                    if (transferee != null)
                    {
                        ViewBag.SelectedTransferee = new SelectListItem
                        {
                            Value = transferee.ShID.ToString(),
                            Text = $"{transferee.FullNameEng} ({transferee.ShareID})"
                        };
                    }
                }

                if (transfer.TransferDoc.HasValue)
                {
                    var document = db.Documents.Find(transfer.TransferDoc);
                    if (document != null)
                    {
                        ViewBag.ExistingDocumentName = document.DocName;
                        ViewBag.DocumentId = document.DocID;
                    }
                }

                ViewBag.CheckedSubscriptions = transfer.SubID?.Split(',').ToList() ?? new List<string>();
                ViewBag.CheckedPayments = transfer.PayID?.Split(',').ToList() ?? new List<string>();
            }

            // Repopulate other dropdowns
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode", selectedPaymentIds);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus", selectedSubscriptionIds);
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
            TempData["SuccessMessage"] = "Transfer Deleted Successfully!";
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
