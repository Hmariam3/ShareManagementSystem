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
    public class ProxiesController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        // GET: Proxies
        public ActionResult Index(string ShID = null, string shareID = null)
        {
            var shareholder = new Shareholder();
            IEnumerable<Proxy> proxies = new List<Proxy>();
            var shareholders = db.Shareholders.OrderBy(s => s.FullNameEng).Select(s => new SelectListItem
            {
                Value = s.ShID.ToString(), // ShID as value
                Text = s.FullNameEng // FullNameEng as text
            }).ToList();
            shareholders.Insert(0, new SelectListItem
            {
                Value = "", // Null value for the default option
                Text = "Select a Shareholder" // Text for the default option
            });

            ViewBag.Shareholders = shareholders;
            ViewBag.ShareID = "";
            var nationalities = db.Nationalities.ToList();


            ViewBag.Nationality = new SelectList(nationalities, "Nationality1", "Nationality1", "Ethiopian"); // "ETH" is the alpha_3_code for Ethiopia

            if (String.IsNullOrEmpty(ShID) && String.IsNullOrEmpty(shareID))
            {
                proxies = db.Proxies.Include(p => p.User).Include(p => p.User1).Include(p => p.Shareholder).Where(s => s.ShID == 1);
                // Pass the list of shareholders to ViewBag
            }
            else
            {

                if (!string.IsNullOrEmpty(ShID))
                {
                    int shID = int.Parse(ShID);

                    shareholder = db.Shareholders
                               .Where(s => s.ShID == shID).FirstOrDefault();
                    // Fetch proxies by ShID (Full Name or Dropdown Selected)
                    proxies = db.Proxies.Where(p => p.ShID == shID).ToList();
                }
                else if (!string.IsNullOrEmpty(shareID))
                {
                    // Fetch proxies by Shareholder ID entered in textbox
                    shareholder = db.Shareholders
                                .Where(s => s.ShareID == shareID).FirstOrDefault();
                    proxies = db.Proxies.Where(p => p.Shareholder.ShareID == shareID).ToList();

                }

                // Return the Partial View with fetched proxies
                ViewBag.ShareID = shareholder.ShareID;
                ViewBag.Shareholder = shareholder;
            }
            ViewBag.NewProxy = new Proxy();


            return View(proxies);
        }

        [HttpPost]
        public ActionResult UpdateProxyDocument(int proxyID, string type, string reason, HttpPostedFileBase proxyFile)
        {
            int userId = Convert.ToInt32(Session["ID"]);
            int branchId = Convert.ToInt32(Session["Branch"]);
            var proxy = db.Proxies.Find(proxyID);

            if (proxyFile == null || proxyFile.ContentLength == 0)
            {
                return Json(new { success = false, message = "File is required." });
            }
            var activeProxy = db.Proxies.Where(p => p.ProxyAuthorizationStatus == "Approved" && p.ProxyStatus == "Active" && p.ProxyID != proxyID && p.ShID != proxy.ShID).FirstOrDefault();
            if (activeProxy != null)
            {
                return Json(new { success = false, message = "There is another active proxy so frist please inactivate that proxyy." });
            }
            if (proxy == null)
            {
                return Json(new { success = false, message = "Proxy not found." });
            }

            // Handle document creation and file upload logic
            Document document = new Document
            {
                DocOwner = "Proxy",
                DocType = type,
                ShID = proxy.ShID,
                ProxyID = proxy.ProxyID,
                CreatedBy = userId,  // Assuming 1 is the user creating it, update as per your logic
                DocAuthorizationStatus = "Pending",
                CreatedDate = DateTime.Now,
            };

            DocumentsController documentsController = new DocumentsController();
            documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

            int documentId = documentsController.Create(document, proxyFile);

            proxy.PendingDoc = documentId;
            proxy.CreatedDate = DateTime.Now;
            proxy.ProxyAuthorizationStatus = "Pending";
            proxy.ProxyStatus = "Document-Updated";
            proxy.Remark = reason;
            // Update the proxy record in the database
            db.Entry(proxy).State = EntityState.Modified;
            db.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult ActivateProxy(int proxyID, string reason, HttpPostedFileBase proxyFile)
        {
            int userId = Convert.ToInt32(Session["ID"]);
            int branchId = Convert.ToInt32(Session["Branch"]);

            if (proxyFile == null || proxyFile.ContentLength == 0)
            {
                return Json(new { success = false, message = "File is required." });
            }

            // Find the proxy to activate
            var proxy = db.Proxies.Find(proxyID);
            if (proxy == null)
            {
                return Json(new { success = false, message = "Proxy not found." });
            }

            // Check for other active proxies for the same shareholder
            var activeProxy = db.Proxies
                .FirstOrDefault(p =>
                    p.ProxyAuthorizationStatus == "Approved" &&
                    p.ProxyStatus == "Active" &&
                    p.ProxyID != proxyID &&
                    p.ShID == proxy.ShID);

            if (activeProxy != null)
            {
                return Json(new { success = false, message = "Another active proxy exists. Please deactivate it first." });
            }
            // Handle document creation and file upload logic
            Document document = new Document
            {
                DocOwner = "Proxy",
                DocType = "DeligationLetter",
                ShID = proxy.ShID,
                ProxyID = proxy.ProxyID,
                CreatedBy = userId,  // Assuming 1 is the user creating it, update as per your logic
                DocAuthorizationStatus = "Pending",
                CreatedDate = DateTime.Now,
            };

            DocumentsController documentsController = new DocumentsController();
            documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

            int documentId = documentsController.Create(document, proxyFile);
            proxy.PendingDoc = documentId;
            proxy.CreatedDate = DateTime.Now;
            proxy.ProxyAuthorizationStatus = "Pending";
            proxy.ProxyStatus = "Proxy-ReActivated";
            proxy.Remark = reason;
            // Update the proxy record in the database
            db.Entry(proxy).State = EntityState.Modified;
            db.SaveChanges();
            AuditLogsController auditLogsController = new AuditLogsController();
            auditLogsController.RecordLog("Activate  Shareholder Proxy", proxy.ProxyID, "Proxy", proxy.CreatedBy ?? 0, Session["BranchName"].ToString());
            return Json(new { success = true });
        }


        [HttpPost]
        public ActionResult DeactivateProxy(int proxyID)
        {
            int userId = Convert.ToInt32(Session["ID"]);
            int branchId = Convert.ToInt32(Session["Branch"]);

            var proxy = db.Proxies.Find(proxyID);
            if (proxy != null)
            {
                proxy.ProxyStatus = "InActive"; // Mark as inactive
                proxy.CreatedBy = userId;
                proxy.ProxyAuthorizationStatus = "Pending";
                db.Entry(proxy).Property(x => x.ProxyDocument).IsModified = false;

                db.SaveChanges();
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("Deactivate  Shareholder Proxy", proxy.ProxyID, "Proxy", proxy.CreatedBy ?? 0, Session["BranchName"].ToString());
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Proxy not found" });
        }

        // GET: Proxies/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Proxy proxy = db.Proxies.Find(id);
            if (proxy == null)
            {
                return HttpNotFound();
            }
            return View(proxy);
        }

        // GET: Proxies/Create
        public ActionResult Create()
        {
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");
            ViewBag.ProxyAuthorizer = new SelectList(db.Users, "UID", "FullName");
            ViewBag.ShID = new SelectList(db.Shareholders.OrderBy(s => s.FullNameEng), "ShID", "ShareID");
            var nationalities = db.Nationalities.ToList();
            ViewBag.Nationality = new SelectList(nationalities, "Nationality1", "Nationality1", "Ethiopian"); // "ETH" is the alpha_3_code for Ethiopia
            return View();
        }

        // POST: Proxies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Proxy proxy, HttpPostedFileBase proxyFile, HttpPostedFileBase kebeleID)
        {
            int userId = Convert.ToInt32(Session["ID"]);
            int branchId = Convert.ToInt32(Session["Branch"]);

            if (proxyFile == null || kebeleID == null)
            {

                TempData["ErrorMessage"] = "Both Identification and Agreement documents are required.";
                ViewBag.NewProxy = proxy;
                return RedirectToAction("Index", new { ShID = proxy.ShID });
            }

            if (ModelState.IsValid)
            {
                // Calculate Age from Birthdate
                if (proxy.BirthDate.HasValue) // Ensure Birthdate is provided
                {
                    DateTime birthdate = proxy.BirthDate.Value;
                    int age = CalculateAge(birthdate);
                    proxy.Age = age; // Assign calculated age to the Shareholder object
                }
                else
                {
                    TempData["ErrorMessage"] = "Birthdate is required to calculate Age.";
                    return View(proxy);
                }
                proxy.CreatedBy = userId;
                proxy.ProxyStatus = "New";
                proxy.CreatedDate = DateTime.Now;
                proxy.ProxyAuthorizationStatus = "Pending";
                proxy.Branch = branchId;

                // Handle document creation
                if (proxyFile.ContentLength > 0 && kebeleID.ContentLength > 0)
                {
                    try
                    {
                        db.Proxies.Add(proxy);
                        db.SaveChanges();
                        AuditLogsController auditLogsController = new AuditLogsController();
                        auditLogsController.RecordLog("Registration of  Shareholder Proxy", proxy.ProxyID, "Proxy", proxy.CreatedBy ?? 0, Session["BranchName"].ToString());
                        Document document = new Document
                        {
                            DocOwner = "Proxy",
                            DocType = "DeligationLetter",
                            ShID = proxy.ShID,
                            ProxyID = proxy.ProxyID,
                            CreatedBy = userId,
                            DocAuthorizationStatus = "Pending",
                            CreatedDate = DateTime.Now,
                        };
                        Document kebele = new Document
                        {
                            DocOwner = "Proxy",
                            DocType = "ProxyID",
                            ShID = proxy.ShID,
                            ProxyID = proxy.ProxyID,
                            CreatedBy = userId,
                            DocAuthorizationStatus = "Pending",
                            CreatedDate = DateTime.Now,
                        };

                        DocumentsController documentsController = new DocumentsController();
                        documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

                        int documentId = documentsController.Create(document, proxyFile);
                        int proID = documentsController.Create(kebele, kebeleID);
                        // Update the ShDocument field of the shareholder with the documentId
                        proxy.ProxyDocument = documentId;
                        proxy.KebeleID = proID;

                        //// Update the shareholder record in the database
                        db.Entry(proxy).State = EntityState.Modified;
                        db.SaveChanges();
                       
                        auditLogsController.RecordLog("Registration of Proxy Document", proxy.ProxyID, "Proxy", proxy.CreatedBy ?? 0, Session["BranchName"].ToString());
                        return RedirectToAction("Index", new { ShID = proxy.ShID });

                    }
                    catch (Exception ex)
                    {
                        TempData["ErrorMessage"] = "An error occurred while saving the documents. Please try again.";
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
                // Collect all ModelState errors and store them in TempData
                var errorMessages = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                TempData["ErrorMessage"] = string.Join("<br>", errorMessages);
            }


            ViewBag.NewProxy = proxy;

            return RedirectToAction("Index", new { ShID = proxy.ShID });
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

        [HttpGet]
        public JsonResult GetProxyData(int id)
        {
            var proxy = db.Proxies.Find(id); // Fetch proxy from database

            if (proxy == null)
            {
                return Json(null, JsonRequestBehavior.AllowGet); // Return null if not found
            }

            return Json(new
            {
                ProxyID = proxy.ProxyID,
                ShID = proxy.ShID,
                FullName = proxy.FullName,
                Nationality = proxy.Nationality,
                PhoneNo = proxy.PhoneNo,
                PhoneNo2 = proxy.PhoneNo2,
                StartDate = proxy.StartDate?.ToString("yyyy-MM-dd"),
                EndDate = proxy.EndDate?.ToString("yyyy-MM-dd"),
                Region = proxy.Region,
                Zone = proxy.Zone,
                City = proxy.City,
                Subcity = proxy.Subcity,
                Woreda = proxy.Woreda,
                Kebele = proxy.Kebele
                // Include other fields as necessary
            }, JsonRequestBehavior.AllowGet);
        }


        // GET: Proxies/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Proxy proxy = db.Proxies.Find(id);
            if (proxy == null)
            {
                return HttpNotFound();
            }
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", proxy.CreatedBy);
            ViewBag.ProxyAuthorizer = new SelectList(db.Users, "UID", "FullName", proxy.ProxyAuthorizer);
            ViewBag.ShID = new SelectList(db.Shareholders.OrderBy(s => s.FullNameEng), "ShID", "ShareID", proxy.ShID);
            var nationalities = db.Nationalities.ToList();


            ViewBag.Nationality = new SelectList(nationalities, "Nationality1", "Nationality1", "Ethiopian"); // "ETH" is the alpha_3_code for Ethiopia
            return View(proxy);
        }

        // POST: Proxies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Proxy proxy)
        {
            int userId = Convert.ToInt32(Session["ID"]);
            int branchId = Convert.ToInt32(Session["Branch"]);

            if (ModelState.IsValid)
            {
                // Calculate Age from Birthdate
                if (proxy.BirthDate.HasValue) // Ensure Birthdate is provided
                {
                    DateTime birthdate = proxy.BirthDate.Value;
                    int age = CalculateAge(birthdate);
                    proxy.Age = age; // Assign calculated age to the Shareholder object
                }
                else
                {
                    TempData["ErrorMessage"] = "Birthdate is required to calculate Age.";
                    return View(proxy);
                }
                proxy.CreatedBy = userId;
                proxy.ProxyStatus = "Updated";
                proxy.CreatedDate = DateTime.Now;
                proxy.ProxyAuthorizationStatus = "Pending";

                db.Entry(proxy).State = EntityState.Modified;
                //db.Entry(proxy).Property(x => x.ShID).IsModified = false;
                db.Entry(proxy).Property(x => x.Remark).IsModified = false;
                db.Entry(proxy).Property(x => x.ProxyDocument).IsModified = false;
                db.Entry(proxy).Property(x => x.KebeleID).IsModified = false;

                db.SaveChanges();
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("Edit  Proxy Information", proxy.ProxyID, "Proxy", proxy.CreatedBy ?? 0, Session["BranchName"].ToString());
                return RedirectToAction("Index", new { ShID = proxy.ShID });
            }
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", proxy.CreatedBy);
            ViewBag.ProxyAuthorizer = new SelectList(db.Users, "UID", "FullName", proxy.ProxyAuthorizer);
            ViewBag.ShID = new SelectList(db.Shareholders.OrderBy(s => s.FullNameEng), "ShID", "ShareID", proxy.ShID);
            return View(proxy);
        }

        [HttpPost]
        public ActionResult Approve(int id)
        {
            var proxy = db.Proxies.Find(id);
            var activeProxy = db.Proxies.Where(p => p.ProxyAuthorizationStatus == "Approved" && p.ProxyStatus == "Active" && p.ShID == proxy.ShID).FirstOrDefault();
            if (activeProxy != null && activeProxy.ProxyID != id)
            {
                return Json(new { success = false, message = "There is another active proxy so frist please inactivate that proxyy." });
            }
            if (proxy != null)
            {
                int userId = Convert.ToInt32(Session["ID"]);


                // Approve the shareholder if it's in "New" status
                if (proxy.ProxyStatus.Equals("New"))
                {
                    int docID = proxy.ProxyDocument ?? 0;
                    int kebeleID = proxy.KebeleID ?? 0;

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
                else if (proxy.ProxyStatus.Equals("Updated"))
                {
                    int docID = proxy.ProxyDocument ?? 0;
                    int kebelID = proxy.KebeleID ?? 0;

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
                else if (proxy.ProxyStatus.Equals("Document-Updated"))
                {
                    int docID = proxy.PendingDoc ?? 0;

                    // Find and approve the documents associated with the shareholder
                    var document = db.Documents.Find(docID);

                    if (document != null)
                    {
                        document.DocAuthorizationStatus = "Approved";
                        document.DocAuthorizer = userId;
                        document.DocAuthorizationDate = DateTime.Now;
                        db.Entry(document).State = EntityState.Modified;
                    }
                    if (document.DocType.Equals("ProxyID"))
                    {
                        proxy.KebeleID = docID;
                    }
                    else
                    {
                        proxy.ProxyDocument = docID;
                    }
                    proxy.PendingDoc = null;
                }
                else
                {
                    int docID = proxy.PendingDoc ?? 0;

                    // Find and approve the documents associated with the shareholder
                    var document = db.Documents.Find(docID);

                    if (document != null)
                    {
                        document.DocAuthorizationStatus = "Approved";
                        document.DocAuthorizer = userId;
                        document.DocAuthorizationDate = DateTime.Now;
                        db.Entry(document).State = EntityState.Modified;
                    }
                    proxy.ProxyDocument = docID;

                }

                if (proxy.ProxyStatus.Equals("InActive"))
                {
                    db.Entry(proxy).Property(x => x.ProxyStatus).IsModified = false;
                }
                else
                {
                    proxy.ProxyStatus = "Active";
                }

                proxy.ProxyAuthorizationStatus = "Approved";
                proxy.ProxyAuthorizer = userId;
                proxy.AuthorizedDate = DateTime.Now;

                db.Entry(proxy).State = EntityState.Modified;
                db.SaveChanges();
                return Json(new { success = true });

            }
            return Json(new { success = false });
        }

        [HttpPost]
        public ActionResult Reject(int id, string remark)
        {
            var proxy = db.Proxies.Find(id);
            if (proxy != null)
            {
                int userId = Convert.ToInt32(Session["ID"]);
                int branchId = Convert.ToInt32(Session["Branch"]);

                if (proxy.ProxyStatus.Equals("New"))
                {
                    int docID = proxy.ProxyDocument ?? 0;
                    int kebeleID = proxy.KebeleID ?? 0;

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
                    int docID = proxy.PendingDoc ?? 0;

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

                if (proxy.ProxyStatus.Equals("InActive"))
                {
                    proxy.ProxyStatus = "Active";
                }
                else if (proxy.ProxyStatus.Equals("Active"))
                {
                    proxy.ProxyStatus = "InActive";
                }

                proxy.ProxyAuthorizer = userId;
                proxy.AuthorizedDate = DateTime.Now;
                proxy.ProxyAuthorizationStatus = "Rejected";
                proxy.Remark = remark;

                db.Entry(proxy).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }



        // GET: Proxies/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Proxy proxy = db.Proxies.Find(id);
            if (proxy == null)
            {
                return HttpNotFound();
            }
            return View(proxy);
        }

        // POST: Proxies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Proxy proxy = db.Proxies.Find(id);
            db.Proxies.Remove(proxy);
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
