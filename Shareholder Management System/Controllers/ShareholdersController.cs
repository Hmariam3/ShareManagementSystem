using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Shareholder_Management_System.Models;
using Newtonsoft.Json; // For JsonConvert



namespace Shareholder_Management_System.Controllers
{
    public class ShareholdersController : BaseController
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        // GET: Shareholders
        public ActionResult Index(int? shId)
        {
            Shareholder shareholder = null;

            if (!shId.HasValue)
            {
                // No shareholder selected; show SweetAlert in view
                return View(new Shareholder());
            }

            if (shId.HasValue)
            {
                // Find shareholder by ShID
                shareholder = db.Shareholders
                    .Include(s => s.Branch1)
                    .Include(s => s.User)
                    .Include(s => s.User1)
                    .Include(s => s.ShareTransfers)
                    .Include(s => s.ShareTransfers1)
                    .FirstOrDefault(s => s.ShID == shId.Value);
            }

            if (shareholder == null)
            {
                // Shareholder not found; show SweetAlert in view
                ViewBag.ErrorMessage = "No such shareholder found.";
                return View(new Shareholder());
            }

            // Set ViewBag for pre-selecting dropdown and shareID input
            ViewBag.SelectedShId = shareholder.ShID;
            ViewBag.SelectedShName = shareholder.FullNameEng + " (" + shareholder.ShareID + ")";
            ViewBag.ShareID = shareholder.ShareID;

            // Merge transfer history
            var mergedTransfers = shareholder.ShareTransfers
                .Select(st => new
                {
                    TransferID = st.TransferID,
                    Transferror = st.Shareholder?.FullNameEng ?? "N/A",
                    Transferee = st.Shareholder1?.FullNameEng ?? "N/A",
                    TransferReason = st.TransferReason,
                    NumSharesTransferred = st.NumSharesTransferred,
                    AmountPerShare = st.AmountPerShare,
                    PaidAmountForTransfer = st.PaidAmountForTransfer,
                    DividenedFor = st.DividenedFor,
                    Status = st.TransferAuthorizationStatus,
                    TransferDate = st.TransferDate.HasValue ? st.TransferDate.Value.ToString("yyyy-MM-dd") : "",
                    CreatedBy = st.User?.FullName ?? "N/A"
                })
                .Union(shareholder.ShareTransfers1
                    .Select(st1 => new
                    {
                        TransferID = st1.TransferID,
                        Transferror = st1.Shareholder?.FullNameEng ?? "N/A",
                        Transferee = st1.Shareholder1?.FullNameEng ?? "N/A",
                        TransferReason = st1.TransferReason,
                        NumSharesTransferred = st1.NumSharesTransferred,
                        AmountPerShare = st1.AmountPerShare,
                        PaidAmountForTransfer = st1.PaidAmountForTransfer,
                        DividenedFor = st1.DividenedFor,
                        Status = st1.TransferAuthorizationStatus,
                        TransferDate = st1.TransferDate.HasValue ? st1.TransferDate.Value.ToString("yyyy-MM-dd") : "",
                        CreatedBy = st1.User?.FullName ?? "N/A"
                    }))
                .ToList();

            ViewBag.MergedTransfers = mergedTransfers;

            return View(shareholder);
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

        // GET: Shareholders/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Shareholder shareholder = db.Shareholders.Find(id);
            if (shareholder == null)
            {
                return HttpNotFound();
            }

            var mergedTransfers = shareholder.ShareTransfers
                                    .Select(st => new
                                    {
                                        TransferID = st.TransferID,
                                        Transferror = st.Shareholder?.FullNameEng,
                                        Transferee = st.Shareholder1?.FullNameEng,
                                        TransferReason = st.TransferReason,
                                        NumSharesTransferred = st.NumSharesTransferred,
                                        AmountPerShare = st.AmountPerShare,
                                        PaidAmountForTransfer = st.PaidAmountForTransfer,
                                        DividenedFor = st.DividenedFor,
                                        Status = st.TransferAuthorizationStatus,
                                        TransferDate = st.TransferDate.HasValue ? st.TransferDate.Value.ToString("yyyy-MM-dd") : "",
                                        CreatedBy = st.User?.FullName ?? "N/A"
                                    })
                                    .Union(shareholder.ShareTransfers1
                                        .Select(st1 => new
                                        {
                                            TransferID = st1.TransferID,
                                            Transferror = st1.Shareholder?.FullNameEng,
                                            Transferee = st1.Shareholder1?.FullNameEng,
                                            TransferReason = st1.TransferReason,
                                            NumSharesTransferred = st1.NumSharesTransferred,
                                            AmountPerShare = st1.AmountPerShare,
                                            PaidAmountForTransfer = st1.PaidAmountForTransfer,
                                            DividenedFor = st1.DividenedFor,
                                            Status = st1.TransferAuthorizationStatus,
                                            TransferDate = st1.TransferDate.HasValue ? st1.TransferDate.Value.ToString("yyyy-MM-dd") : "",
                                            CreatedBy = st1.User?.FullName ?? "N/A"
                                        }))
                                    .ToList();

            ViewBag.MergedTransfers = mergedTransfers;

            return View(shareholder);
        }


        // GET: Shareholders/Create
        public ActionResult Create()
        {
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode");
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");
            ViewBag.Authorizer = new SelectList(db.Users, "UID", "FullName");
            ViewBag.ShCategories = new SelectList(db.ShCategories, "ShCategories", "ShCategories");


            var nationalities = db.Nationalities.ToList();


            ViewBag.Nationality = new SelectList(nationalities, "Nationality1", "Nationality1", "Ethiopian"); // "ETH" is the alpha_3_code for Ethiopia
            // Generate the next Shareholder ID
            ViewBag.AutoGeneratedID = GenerateNextShareholderID();

            return View();
        }
        private string GenerateNextShareholderID()
        {
            var lastShareholder = db.Shareholders
                .OrderByDescending(s => s.ShID)
                .FirstOrDefault();

            string lastID = lastShareholder?.ShareID;

            if (string.IsNullOrEmpty(lastID) || lastID.Length != 7 || !lastID.All(char.IsDigit))
            {
                return "0000001";
            }

            if (int.TryParse(lastID, out int number))
            {
                number++;
                return number.ToString("D7");

            }

            return "0000001";
        }

        [HttpPost]

        public ActionResult Create(Shareholder shareholder, HttpPostedFileBase shFile1, HttpPostedFileBase shFile)
        {
            int userId = Convert.ToInt32(Session["ID"]);
            int branchId = Convert.ToInt32(Session["Branch"]);

            TrimShareholderStrings(shareholder); 

            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", shareholder.Branch);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", shareholder.CreatedBy);
            ViewBag.Authorizer = new SelectList(db.Users, "UID", "FullName", shareholder.Authorizer);
            ViewBag.ShCategories = new SelectList(db.ShCategories, "ShCategories", "ShCategories", shareholder.SHCategory);
            // Generate the next Shareholder ID
            ViewBag.AutoGeneratedID = GenerateNextShareholderID();
            ViewBag.Nationality = new SelectList(db.Nationalities, "Nationality1", "Nationality1", "Ethiopian"); // "ETH" is the alpha_3_code for Ethiopia


            //Ensure both files are provided
            if (shFile == null || shFile1 == null)
            {
                //ModelState.AddModelError("", "Both Identification and Agreement documents are required.");
                TempData["ErrorMessage"] = "Both Identification and Agreement documents are required.";

                return View(shareholder);
            }

            if (ModelState.IsValid)
            {
                // Calculate Age from Birthdate
                if (shareholder.BirthDate.HasValue) // Ensure Birthdate is provided
                {
                    DateTime birthdate = shareholder.BirthDate.Value;
                    int age = CalculateAge(birthdate);
                    shareholder.Age = age; // Assign calculated age to the Shareholder object
                }
                else
                {
                    shareholder.Age = 0;
                }
                // Basic shareholder setup
                shareholder.ShareID = GenerateNextShareholderID();
                shareholder.Branch = branchId;
                shareholder.CreatedBy = userId;
                shareholder.Status = "New";
                shareholder.CreatedDate = DateTime.Now;
                shareholder.AuthorizationStatus = "Pending";


                // Document handling
                if (shFile.ContentLength > 0 && shFile1.ContentLength > 0)
                {
                    try
                    {

                        // 50 MB max size per file
                        const int MaxContentLength = 2 * 1024 * 1024; // 50 MB in bytes

                        if (shFile.ContentLength > MaxContentLength || shFile1.ContentLength > MaxContentLength)
                        {
                            TempData["ErrorMessage"] = "The file must not exceed  2MB.";
                            return View(shareholder);
                        }

                        var isShareIDTaken = db.Shareholders.Any(s => s.ShareID == shareholder.ShareID);
                        if (isShareIDTaken)
                        {
                            TempData["ErrorMessage"] = "Shareholder ID is already taken, so please check it again.";
                            return View(shareholder);
                        }




                        db.Shareholders.Add(shareholder);
                        db.SaveChanges();

                        AuditLogsController auditLogsController = new AuditLogsController();
                        auditLogsController.RecordLog("Registration of Shareholder", shareholder.ShID, "Shareholder", shareholder.CreatedBy ?? 0, Session["BranchName"].ToString());
                        // First document setup
                        Document document = new Document
                        {
                            DocOwner = "Shareholder",
                            DocType = "ShareholderAgreement",
                            ShID = shareholder.ShID,
                            CreatedBy = userId,
                            DocAuthorizationStatus = "Pending",
                            CreatedDate = DateTime.Now,
                        };

                        // Second document setup
                        Document kebele = new Document
                        {
                            DocOwner = "Shareholder",
                            DocType = "ShareholderID",
                            ShID = shareholder.ShID,
                            CreatedBy = userId,
                            DocAuthorizationStatus = "Pending",
                            CreatedDate = DateTime.Now,
                        };

                        // Instantiate DocumentsController and save files
                        DocumentsController documentsController = new DocumentsController();
                        documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

                        int documentId = documentsController.Create(document, shFile);
                        int kebeleId = documentsController.Create(kebele, shFile1);

                        // Update ShDocument fields
                        shareholder.ShDocument = documentId;
                        shareholder.KebeleID = kebeleId;

                        // Update shareholder with related document IDs
                        db.Entry(shareholder).State = EntityState.Modified;
                        db.SaveChanges();
                        auditLogsController.RecordLog("Registration of Shareholder Document", shareholder.ShID, "Shareholder", shareholder.CreatedBy ?? 0, Session["BranchName"].ToString());

                        return RedirectToAction("Index");
                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = "An error occurred while registering shareholder. Please try again.";
                        System.Diagnostics.Debug.WriteLine("Error: " + ex.Message);
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Files cannot be empty.";
                }
            }
            else
            {
                // Collect all ModelState errors into a single string
                var errorMessages = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                // Combine all error messages into a single string, separated by line breaks
                TempData["ErrorMessage"] = string.Join("<br>", errorMessages);

                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Property: {state.Key} Error: {error.ErrorMessage}");
                    }
                }
            }


            // Re-assign ViewBags if the model state is invalid or error occurs


            return View(shareholder);
        }
        private void TrimShareholderStrings(Shareholder shareholder)
        {
            if (shareholder == null)
                return;

            var stringProperties = typeof(Shareholder)
                .GetProperties()
                .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite);

            foreach (var property in stringProperties)
            {
                var currentValue = (string)property.GetValue(shareholder);

                if (currentValue != null)
                {
                    property.SetValue(shareholder, currentValue.Trim());
                }
            }
        }
        private int CalculateAge(DateTime birthdate)
        {
            int age = DateTime.Now.Year - birthdate.Year;

            // Subtract one year if the birthday hasn't occurred yet this year
            if (DateTime.Now.Month < birthdate.Month ||
                (DateTime.Now.Month == birthdate.Month && DateTime.Now.Day < birthdate.Day))
            {
                age--;
            }

            return age;
        }
        // GET: Shareholders/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Shareholder shareholder = db.Shareholders.Find(id);
            if (shareholder == null)
            {
                return HttpNotFound();
            }
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", shareholder.Branch);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", shareholder.CreatedBy);
            ViewBag.Authorizer = new SelectList(db.Users, "UID", "FullName", shareholder.Authorizer);
            ViewBag.ShCategories = new SelectList(db.ShCategories, "ShCategories", "ShCategories", shareholder.SHCategory);
            var nationalities = db.Nationalities.ToList();


            ViewBag.Nationality = new SelectList(nationalities, "Nationality1", "Nationality1", "Ethiopian"); // "ETH" is the alpha_3_code for Ethiopia

            return View(shareholder);
        }

        // POST: Shareholders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Shareholder shareholder)
        {
            int userId = Convert.ToInt32(Session["ID"]);
            int branchId = Convert.ToInt32(Session["Branch"]);

            TrimShareholderStrings(shareholder);

            if (ModelState.IsValid)
            {
                // Calculate Age from Birthdate
                if (shareholder.BirthDate.HasValue) // Ensure Birthdate is provided
                {
                    DateTime birthdate = shareholder.BirthDate.Value;
                    int age = CalculateAge(birthdate);
                    shareholder.Age = age; // Assign calculated age to the Shareholder object
                }
                else
                {
                    shareholder.Age = 0; 
                }
                shareholder.Branch = branchId;
                shareholder.CreatedBy = userId;
                shareholder.Status = "Updated";
                shareholder.CreatedDate = DateTime.Now;
                shareholder.AuthorizationStatus = "Pending";

                db.Entry(shareholder).State = EntityState.Modified;
                db.Entry(shareholder).Property(x => x.ShDocument).IsModified = false;
                db.Entry(shareholder).Property(x => x.KebeleID).IsModified = false;

                db.SaveChanges();
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("Edit Shareholder Information", shareholder.ShID, "Shareholder", shareholder.CreatedBy ?? 0, Session["BranchName"].ToString());
                return RedirectToAction("Index");
            }
            else
            {
                // Collect all ModelState errors into a single string
                var errorMessages = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                // Combine all error messages into a single string, separated by line breaks
                TempData["ErrorMessage"] = string.Join("<br>", errorMessages);


            }
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", shareholder.Branch);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", shareholder.CreatedBy);
            ViewBag.Authorizer = new SelectList(db.Users, "UID", "FullName", shareholder.Authorizer);
            ViewBag.ShCategories = new SelectList(db.ShCategories, "ShCategories", "ShCategories", shareholder.SHCategory);

            return View(shareholder);
        }


        [HttpPost]
        public ActionResult UpdateShDocument(int shID, string type, string reason, HttpPostedFileBase shFile)
        {
            int userId = Convert.ToInt32(Session["ID"]);
            int branchId = Convert.ToInt32(Session["Branch"]);
            type = type.Trim();
            reason = reason.Trim();

            if (shFile == null || shFile.ContentLength == 0)
            {
                return Json(new { success = false, message = "File is required." });
            }

            var shareholder = db.Shareholders.Find(shID);
            if (shareholder == null)
            {
                return Json(new { success = false, message = "Shareholder not found." });
            }

            // Handle document creation and file upload logic
            Document document = new Document
            {
                DocOwner = "Shareholder",
                DocType = type,
                ShID = shareholder.ShID,
                CreatedBy = userId,
                DocAuthorizationStatus = "Pending",
                CreatedDate = DateTime.Now,
            };

            DocumentsController documentsController = new DocumentsController();
            documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

            int documentId = documentsController.Create(document, shFile);
            // Update the ShDocument field of the shareholder with the documentId
            shareholder.PendingDoc = documentId;

            shareholder.Branch = branchId;
            shareholder.CreatedBy = userId;
            shareholder.Status = "Document-Updated";
            shareholder.CreatedDate = DateTime.Now;
            shareholder.AuthorizationStatus = "Pending";
            shareholder.Remark = reason;

            // Update the proxy record in the database
            db.Entry(shareholder).State = EntityState.Modified;
            db.SaveChanges();
            AuditLogsController auditLogsController = new AuditLogsController();
            auditLogsController.RecordLog("Edit Shareholder Document", shareholder.ShID, "Shareholder", shareholder.CreatedBy ?? 0, Session["BranchName"].ToString());
            return Json(new { success = true });
        }



        [HttpPost]
        public ActionResult BlockSh(int shID, string reason, HttpPostedFileBase shFile)
        {
            int userId = Convert.ToInt32(Session["ID"]);
            int branchId = Convert.ToInt32(Session["Branch"]);
            reason = reason.Trim();

            if (shFile == null || shFile.ContentLength == 0)
            {
                return Json(new { success = false, message = "File is required." });
            }

            var shareholder = db.Shareholders.Find(shID);
            if (shareholder == null)
            {
                return Json(new { success = false, message = "Shareholder not found." });
            }

            // Handle document creation and file upload logic
            Document document = new Document
            {
                DocOwner = "Shareholder",
                DocType = "ShBlockLetter",
                ShID = shareholder.ShID,
                CreatedBy = userId,
                DocAuthorizationStatus = "Pending",
                CreatedDate = DateTime.Now,
            };

            DocumentsController documentsController = new DocumentsController();
            documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

            int documentId = documentsController.Create(document, shFile);

            shareholder.PendingDoc = documentId;
            shareholder.Branch = branchId;
            shareholder.CreatedBy = userId;
            shareholder.Status = "Blocked";
            shareholder.CreatedDate = DateTime.Now;
            shareholder.AuthorizationStatus = "Pending";
            shareholder.Remark = reason;

            // Update the proxy record in the database
            db.Entry(shareholder).State = EntityState.Modified;
            db.Entry(shareholder).Property(x => x.ShDocument).IsModified = false;
            db.Entry(shareholder).Property(x => x.KebeleID).IsModified = false;

            db.SaveChanges();
            AuditLogsController auditLogsController = new AuditLogsController();
            auditLogsController.RecordLog("Block Shareholder", shareholder.ShID, "Shareholder", shareholder.CreatedBy ?? 0, Session["BranchName"].ToString());
            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult UnBlockSh(int shID, string reason, HttpPostedFileBase shFile)
        {
            int userId = Convert.ToInt32(Session["ID"]);
            int branchId = Convert.ToInt32(Session["Branch"]);
            reason = reason.Trim();

            if (shFile == null || shFile.ContentLength == 0)
            {
                return Json(new { success = false, message = "File is required." });
            }

            var shareholder = db.Shareholders.Find(shID);
            if (shareholder == null)
            {
                return Json(new { success = false, message = "Shareholder not found." });
            }

            // Handle document creation and file upload logic
            Document document = new Document
            {
                DocOwner = "Shareholder",
                DocType = "ShUnBlockLetter",
                ShID = shareholder.ShID,
                CreatedBy = userId,
                DocAuthorizationStatus = "Pending",
                CreatedDate = DateTime.Now,
            };

            DocumentsController documentsController = new DocumentsController();
            documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);
            int documentId = documentsController.Create(document, shFile);

            shareholder.PendingDoc = documentId;
            shareholder.Branch = branchId;
            shareholder.CreatedBy = userId;
            shareholder.Status = "UnBlocked";
            shareholder.CreatedDate = DateTime.Now;
            shareholder.AuthorizationStatus = "Pending";
            shareholder.Remark = reason;

            // Update the proxy record in the database
            db.Entry(shareholder).State = EntityState.Modified;
            db.Entry(shareholder).Property(x => x.ShDocument).IsModified = false;

            db.SaveChanges();
            AuditLogsController auditLogsController = new AuditLogsController();
            auditLogsController.RecordLog("UnBlock  Shareholder", shareholder.ShID, "Shareholder", shareholder.CreatedBy ?? 0, Session["BranchName"].ToString());

            return Json(new { success = true });
        }




        // GET: Shareholders
        public ActionResult PendingRequests()
        {
            // Assuming a context named 'db' exists for database operations
            var shareholders = db.Shareholders.Where(s => s.AuthorizationStatus == "Pending").ToList();
            var proxies = db.Proxies.Where(p => p.ProxyAuthorizationStatus == "Pending").ToList();
            var subscriptions = db.Subscribtions.Where(sub => sub.SubAuthorizationStatus == "Pending").ToList();
            var payments = db.Payments.Where(pay => pay.PaymentAuthorizationStatus == "Pending").ToList();
            var shareTransfers = db.ShareTransfers.Where(st => st.TransferAuthorizationStatus == "Pending").ToList();
            var blockeds = db.Blockeds.Where(b => b.BlockedAuthorizationStatus == "Pending").ToList();
            var documents = db.Documents.Where(doc => doc.DocAuthorizationStatus == "Pending").ToList();
            var certificates = db.Certificates.Where(c => c.CertAuthorizationStatus == "Pending").ToList();

            // Creating a new view model instance and populating it with the pending entities
            var viewModel = new ApprovalsViewModel
            {
                Shareholders = shareholders,
                Proxies = proxies,
                Subscribtions = subscriptions,
                Payments = payments,
                ShareTransfers = shareTransfers,
                Blockeds = blockeds,
                Documents = documents,
                Certificates = certificates
            };

            return View(viewModel);
        }

        public ActionResult Authorization(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Shareholder shareholder = db.Shareholders.Find(id);
            if (shareholder == null)
            {
                return HttpNotFound();
            }
            return View(shareholder);
        }


        [HttpPost]
        public ActionResult Approve(int id)
        {
            var shareholder = db.Shareholders.Find(id);
            if (shareholder != null)
            {
                int userId = Convert.ToInt32(Session["ID"]);

                // Approve the shareholder if it's in "New" status
                if (shareholder.Status.Equals("New"))
                {
                    int docID = shareholder.ShDocument ?? 0;
                    int kebeleID = shareholder.KebeleID ?? 0;

                    // Find and approve the documents associated with the shareholder
                    var document = db.Documents.Find(docID);
                    var kebeleDocument = db.Documents.Find(kebeleID);

                    if (document != null)
                    {
                        document.DocAuthorizationStatus = "Approved";
                        document.DocAuthorizer = userId;
                        document.DocAuthorizationDate = DateTime.Now;
                        db.Entry(document).State = EntityState.Modified;
                    }

                    if (kebeleDocument != null)
                    {
                        kebeleDocument.DocAuthorizationStatus = "Approved";
                        kebeleDocument.DocAuthorizer = userId;
                        kebeleDocument.DocAuthorizationDate = DateTime.Now;
                        db.Entry(kebeleDocument).State = EntityState.Modified;
                    }
                }
                else if (shareholder.Status.Equals("Document-Updated"))
                {
                    int docID = shareholder.PendingDoc ?? 0;

                    // Find and approve the documents associated with the shareholder
                    var document = db.Documents.Find(docID);

                    if (document != null)
                    {
                        document.DocAuthorizationStatus = "Approved";
                        document.DocAuthorizer = userId;
                        document.DocAuthorizationDate = DateTime.Now;
                        db.Entry(document).State = EntityState.Modified;
                    }
                    if (document.DocType.Equals("ShareholderID"))
                    {
                        shareholder.KebeleID = docID;
                    }
                    else
                    {
                        shareholder.ShDocument = docID;
                    }
                    shareholder.PendingDoc = null;
                }
                else if (shareholder.Status.Equals("Updated"))
                {
                    int docID = shareholder.ShDocument ?? 0;
                    int kebelID = shareholder.KebeleID ?? 0;

                    // Find and approve the documents associated with the shareholder
                    var document = db.Documents.Find(docID);
                    var IDdocument = db.Documents.Find(kebelID);

                    if (document != null && document.DocAuthorizationStatus.Equals("Rejected"))
                    {
                        document.DocAuthorizationStatus = "Approved";
                        document.DocAuthorizer = userId;
                        document.DocAuthorizationDate = DateTime.Now;
                        db.Entry(document).State = EntityState.Modified;
                    }

                    if (IDdocument != null && IDdocument.DocAuthorizationStatus.Equals("Rejected"))
                    {
                        IDdocument.DocAuthorizationStatus = "Approved";
                        IDdocument.DocAuthorizer = userId;
                        IDdocument.DocAuthorizationDate = DateTime.Now;
                        db.Entry(document).State = EntityState.Modified;
                    }

                }
                else
                {
                    int docID = shareholder.PendingDoc ?? 0;

                    // Find and approve the documents associated with the shareholder
                    var document = db.Documents.Find(docID);

                    if (document != null)
                    {
                        document.DocAuthorizationStatus = "Approved";
                        document.DocAuthorizer = userId;
                        document.DocAuthorizationDate = DateTime.Now;
                        db.Entry(document).State = EntityState.Modified;
                    }
                }
                if (shareholder.Status.Equals("Blocked"))
                {
                    db.Entry(shareholder).Property(x => x.Status).IsModified = false;
                }
                else
                {
                    shareholder.Status = "Active";
                }
                // Update shareholder status to Active and Approved
                shareholder.AuthorizationStatus = "Approved";
                shareholder.Authorizer = userId;
                shareholder.AuthorizedDate = DateTime.Now;

                db.Entry(shareholder).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { success = true });

            }
            return Json(new { success = false });
        }

        [HttpPost]
        public ActionResult Reject(int id, string remark)
        {
            remark = remark.Trim();

            var shareholder = db.Shareholders.Find(id);
            if (shareholder != null)
            {
                int userId = Convert.ToInt32(Session["ID"]);
                int branchId = Convert.ToInt32(Session["Branch"]);

                if (shareholder.Status.Equals("New"))
                {
                    int docID = shareholder.ShDocument ?? 0;
                    int kebeleID = shareholder.KebeleID ?? 0;

                    // Find and approve the documents associated with the shareholder
                    var document = db.Documents.Find(docID);
                    var kebeleDocument = db.Documents.Find(kebeleID);

                    if (document != null)
                    {
                        document.DocAuthorizationStatus = "Rejected";
                        document.DocAuthorizer = userId;
                        document.DocAuthorizationDate = DateTime.Now;
                        db.Entry(document).State = EntityState.Modified;
                    }

                    if (kebeleDocument != null)
                    {
                        kebeleDocument.DocAuthorizationStatus = "Rejected";
                        kebeleDocument.DocAuthorizer = userId;
                        kebeleDocument.DocAuthorizationDate = DateTime.Now;
                        db.Entry(kebeleDocument).State = EntityState.Modified;
                    }
                }
                else
                {
                    int docID = shareholder.PendingDoc ?? 0;

                    // Find and approve the documents associated with the shareholder
                    var document = db.Documents.Find(docID);

                    if (document != null)
                    {
                        document.DocAuthorizationStatus = "Rejected";
                        document.DocAuthorizer = userId;
                        document.DocAuthorizationDate = DateTime.Now;
                        db.Entry(document).State = EntityState.Modified;
                    }
                }

                if (shareholder.Status.Equals("Blocked"))
                {
                    shareholder.Status = "Active";
                }
                else if (shareholder.Status.Equals("UnBlocked"))
                {
                    shareholder.Status = "Blocked";
                }
                shareholder.Authorizer = userId;
                shareholder.AuthorizedDate = DateTime.Now;
                shareholder.AuthorizationStatus = "Rejected";
                shareholder.Remark = remark;

                db.Entry(shareholder).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        public ActionResult GetPendingRequestsCount()
        {
            var pendingShareholders = db.Shareholders.Count(s => s.AuthorizationStatus == "Pending");
            var pendingProxies = db.Proxies.Count(p => p.ProxyAuthorizationStatus == "Pending");
            var pendingSubscriptions = db.Subscribtions.Count(sub => sub.SubAuthorizationStatus == "Pending");
            var pendingPayments = db.Payments.Count(pay => pay.PaymentAuthorizationStatus == "Pending");
            var pendingShareTransfers = db.ShareTransfers.Count(st => st.TransferAuthorizationStatus == "Pending");
            var pendingBlockeds = db.Blockeds.Count(b => b.BlockedAuthorizationStatus == "Pending");
            var pendingCertificates = db.Certificates.Count(c => c.CertAuthorizationStatus == "Pending");

            int totalPendingRequests = pendingShareholders + pendingProxies + pendingSubscriptions + pendingPayments + pendingShareTransfers + pendingBlockeds + pendingCertificates;

            return PartialView("_PendingRequestsCount", totalPendingRequests);
        }

        // GET: Shareholders/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Shareholder shareholder = db.Shareholders.Find(id);
            if (shareholder == null)
            {
                return HttpNotFound();
            }
            return View(shareholder);
        }

        // POST: Shareholders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Shareholder shareholder = db.Shareholders.Find(id);
            db.Shareholders.Remove(shareholder);
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
