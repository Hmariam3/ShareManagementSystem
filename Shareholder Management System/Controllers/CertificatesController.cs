using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Shareholder_Management_System.Models;

namespace Shareholder_Management_System.Controllers
{
    public class CertificatesController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();
        public ActionResult FilterPending()
        {
            var certificates = db.Certificates.Include(c => c.Shareholder).ToList().Where(a => a.CertAuthorizationStatus == "Pending");

            return View(certificates);
        }
        // GET: Certificates
        public ActionResult Index(int? ShID)
        {
            var certificates = Enumerable.Empty<Certificate>().AsQueryable();
            string selectedShID = null;
            Shareholder shareholder = null;
            // Log the incoming ShID from query string
            var queryShID = Request.QueryString["ShID"];
            System.Diagnostics.Debug.WriteLine($"Index Action: QueryString ShID = {queryShID}, ShID parameter = {ShID}");

            if (ShID.HasValue && ShID != 0)
            {
                if (!db.Shareholders.Any(s => s.ShID == ShID.Value))
                {
                    ModelState.AddModelError("ShID", $"Shareholder ID {ShID} not found.");
                    System.Diagnostics.Debug.WriteLine($"ModelState Error: Shareholder ID {ShID} not found.");
                }
                else
                {
                    shareholder = db.Shareholders
                   .FirstOrDefault(s => s.ShID == ShID.Value);

                    certificates = db.Certificates
                        .Include(c => c.Shareholder)
                        .Include(c => c.User)
                        .Where(c => c.ShID == ShID.Value);
                    selectedShID = ShID.Value.ToString();
                    ViewBag.SelectedShId = shareholder.ShID;
                    ViewBag.SelectedShName = shareholder.FullNameEng + " (" + shareholder.ShareID + ")";
                    ViewBag.ShareID = shareholder.ShareID;
                }
            }
            //else if (queryShID != null)
            //{
            //    // Log and reject invalid ShID from query string
            //    ModelState.AddModelError("ShID", $"Invalid Shareholder ID: {queryShID}. Expected a valid integer.");
            //    System.Diagnostics.Debug.WriteLine($"ModelState Error: Invalid Shareholder ID: {queryShID}");
            //}


            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                System.Diagnostics.Debug.WriteLine("ModelState Errors: " + string.Join("; ", errors));
                ViewBag.ModelStateErrors = errors;
            }

            return View(certificates.ToList());
        }
        //public ActionResult GetCertificates(string searchQuery)
        //{
        //    var certificates = db.Certificates.AsQueryable();

        //    if (!string.IsNullOrEmpty(searchQuery))
        //    {
        //        certificates = certificates.Where(c => c.Shareholder.FullNameEng.Contains(searchQuery) || c.Shareholder.ShareID.Contains(searchQuery));
        //    }

        //    var result = certificates.Select(c => new
        //    {
        //        c.CertID,
        //        c.CertNum,
        //        Shareholder = new { c.Shareholder.ShareID, c.Shareholder.FullNameEng, c.Shareholder.FullNameAfanOromo, c.Shareholder.SHCategory },
        //        c.BeginingSerial,
        //        c.EndingSerial,
        //        c.TotalPaidupAmount,
        //        User = new { c.User.FullName },
        //        c.CertAuthorizationStatus,
        //        CertGenerationDate = c.CertGenerationDate, // Ensure ISO 8601 or /Date(1750885200000)/ format
        //        c.Remark,
        //        c.RevokeStatus
        //    }).ToList();

        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}

        // GET: Certificates/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Certificate certificate = db.Certificates.Find(id);
            if (certificate == null)
            {
                return HttpNotFound();
            }

            return View(certificate);
        }

        // GET: Certificates/Create
        public ActionResult Create()
        {
            TempData["ErrorMessage"] = "";
            // Fetch the last certificate number and convert it after retrieving it in memory
            int lastCertNum = db.Certificates
                                .Where(c => c.CertNum != null) // Filter out any null CertNum
                                .ToList() // Fetch into memory
                                .Select(c => int.Parse(c.CertNum)) // Parse CertNum as an integer
                                .DefaultIfEmpty(0) // Handle case where no CertNum exists
                                .Max(); // Get the max value

            // Set the new certificate number by incrementing the last one
            var newCertNum = (lastCertNum + 1).ToString();
            ViewBag.Shareholders = db.Shareholders
                                        .Where(s => s.Status == "Active" && s.AuthorizationStatus == "Approved")
                                        .OrderBy(s => s.FullNameEng)
                                        .Select(s => new SelectListItem
                                        {
                                            Value = s.ShID.ToString(),
                                            Text = s.FullNameEng + " (" + s.ShareID + ")"
                                        }).ToList();


            // Assign the certificate number to ViewBag
            ViewBag.NewCertNum = newCertNum;
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID");
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");
            ViewBag.CertAuthorizer = new SelectList(db.Users, "UID", "FullName");
            return View();
        }

        // POST: Certificates/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CertID,ShID,CertNum,CreatedBy,CreatedDate,DeliveryStatus,DeliveredBy,DeliveryDate,CertAuthorizationStatus,CertGenerationDate,CertAuthorizer,Remark")] Certificate certificate, int[] selectedPayments)
        {
            TempData["ErrorMessage"] = "";
            if (selectedPayments == null || !selectedPayments.Any())
            {
                TempData["ErrorMessage"] = "At least one payment must be selected.";
                //PopulateCreateViewBag(certificate.ShID);
                return View(certificate);
            }

            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        int userId = Session["ID"] != null ? Convert.ToInt32(Session["ID"]) : throw new InvalidOperationException("User session expired.");

                        //// Calculate serials and certificate number
                        //int lastEndingSerial = db.Certificates.Any() ? db.Certificates.Max(c => c.EndingSerial).GetValueOrDefault() : 0;
                        //certificate.BeginingSerial = lastEndingSerial + 1;

                        //int lastCertNum = GetLastCertificateNumber();
                        //certificate.CertNum = (lastCertNum + 1).ToString();

                        // Calculate total paid amount
                        certificate.TotalPaidupAmount = db.Payments
                            .Where(p => selectedPayments.Contains(p.PayID))
                            .Sum(p => (int)p.PaidAmount);

                        //certificate.EndingSerial = certificate.BeginingSerial + (int)(certificate.TotalPaidupAmount / 1000) - 1;

                        // Check for duplicate payments
                        var existingCertificates = db.Certificates
                            .Where(c => c.PaymentIDs != null && c.PaymentIDs != "" && c.CertAuthorizationStatus != "Revoked" && c.CertAuthorizationStatus != "Rejected")
                            .ToList();

                        bool hasDuplicate = existingCertificates.Any(c =>
                        {
                            if (string.IsNullOrWhiteSpace(c.PaymentIDs))
                            {
                                System.Diagnostics.Debug.WriteLine($"Skipping invalid PaymentIDs for CertID {c.CertID}: '{c.PaymentIDs}'");
                                return false;
                            }

                            // Handle both single and comma-separated PaymentIDs
                            var paymentIdStrings = c.PaymentIDs.Contains(',')
                                ? c.PaymentIDs.Split(',').Where(id => !string.IsNullOrWhiteSpace(id))
                                : new[] { c.PaymentIDs.Trim() };

                            var paymentIds = paymentIdStrings
                                .Select(id =>
                                {
                                    bool isValid = int.TryParse(id.Trim(), out int parsedId);
                                    if (!isValid)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Invalid PaymentID in CertID {c.CertID}: '{id}'");
                                    }
                                    return new { IsValid = isValid, ParsedId = parsedId };
                                })
                                .Where(x => x.IsValid)
                                .Select(x => x.ParsedId);

                            return paymentIds.Intersect(selectedPayments).Any();
                        });

                        if (hasDuplicate)
                        {
                            TempData["ErrorMessage"] = "One or more selected payments are already used in another certificate.";
                            ViewBag.Shareholders = db.Shareholders
                              .Where(s => s.Status == "Active" && s.AuthorizationStatus == "Approved")
                              .OrderBy(s => s.FullNameEng)
                              .Select(s => new SelectListItem
                              {
                                  Value = s.ShID.ToString(),
                                  Text = s.FullNameEng + " (" + s.ShareID + ")"
                              }).ToList();
                            return View(certificate);
                        }

                        // Set certificate properties
                        certificate.PaymentIDs = string.Join(",", selectedPayments);
                        certificate.CertGenerationDate = DateTime.Now;
                        certificate.DeliveredBy = userId;
                        certificate.CreatedBy = userId;
                        certificate.CreatedDate = DateTime.Now;
                        certificate.CertAuthorizationStatus = "Pending";

                        // Save certificate
                        db.Certificates.Add(certificate);
                        db.SaveChanges();

                        // Log action
                        var auditLogsController = new AuditLogsController();
                        auditLogsController.RecordLog("Registration", certificate.CertID, "Certificate", certificate.CreatedBy, Session["BranchName"]?.ToString());

                        transaction.Commit();
                        return View();


                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"Error creating certificate: {ex.Message}");
                        TempData["ErrorMessage"] = "An error occurred while creating the certificate.";
                        ViewBag.Shareholders = db.Shareholders
                            .Where(s => s.Status == "Active" && s.AuthorizationStatus == "Approved")
                            .OrderBy(s => s.FullNameEng)
                            .Select(s => new SelectListItem
                            {
                                Value = s.ShID.ToString(),
                                Text = s.FullNameEng + " (" + s.ShareID + ")"
                            }).ToList();
                        return View(certificate);
                    }
                }
            }

            ViewBag.Shareholders = db.Shareholders
                             .Where(s => s.Status == "Active" && s.AuthorizationStatus == "Approved")
                             .OrderBy(s => s.FullNameEng)
                             .Select(s => new SelectListItem
                             {
                                 Value = s.ShID.ToString(),
                                 Text = s.FullNameEng + " (" + s.ShareID + ")"
                             }).ToList();
            return View(certificate);
        }

        //private void PopulateCreateViewBag(int shID)
        //{
        //    ViewBag.Shareholders = db.Shareholders
        //        .Where(s => s.Status == "Active" && s.AuthorizationStatus == "Approved")
        //        .OrderBy(s => s.FullNameEng)
        //        .Select(s => new SelectListItem
        //        {
        //            Value = s.ShID.ToString(),
        //            Text = $"{s.FullNameEng} ({s.ShareID})"
        //        })
        //        .ToList();

        //    ViewBag.SelectedShID = shID.ToString();
        //    ViewBag.NewCertNum = (GetLastCertificateNumber() + 1).ToString();
        //}

        [HttpGet]
        public JsonResult GetShareholders(string searchTerm)
        {
            var results = db.Shareholders
                .Where(s => s.Status == "Active" &&
                            s.AuthorizationStatus == "Approved" &&
                            (
                                string.IsNullOrEmpty(searchTerm) ||
                                s.FullNameEng.Contains(searchTerm) ||
                                s.ShareID.ToString().StartsWith(searchTerm)
                            ))
                .OrderBy(s => s.FullNameEng)
                .Select(s => new
                {
                    id = s.ShID,
                    text = s.FullNameEng + " (" + s.ShareID + ")",
                    shareID = s.ShareID
                })
                .Take(20)
                .ToList();

            return Json(results, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetShareholdersById(string searchTerm) 
        {
            var results = db.Shareholders
                .Where(s => s.Status == "Active" &&
                            s.AuthorizationStatus == "Approved" &&
                            (
                                string.IsNullOrEmpty(searchTerm) ||
                                s.FullNameEng.Contains(searchTerm) ||
                                s.ShID.ToString().StartsWith(searchTerm)
                            ))
                .OrderBy(s => s.FullNameEng)
                .Select(s => new
                {
                    id = s.ShID,
                    text = s.FullNameEng + " (" + s.ShareID + ")",
                    shareID = s.ShareID
                })
                .Take(20)
                .ToList();

            return Json(results, JsonRequestBehavior.AllowGet);
        }
        public ActionResult SearchShareholders(string term)
        {
            try
            {
                var shareholders = db.Shareholders
               .Where(s => string.IsNullOrEmpty(term) ||
                           s.FullNameEng.ToLower().StartsWith(term.ToLower()) ||
                           s.ShareID.ToLower().StartsWith(term.ToLower()))
               .Select(s => new
               {
                   id = s.ShID,
                   text = s.FullNameEng + " (" + s.ShareID + ")",
                   shareID = s.ShareID
               })
               .Take(500) // Limit results per search for performance
               .ToList();

                return Json(new { results = shareholders }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
        public JsonResult GetPaymentsByShID(int shID)
        {
            // Fetch the shareholder details
            var shareholder = db.Shareholders
                .Where(s => s.ShID == shID)
                .Select(s => new
                {
                    SHName = s.FullNameEng,
                    ShareNum = s.ShareID,
                    Region = s.Region,
                    Zone = s.Zone,
                    City = s.City,
                    Subcity = s.Subcity,
                    Woreda = s.Woreda,
                    Kebele = s.Kebele,
                    HouseNo = s.HouseNo,
                    PhoneNo = s.PhoneNo,
                })
                .FirstOrDefault();

            // If the shareholder is not found, return an empty response
            if (shareholder == null)
            {
                return Json(new { message = "Shareholder not found" }, JsonRequestBehavior.AllowGet);
            }

            // Fetch payments related to the shareholder
            var payments = db.Payments
                .Where(p => p.ShID == shID && p.PaidAmount != 0 && p.PaymentAuthorizationStatus == "Approved")
                .Select(p => new
                {
                    SHName = p.Shareholder.FullNameEng,
                    PayID = p.PayID,
                    PaymentMode = p.PaymentMode,
                    Branch = p.Branch,
                    PaidAmount = p.PaidAmount,
                    BlockedAmount = p.BlockedAmount,
                    ReferenceNum = p.ReferenceNum,
                    PaymentDate = p.PaymentDate // Leave as DateTime here
                })
                .ToList();

            // Format the PaymentDate after retrieving the payments
            var formattedPayments = payments.Select(p => new
            {
                p.SHName,
                p.PayID,
                p.PaymentMode,
                p.Branch,
                p.PaidAmount,
                p.BlockedAmount,
                p.ReferenceNum,
                PaymentDate = p.PaymentDate.ToString() // Format here
            }).ToList();

            // Return the shareholder details along with their payments
            return Json(new
            {
                Shareholder = shareholder,
                Payments = formattedPayments
            }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetPaymentDetails(IEnumerable<int> payIDs, int shID)
        {
            // Ensure both parameters are used correctly in your logic
            var payments = db.Payments
                             .Where(p => payIDs.Contains(p.PayID) && p.ShID == shID) // Example filter
                             .Select(p => new
                             {
                                 PayID = p.PayID,
                                 PerShareValue = 100,
                                 PaidAmount = p.PaidAmount,
                                 NoofShares = p.PaidAmount / 100,
                                 PaymentDate = p.PaymentDate.ToString(),
                                 ReferenceNum = p.ReferenceNum
                             })
                             .ToList();

            return Json(payments, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Certificate certificate = db.Certificates.Find(id);
            if (certificate == null)
            {
                return HttpNotFound();
            }

            // Set ViewBag.SelectedShID to pre-populate the Select2 dropdown
            ViewBag.SelectedShID = certificate.ShID.ToString();

            // Populate ViewBag.Shareholders for fallback (optional, as the view uses AJAX)
            ViewBag.Shareholders = db.Shareholders
                .Select(s => new SelectListItem
                {
                    Value = s.ShID.ToString(),
                    Text = s.FullNameEng,
                    Selected = s.ShID == certificate.ShID // Pre-select the current shareholder
                })
                .ToList();

            // Populate other ViewBag properties if needed
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", certificate.CreatedBy);
            ViewBag.CertAuthorizer = new SelectList(db.Users, "UID", "FullName", certificate.CertAuthorizer);

            return View(certificate);
        }
        public ActionResult SplitCertificate(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Certificate certificate = db.Certificates.Find(id);
            if (certificate == null)
            {
                return HttpNotFound();
            }
            // Populate ViewBag.Shareholders with a list of SelectListItem
            ViewBag.Shareholders = db.Shareholders
                .Select(s => new SelectListItem
                {
                    Value = s.ShID.ToString(), // ShID as the value
                    Text = s.FullNameEng      // FullNameEng as the display text
                })
                .ToList();
            return View(certificate);
        }
        public ActionResult SplitPayment()
        {
            var shareholders = db.Shareholders
             .Where(s => s.Status.Equals("Active") && s.AuthorizationStatus.Equals("Approved")) // Filter by Status and AuthorizationStatus
             .OrderBy(s => s.FullNameEng) // Order by FullNameEng
             .Select(s => new SelectListItem
             {
                 Value = s.ShID.ToString(), // ShID as value
                 Text = s.FullNameEng + " (" + s.ShareID + ")"  // Display FullNameEng and ID Number
             })
              .ToList();
            // Add a default option
            shareholders.Insert(0, new SelectListItem
            {
                Value = "", // Null value for the default option
                Text = "Select a Shareholder" // Text for the default option
            });

            ViewBag.Shareholders = shareholders;


            return View();
        }
        [HttpGet]
        public JsonResult GetLastEndingSerial()
        {
            var lastEndingSerial = db.Certificates.Any() ? db.Certificates.Max(c => c.EndingSerial).GetValueOrDefault() : 0;
            return Json(new { lastEndingSerial }, JsonRequestBehavior.AllowGet);
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult SplitPayment([Bind(Include = "CertID,ShID,CertNum,CreatedBy,CreatedDate,DeliveryStatus,DeliveredBy,DeliveryDate,CertAuthorizationStatus,CertGenerationDate,CertAuthorizer,Remark")]Certificate certificate, string SelectedPayID, decimal[] RequestedAmounts)
        //{
        //    try
        //    {
        //        // Log incoming data
        //        System.Diagnostics.Debug.WriteLine($"Entering SplitPayment - SelectedPayID: {SelectedPayID}");
        //        System.Diagnostics.Debug.WriteLine($"RequestedAmounts: {(RequestedAmounts != null ? RequestedAmounts.Length : 0)} items");
        //        if (RequestedAmounts != null)
        //        {
        //            foreach (var amount in RequestedAmounts)
        //            {
        //                System.Diagnostics.Debug.WriteLine($"Requested Amount: {amount}");
        //            }
        //        }
        //        System.Diagnostics.Debug.WriteLine($"Certificate: ShID={certificate?.ShID}, Remark={certificate?.Remark}");

        //        // Minimal checks to avoid null references
        //        if (certificate == null)
        //        {
        //            System.Diagnostics.Debug.WriteLine("Certificate object is null");
        //            PopulateShareholdersDropDown(0); // Default ShID
        //            return View(new Certificate());
        //        }

        //        if (string.IsNullOrEmpty(SelectedPayID))
        //        {
        //            System.Diagnostics.Debug.WriteLine("SelectedPayID is null or empty");
        //            ModelState.AddModelError("", "Please select a payment.");
        //            PopulateShareholdersDropDown(certificate.ShID);
        //            return View(certificate);
        //        }

        //        // Fetch all certificates that contain the selected payment ID
        //        var existingCertificates = db.Certificates
        //            .Where(c => c.PaymentIDs.Contains(SelectedPayID))
        //            .ToList();

        //        // Get the total amount linked to the selected payment
        //        var totalPaymentAmount = db.Payments
        //            .Where(p => p.PayID.ToString() == SelectedPayID)
        //            .Select(p => p.PaidAmount)
        //            .FirstOrDefault();

        //        // Calculate the already certified amount
        //        var alreadyCertifiedAmount = existingCertificates.Sum(c => (c.EndingSerial - c.BeginingSerial + 1) * 100);

        //        // Determine the remaining amount
        //        var remainingAmount = totalPaymentAmount - alreadyCertifiedAmount;

        //        System.Diagnostics.Debug.WriteLine($"Total Payment Amount: {totalPaymentAmount}, Already Certified: {alreadyCertifiedAmount}, Remaining: {remainingAmount}");

        //        if (remainingAmount <= 0)
        //        {
        //            System.Diagnostics.Debug.WriteLine("No remaining amount for certification.");
        //            ModelState.AddModelError("", "This payment has already been fully certified.");
        //            PopulateShareholdersDropDown(certificate.ShID);
        //            return View(certificate);
        //        }

        //        // Validate if requested amounts exceed the remaining amount
        //        decimal requestedTotal = RequestedAmounts != null ? RequestedAmounts.Sum() : 0;
        //        if (requestedTotal > remainingAmount)
        //        {
        //            System.Diagnostics.Debug.WriteLine($"Requested amount {requestedTotal} exceeds remaining amount {remainingAmount}");
        //            ModelState.AddModelError("", "Requested amount exceeds the remaining available balance.");
        //            PopulateShareholdersDropDown(certificate.ShID);
        //            return View(certificate);
        //        }

        //        if (RequestedAmounts == null || !RequestedAmounts.Any())
        //        {
        //            System.Diagnostics.Debug.WriteLine("RequestedAmounts is null or empty");
        //            ModelState.AddModelError("", "Please specify at least one requested amount.");
        //            PopulateShareholdersDropDown(certificate.ShID);
        //            return View(certificate);
        //        }

        //        int userId = Convert.ToInt32(Session["ID"] ?? 0);
        //        System.Diagnostics.Debug.WriteLine($"UserID from session: {userId}");
        //        var createdDate = DateTime.Now;

        //        // Get last certificate details
        //        var lastCertificate = db.Certificates
        //            .OrderByDescending(c => c.EndingSerial)
        //            .FirstOrDefault();
        //        int lastEndingSerial = lastCertificate?.EndingSerial ?? 0;
        //        int lastCertNum = GetLastCertificateNumber();
        //        System.Diagnostics.Debug.WriteLine($"LastEndingSerial: {lastEndingSerial}, LastCertNum: {lastCertNum}");

        //        // Create certificates
        //        var newCertificates = new List<Certificate>();
        //        try
        //        {
        //            System.Diagnostics.Debug.WriteLine("Entering certificate creation");
        //            int? currentSerial = lastEndingSerial + 1;

        //            for (int i = 0; i < RequestedAmounts.Length; i++)
        //            {
        //                var amount = RequestedAmounts[i];
        //                System.Diagnostics.Debug.WriteLine($"Processing amount {i}: {amount}");

        //                if (amount <= 0)
        //                {
        //                    System.Diagnostics.Debug.WriteLine($"Skipping amount {amount} as it’s <= 0");
        //                    continue;
        //                }

        //                var newCert = new Certificate
        //                {
        //                    ShID = certificate.ShID,
        //                    CreatedBy = userId,
        //                    CreatedDate = createdDate,
        //                    DeliveryStatus = certificate.DeliveryStatus,
        //                    DeliveredBy = userId,
        //                    DeliveryDate = certificate.DeliveryDate,
        //                    CertAuthorizationStatus = "Pending",
        //                    CertGenerationDate = createdDate,
        //                    CertAuthorizer = certificate.CertAuthorizer,
        //                    Remark = certificate.Remark,
        //                    PaymentIDs = SelectedPayID,
        //                    CertNum = (lastCertNum + i + 1).ToString(),
        //                    BeginingSerial = currentSerial,
        //                    EndingSerial = currentSerial + (int)(amount / 100) - 1,
        //                    TotalPaidupAmount = (((currentSerial + (int)(amount / 100) - 1) - currentSerial) + 1) * 100,
        //                };

        //                currentSerial = newCert.EndingSerial + 1;
        //                newCertificates.Add(newCert);
        //                System.Diagnostics.Debug.WriteLine($"Created certificate: CertNum={newCert.CertNum}, BeginingSerial={newCert.BeginingSerial}, EndingSerial={newCert.EndingSerial}");
        //            }

        //            System.Diagnostics.Debug.WriteLine($"Created {newCertificates.Count} new certificates");
        //        }
        //        catch (Exception ex)
        //        {
        //            System.Diagnostics.Debug.WriteLine($"Error creating certificates: {ex.Message}");
        //            System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
        //            PopulateShareholdersDropDown(certificate.ShID);
        //            return View(certificate);
        //        }

        //        // Save to database
        //        using (var transaction = db.Database.BeginTransaction())
        //        {
        //            try
        //            {
        //                System.Diagnostics.Debug.WriteLine($"Adding {newCertificates.Count} certificates to context");
        //                db.Certificates.AddRange(newCertificates);
        //                int rowsAffected = db.SaveChanges();
        //                System.Diagnostics.Debug.WriteLine($"Rows affected: {rowsAffected}");

        //                transaction.Commit();
        //                // Call RecordLog method with null-safe value for CreatedBy
        //                AuditLogsController auditLogsController = new AuditLogsController();
        //                auditLogsController.RecordLog("Registration", certificate.CertID, "Certificate", certificate.CreatedBy, Session["BranchName"].ToString());
        //                System.Diagnostics.Debug.WriteLine("Transaction committed successfully");
        //                return RedirectToAction("Index");
        //            }
        //            catch (Exception ex)
        //            {
        //                transaction.Rollback();
        //                System.Diagnostics.Debug.WriteLine($"Save error: {ex.Message}");
        //                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
        //                PopulateShareholdersDropDown(certificate.ShID);
        //                return View(certificate);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"Unexpected error: {ex.Message}");
        //        System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
        //        PopulateShareholdersDropDown(certificate?.ShID ?? 0);
        //        return View(certificate ?? new Certificate());
        //    }
        //}

        // Helper method to populate shareholders dropdown
        private void PopulateShareholdersDropDown(int selectedShID)
        {
            var shareholders = db.Shareholders
                .Where(s => s.Status.Equals("Active") && s.AuthorizationStatus.Equals("Approved"))
                .OrderBy(s => s.FullNameEng)
                .Select(s => new SelectListItem
                {
                    Value = s.ShID.ToString(),
                    Text = s.FullNameEng + " (" + s.ShareID + ")"
                })
                .ToList();

            ViewBag.Shareholders = shareholders;
            ViewBag.SelectedShID = selectedShID; // Preserve selected value
        }
        // Helper method: GetLastCertificateNumber
        private int GetLastCertificateNumber()
        {
            try
            {
                var certNums = db.Certificates
                    .Where(c => c.CertNum != null)
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

        // Helper method: PopulateShareholdersDropDown
        private List<SelectListItem> PopulateShareholdersDropDown(int selectedShID, bool includeEmptyOption = false)
        {
            try
            {
                const string ACTIVE_STATUS = "Active";
                const string APPROVED_STATUS = "Approved";

                var shareholdersQuery = db.Shareholders
                    .Where(s => s.Status == ACTIVE_STATUS &&
                               s.AuthorizationStatus == APPROVED_STATUS &&
                               s.FullNameEng != null &&
                               s.ShID > 0)
                    .OrderBy(s => s.FullNameEng)
                    .Select(s => new SelectListItem
                    {
                        Value = s.ShID.ToString(),
                        Text = $"{s.FullNameEng} ({s.ShareID ?? "N/A"})",
                        Selected = s.ShID == selectedShID
                    });

                var shareholdersList = shareholdersQuery.ToList();

                if (includeEmptyOption)
                {
                    shareholdersList.Insert(0, new SelectListItem
                    {
                        Value = "",
                        Text = "-- Select Shareholder --",
                        Selected = selectedShID == 0
                    });
                }

                ViewBag.Shareholders = shareholdersList;
                ViewBag.SelectedShID = selectedShID;
                System.Diagnostics.Debug.WriteLine($"Populated {shareholdersList.Count} shareholders, SelectedShID: {selectedShID}");
                return shareholdersList;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error populating shareholders: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                var errorList = new List<SelectListItem>
        {
            new SelectListItem { Value = "", Text = "Error loading shareholders" }
        };
                ViewBag.Shareholders = errorList;
                ViewBag.SelectedShID = 0;
                return errorList;
            }
        }
        //[HttpPost]
        //public ActionResult SplitCertificate(int certID, List<int> RequestedAmounts, List<int> BeginSerials, List<int> EndSerials, string remark)
        //{
        //    // Validate input data
        //    if (certID <= 0)
        //    {
        //        ModelState.AddModelError("certID", "Invalid certificate ID.");
        //    }
        //    if (RequestedAmounts == null || RequestedAmounts.Count == 0)
        //    {
        //        ModelState.AddModelError("RequestedAmounts", "Requested amounts are required.");
        //    }
        //    if (BeginSerials == null || BeginSerials.Count == 0)
        //    {
        //        ModelState.AddModelError("BeginSerials", "Beginning serials are required.");
        //    }
        //    if (EndSerials == null || EndSerials.Count == 0)
        //    {
        //        ModelState.AddModelError("EndSerials", "Ending serials are required.");
        //    }
        //    if (RequestedAmounts != null && BeginSerials != null && EndSerials != null &&
        //        (RequestedAmounts.Count != BeginSerials.Count || RequestedAmounts.Count != EndSerials.Count))
        //    {
        //        ModelState.AddModelError("", "Amounts and serial ranges must match in count.");
        //    }

        //    if (!ModelState.IsValid)
        //    {
        //        return View("SplitCertificate", new { CertID = certID, RequestedAmounts, BeginSerials, EndSerials, Remark = remark });
        //    }

        //    // Fetch the certificate
        //    var certificate = db.Certificates.FirstOrDefault(c => c.CertID == certID);
        //    if (certificate == null)
        //    {
        //        ModelState.AddModelError("certID", "Certificate not found.");
        //        return View("SplitCertificate", new { CertID = certID, RequestedAmounts, BeginSerials, EndSerials, Remark = remark });
        //    }

        //    // Get total paid-up amount from payments
        //    var paymentIDs = certificate.PaymentIDs?.Split(',').Select(int.Parse).ToList();
        //    if (paymentIDs == null || !paymentIDs.Any())
        //    {
        //        ModelState.AddModelError("PaymentIDs", "No payments associated with this certificate.");
        //        return View("SplitCertificate", new { CertID = certID, RequestedAmounts, BeginSerials, EndSerials, Remark = remark });
        //    }

        //    int? totalPaidUpAmount = certificate.TotalPaidupAmount;

        //    // Validate total requested amount
        //    int totalRequestedAmount = RequestedAmounts.Sum();
        //    if (totalRequestedAmount > totalPaidUpAmount)
        //    {
        //        ModelState.AddModelError("RequestedAmounts", "Total requested amount exceeds original paid-up amount.");
        //        return View("SplitCertificate", new { CertID = certID, RequestedAmounts, BeginSerials, EndSerials, Remark = remark });
        //    }

        //    // Validate serial ranges and calculate child serials from the end
        //    int? originalBeginSerial = certificate.BeginingSerial;
        //    int? originalEndSerial = certificate.EndingSerial;
        //    int? totalSerials = originalEndSerial - originalBeginSerial + 1;
        //    int totalSerialsRequested = BeginSerials.Zip(EndSerials, (b, e) => e - b + 1).Sum();

        //    for (int i = 0; i < BeginSerials.Count; i++)
        //    {
        //        if (BeginSerials[i] < originalBeginSerial || EndSerials[i] > originalEndSerial ||
        //            BeginSerials[i] > EndSerials[i] || RequestedAmounts[i] <= 0)
        //        {
        //            ModelState.AddModelError($"BeginSerials[{i}]", $"Invalid serial range or amount for split {i + 1}.");
        //            return View("SplitCertificate", new { CertID = certID, RequestedAmounts, BeginSerials, EndSerials, Remark = remark });
        //        }

        //        for (int j = 0; j < i; j++)
        //        {
        //            if (BeginSerials[i] <= EndSerials[j] && EndSerials[i] >= BeginSerials[j])
        //            {
        //                ModelState.AddModelError($"BeginSerials[{i}]", $"Serial range overlap detected in split {i + 1}.");
        //                return View("SplitCertificate", new { CertID = certID, RequestedAmounts, BeginSerials, EndSerials, Remark = remark });
        //            }
        //        }

        //        int numberOfShares = EndSerials[i] - BeginSerials[i] + 1;
        //        if (RequestedAmounts[i] != numberOfShares * 100)
        //        {
        //            ModelState.AddModelError($"RequestedAmounts[{i}]", $"Amount for split {i + 1} does not match serial range.");
        //            return View("SplitCertificate", new { CertID = certID, RequestedAmounts, BeginSerials, EndSerials, Remark = remark });
        //        }
        //    }

        //    var newCertificates = new List<Certificate>();

        //    // Get the last certificate number once
        //    int lastCertNum = db.Certificates
        //        .Where(c => c.CertNum != null)
        //        .AsEnumerable()
        //        .Select(c => int.TryParse(c.CertNum, out int num) ? num : 0)
        //        .DefaultIfEmpty(0)
        //        .Max();

        //    // Sort splits by BeginSerials in descending order to assign from the end
        //    var splits = BeginSerials.Zip(EndSerials, (b, e) => new { Begin = b, End = e })
        //        .Zip(RequestedAmounts, (se, a) => new { Begin = se.Begin, End = se.End, Amount = a })
        //        .OrderByDescending(s => s.Begin)
        //        .ToList();

        //    int? currentEndSerial = originalEndSerial;

        //    // Process each split from the end
        //    for (int i = 0; i < splits.Count; i++)
        //    {
        //        lastCertNum++;
        //        string newCertNum = lastCertNum.ToString();

        //        int numberOfShares = splits[i].End - splits[i].Begin + 1;
        //        int? newEndSerial = currentEndSerial;
        //        int? newBeginSerial = newEndSerial - numberOfShares + 1;

        //        var newCertificate = new Certificate
        //        {
        //            ShID = certificate.ShID,
        //            CertNum = newCertNum,
        //            CreatedBy = certificate.CreatedBy,
        //            CreatedDate = DateTime.Now,
        //            CertGenerationDate = DateTime.Now,
        //            CertAuthorizationStatus = "Approved",
        //            Remark = remark,
        //            BeginingSerial = newBeginSerial,
        //            EndingSerial = newEndSerial,
        //            PaymentIDs = certificate.PaymentIDs,
        //            ParentCertId = certID,
        //            TotalPaidupAmount = numberOfShares * 100
        //        };

        //        db.Certificates.Add(newCertificate);
        //        newCertificates.Add(newCertificate);

        //        // Update the current end serial for the next split
        //        currentEndSerial = newBeginSerial - 1;
        //    }

        //    // Update parent certificate to keep the beginning serials
        //    if (totalSerialsRequested < totalSerials)
        //    {
        //        certificate.SplitStatus = 1;
        //        certificate.EndingSerial = originalBeginSerial + (totalSerials - totalSerialsRequested) - 1;
        //        certificate.TotalPaidupAmount = (certificate.EndingSerial - certificate.BeginingSerial + 1) * 100;
        //    }
        //    else
        //    {
        //        certificate.SplitStatus = 1;
        //        certificate.BeginingSerial = 0;
        //        certificate.EndingSerial = 0;
        //        certificate.TotalPaidupAmount = 0;
        //    }

        //    // Save changes
        //    db.Entry(certificate).State = EntityState.Modified;
        //    db.SaveChanges();

        //    AuditLogsController auditLogsController = new AuditLogsController();
        //    auditLogsController.RecordLog("Edit", certificate.CertID, "Certificate", certificate.CreatedBy, Session["BranchName"].ToString());

        //    return RedirectToAction("Index");
        //}

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, [Bind(Include = "CertID,ShID,PaymentIDs,BeginingSerial,EndingSerial,CertNum,CreatedBy,CreatedDate,DeliveryStatus,DeliveredBy,DeliveryDate,CertAuthorizationStatus,CertGenerationDate,CertAuthorizer,Remark")] Certificate certificate, int[] selectedPayments)
        {
            TempData["ErrorMessage"] = "";

            // Validate selected payments
            if (selectedPayments == null || !selectedPayments.Any())
            {
                TempData["ErrorMessage"] = "At least one payment must be selected.";
                ViewBag.Shareholders = db.Shareholders
                    .Where(s => s.Status == "Active" && s.AuthorizationStatus == "Approved")
                    .OrderBy(s => s.FullNameEng)
                    .Select(s => new SelectListItem
                    {
                        Value = s.ShID.ToString(),
                        Text = s.FullNameEng + " (" + s.ShareID + ")"
                    }).ToList();
                return View(certificate);
            }

            if (ModelState.IsValid)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Retrieve the existing certificate
                        var existingCertificate = db.Certificates.Find(id);
                        if (existingCertificate == null)
                        {
                            TempData["ErrorMessage"] = "Certificate not found.";
                            ViewBag.Shareholders = db.Shareholders
                                .Where(s => s.Status == "Active" && s.AuthorizationStatus == "Approved")
                                .OrderBy(s => s.FullNameEng)
                                .Select(s => new SelectListItem
                                {
                                    Value = s.ShID.ToString(),
                                    Text = s.FullNameEng + " (" + s.ShareID + ")"
                                }).ToList();
                            return View(certificate);
                        }

                        // Check for duplicate payments in other certificates
                        var existingCertificates = db.Certificates
                            .Where(c => c.CertID != id && c.PaymentIDs != null && c.PaymentIDs != "" && c.CertAuthorizationStatus != "Revoked" && c.CertAuthorizationStatus != "Rejected")
                            .ToList();

                        bool hasDuplicate = existingCertificates.Any(c =>
                        {
                            if (string.IsNullOrWhiteSpace(c.PaymentIDs))
                            {
                                System.Diagnostics.Debug.WriteLine($"Skipping invalid PaymentIDs for CertID {c.CertID}: '{c.PaymentIDs}'");
                                return false;
                            }

                            // Handle both single and comma-separated PaymentIDs
                            var paymentIdStrings = c.PaymentIDs.Contains(',')
                                ? c.PaymentIDs.Split(',').Where(i => !string.IsNullOrWhiteSpace(i))
                                : new[] { c.PaymentIDs.Trim() };

                            var paymentIds = paymentIdStrings
                                .Select(i =>
                                {
                                    bool isValid = int.TryParse(i.Trim(), out int parsedId);
                                    if (!isValid)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Invalid PaymentID in CertID {c.CertID}: '{id}'");
                                    }
                                    return new { IsValid = isValid, ParsedId = parsedId };
                                })
                                .Where(x => x.IsValid)
                                .Select(x => x.ParsedId);

                            return paymentIds.Intersect(selectedPayments).Any();
                        });

                        if (hasDuplicate)
                        {
                            TempData["ErrorMessage"] = "One or more selected payments are already used in another certificate.";
                            ViewBag.Shareholders = db.Shareholders
                                .Where(s => s.Status == "Active" && s.AuthorizationStatus == "Approved")
                                .OrderBy(s => s.FullNameEng)
                                .Select(s => new SelectListItem
                                {
                                    Value = s.ShID.ToString(),
                                    Text = s.FullNameEng + " (" + s.ShareID + ")"
                                }).ToList();
                            return View(certificate);
                        }

                        // Update specific fields from the form
                        existingCertificate.ShID = certificate.ShID;
                        existingCertificate.PaymentIDs = string.Join(",", selectedPayments);

                        existingCertificate.Remark = certificate.Remark;

                        // Recalculate TotalPaidupAmount based on selected payments
                        existingCertificate.TotalPaidupAmount = db.Payments
                            .Where(p => selectedPayments.Contains(p.PayID))
                            .Sum(p => (int)p.PaidAmount);

                        // Update audit fields
                        int userId = Session["ID"] != null
                            ? Convert.ToInt32(Session["ID"])
                            : throw new InvalidOperationException("User session expired.");
                        existingCertificate.CreatedBy = userId;
                        existingCertificate.CreatedDate = existingCertificate.CreatedDate == null
                            ? DateTime.Now
                            : existingCertificate.CreatedDate; // Preserve original if already set

                        // Save changes
                        db.Entry(existingCertificate).State = EntityState.Modified;
                        db.SaveChanges();

                        // Log action
                        var auditLogsController = new AuditLogsController();
                        auditLogsController.RecordLog("Edit", existingCertificate.CertID, "Certificate", existingCertificate.CreatedBy, Session["BranchName"]?.ToString());

                        transaction.Commit();
                        return RedirectToAction("Index");
                    }
                    catch (DbUpdateException ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"DbUpdateException: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                        }
                        TempData["ErrorMessage"] = "An error occurred while saving the certificate. Please check the data and try again.";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Debug.WriteLine($"Error editing certificate: {ex.Message}");
                        TempData["ErrorMessage"] = "An unexpected error occurred while editing the certificate.";
                    }
                }
            }


            return View(certificate);
        }    // GET: Certificates/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Certificate certificate = db.Certificates.Find(id);
            if (certificate == null)
            {
                return HttpNotFound();
            }
            return View(certificate);
        }

        // POST: Certificates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Certificate certificate = db.Certificates.Find(id);
            db.Certificates.Remove(certificate);
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
        // GET: Certificates/Revoke/5
        // [Authorize(Roles = "Admin, CertAuthorizer")]
        public ActionResult Revoke(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Invalid certificate ID.";
                return RedirectToAction("Index");
            }

            Certificate certificate = db.Certificates.Find(id);
            if (certificate == null)
            {
                TempData["ErrorMessage"] = "Certificate not found.";
                return RedirectToAction("Index");
            }

            if (certificate.CertAuthorizationStatus == "Revoked")
            {
                TempData["ErrorMessage"] = "This certificate is already revoked.";
                return RedirectToAction("Index");
            }

            if (certificate.CertAuthorizationStatus != "Approved")
            {
                TempData["ErrorMessage"] = "Only approved certificates can be revoked.";
                return RedirectToAction("Index");
            }

            return View(certificate);
        }

        // POST: Certificates/Revoke/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        //  [Authorize(Roles = "Admin, CertAuthorizer")]
        public ActionResult Revoke(int CertID, string RevokeRemark)
        {
            Certificate certificate = db.Certificates.Find(CertID);
            var splits = new List<Certificate>();
            Certificate parent = null;
            if (certificate.ParentCertId != null)
            {
                parent = db.Certificates
                         .Where(c => c.CertID == certificate.ParentCertId)
                         .FirstOrDefault();
                splits = db.Certificates
                     .Where(c => c.ParentCertId == parent.CertID)
                     .ToList();
            }
            if (certificate.SplitStatus == 1)
            {
                splits = db.Certificates
                     .Where(c => c.ParentCertId == certificate.CertID)
                     .ToList();
            }

            if (certificate == null)
            {
                TempData["ErrorMessage"] = "Certificate not found.";
                return RedirectToAction("Index");
            }

            if (certificate.CertAuthorizationStatus == "Revoked")
            {
                TempData["ErrorMessage"] = "This certificate is already revoked.";
                return View(certificate);
            }

            if (certificate.CertAuthorizationStatus != "Approved")
            {
                TempData["ErrorMessage"] = "Only approved certificates can be revoked.";
                return View(certificate);
            }

            try
            {
                int userId = Session["ID"] != null ? Convert.ToInt32(Session["ID"]) : throw new InvalidOperationException("User session expired.");

                using (var transaction = db.Database.BeginTransaction())
                {
                    // Update certificate
                    // certificate.CertAuthorizationStatus = "Revoked";
                    certificate.RevokedBy = userId;
                    certificate.RevokedDate = DateTime.Now;
                    certificate.RevokeReason = RevokeRemark;
                    certificate.RevokeStatus = "Revoked";
                    certificate.CertAuthorizationStatus = "Revoked";

                    if (parent != null)
                    {
                        parent.RevokedDate = DateTime.Now;
                        parent.RevokeReason = RevokeRemark;
                        parent.RevokeStatus = "Revoked";
                        certificate.CertAuthorizationStatus = "Revoked";
                        db.Entry(parent).State = EntityState.Modified;
                    }
                    if (splits != null)
                    {
                        foreach (var split in splits)
                        {
                            split.RevokedDate = DateTime.Now;
                            split.RevokeReason = RevokeRemark;
                            split.RevokeStatus = "Revoked";
                            certificate.CertAuthorizationStatus = "Revoked";
                            db.Entry(split).State = EntityState.Modified;
                        }
                    }


                    db.Entry(certificate).State = EntityState.Modified;

                    db.SaveChanges();

                    // Log the revocation
                    AuditLogsController auditLogsController = new AuditLogsController();

                    auditLogsController.RecordLog("Revocation", certificate.CertID, "Certificate", certificate.RevokedBy.Value, Session["BranchName"]?.ToString());


                    transaction.Commit();
                }

                TempData["SuccessMessage"] = $"Certificate #{certificate.CertNum} revoked successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error revoking certificate: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while revoking the certificate.";
                return View(certificate);
            }
        }


        public ActionResult Authorize(int? id, string action)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Certificate certificate = db.Certificates.Find(id);
            if (certificate == null)
            {
                return HttpNotFound();
            }
            return View(certificate);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Authorize(int id, string action)
        {
            Certificate certificate = db.Certificates.Find(id);
            if (certificate == null)
            {
                return HttpNotFound();
            }

            int userId = Convert.ToInt32(Session["ID"]);
            if (action == "approve")
            {

                int lastEndingSerial = db.Certificates.Any() ? db.Certificates.Max(c => c.EndingSerial).GetValueOrDefault() : 0;
                certificate.BeginingSerial = lastEndingSerial + 1;
                int numberOfShares = (int)(certificate.TotalPaidupAmount / 100);
                certificate.EndingSerial = certificate.BeginingSerial + numberOfShares - 1;

                int lastCertNum = db.Certificates
                                    .Where(c => c.CertNum != null)
                                    .ToList()
                                    .Select(c => int.Parse(c.CertNum))
                                    .DefaultIfEmpty(0)
                                    .Max();

                var newCertNum = (lastCertNum + 1).ToString();
                certificate.CertNum = newCertNum;
                certificate.CertAuthorizationStatus = "Approved";
                certificate.CertAuthorizer = userId;
                // Record approval log
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("Approval", certificate.CertID, "Certificate", certificate.CreatedBy, Session["BranchName"]?.ToString());

            }
            else if (action == "reject")
            {
                certificate.CertAuthorizationStatus = "Rejected";
                certificate.CertAuthorizer = userId;

                db.Entry(certificate).State = EntityState.Modified;
                db.SaveChanges();

                // Record approval log
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("Approval", certificate.CertID, "Certificate", certificate.CreatedBy, Session["BranchName"]?.ToString());

                //string branchName = db.Branches.FirstOrDefault(b => b.ID == payment.Branch)?.BranchName ?? "Unknown Branch";

                // Call RecordLog method with null-safe value for CreatedBy
                //AuditLogsController auditLogsController = new AuditLogsController();
                //auditLogsController.RecordLog("Rejection", certificate.CertID, "Payment", certificate.CreatedBy, certificate.User.Branch);
                // certificate.CertAuthorizationStatus = "Rejected";
            }


            db.SaveChanges();

            return RedirectToAction("FilterPending");
        }
    }
}
