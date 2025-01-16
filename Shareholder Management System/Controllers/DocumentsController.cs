using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Shareholder_Management_System.Models;

namespace Shareholder_Management_System.Controllers
{
    public class DocumentsController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        public List<Document> GetDocumentsFromDatabase(string ShID, string ShareID, string branch)
        {
            // Initial query with includes for related User and other necessary tables
            IQueryable<Document> query = db.Documents.Include(d => d.Shareholder);  // Assuming 'User' relates to the Shareholder

            // Convert Shareholder ID to int (assuming it is stored as an int)
            int shareholderId;
            if (int.TryParse(ShID, out shareholderId))
            {
                // Filter documents where Document's ShID matches the provided ShID (Shareholder ID)
                query = query.Where(d => d.ShID == shareholderId && d.ShID != null);
            }

            // Further filtering based on ShareID or branch if necessary
            if (!string.IsNullOrEmpty(ShareID))
            {
                // Assuming the ShareID is another identifier you want to use for filtering
                query = query.Where(d => d.Shareholder.ShID.ToString() == ShareID);
            }

            if (!string.IsNullOrEmpty(branch))
            {
                // Filter documents based on branch if provided
                query = query.Where(d => d.Shareholder.Branch.Equals(branch));
            }

            // Return the final filtered list of documents
            return query.ToList();
        }

        // GET: Documents
        public ActionResult Index(string ShID = null, string shareID = null, string branch = null)
        {
            IEnumerable<Document> documents = new List<Document>();

            /*var shareholders = GetDocumentsFromDatabase(ShID, shareID, branch);*/ // Fetch shareholders from a database or service
            var shareholders = db.Documents.Select(s => new SelectListItem
            {
                Value = s.ShID.ToString(), // ShID as value
                Text = s.Shareholder.FullNameEng // FullNameEng as text
            }).ToList();
            shareholders.Insert(0, new SelectListItem
            {
                Value = "", // Null value for the default option
                Text = "Select a Shareholder" // Text for the default option
            });

            ViewBag.Shareholders = shareholders;
            // Ensure model is also set if needed

            if (String.IsNullOrEmpty(ShID) && String.IsNullOrEmpty(shareID))
            {
                documents = db.Documents.Where(s => s.ShID == 199999999);
                // Pass the list of shareholders to ViewBag
            }
            else
            {
                int shID = int.Parse(ShID);
                // Fetch proxies by ShID (Full Name or Dropdown Selected)
                documents = db.Documents.Where(p => p.ShID == shID).ToList();
            }

            return View(documents);

        }

        // GET: Documents/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Document document = db.Documents.Find(id);
            if (document == null)
            {
                return HttpNotFound();
            }
            return View(document);
        }

        // GET: Documents/Create
        public ActionResult Create()
        {
            ViewBag.DocID = new SelectList(db.Documents, "DocID", "DocName");
            ViewBag.DocID = new SelectList(db.Documents, "DocID", "DocName");
            ViewBag.ProxyID = new SelectList(db.Proxies, "ProxyID", "FullName");
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");
            ViewBag.DocAuthorizer = new SelectList(db.Users, "UID", "FullName");
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID");
            return View();
        }

        // POST: Documents/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public int Create(Document document, HttpPostedFileBase uploadedFile, int? transfreeID = null)
        {
            if (ModelState.IsValid)
            {
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    var shareholder = db.Shareholders.Find(document.ShID);
                    var transfer = db.ShareTransfers.Find(document.ShID);
                    var proxies = db.Proxies.Find(document.ProxyID);

                    // Generate a base file name using Shareholder.FullNameEng based on document type
                    string baseFileName;
                    var fileExtension = Path.GetExtension(uploadedFile.FileName);

                    switch (document.DocType)
                    {
                        case "ShareholderAgreement":
                            baseFileName = $"{shareholder.FullNameEng}_ShareholderAgreement";
                            break;
                        case "ShareholderID":
                            baseFileName = $"{shareholder.FullNameEng}_ID";
                            break;
                        case "ShBlockLetter":
                            baseFileName = $"{shareholder.FullNameEng}_ShBlockLetter";
                            break;
                        case "ShUnBlockLetter":
                            baseFileName = $"{shareholder.FullNameEng}_ShUnBlockLetter";
                            break;
                        case "ProxyID":
                            baseFileName = $"{shareholder.FullNameEng}_ProxyID";
                            break;
                        case "DeligationLetter":
                            baseFileName = $"{shareholder.FullNameEng}_DelegationLetter";
                            break;
                        case "Payment Slip":
                            baseFileName = $"{shareholder.FullNameEng}_PaymentSlip";
                            break;
                        case "Blocking Document":
                            baseFileName = $"{shareholder.FullNameEng}_BlockingDocument";
                            break;
                        case "Transfer Document":
                            var shareholder1 = db.Shareholders.Find(transfreeID);
                            baseFileName = $"{shareholder.FullNameEng}_{shareholder1.FullNameEng}_Transfer Document";
                            break;
                        default:
                            throw new Exception("Invalid document type.");
                    }

                    // Define the base folder
                    string baseFolder = Server.MapPath("~/Documents/");

                    // Define the path based on the document type
                    string folderPath = "";
                    switch (document.DocType)
                    {
                        case "ShareholderAgreement":
                            folderPath = Path.Combine(baseFolder, "Shareholder", "Agreement");
                            break;
                        case "ShareholderID":
                            folderPath = Path.Combine(baseFolder, "Shareholder", "ID");
                            break;
                        case "ShBlockLetter":
                            folderPath = Path.Combine(baseFolder, "Shareholder", "ShareholderBlock");
                            break;
                        case "ShUnBlockLetter":
                            folderPath = Path.Combine(baseFolder, "Shareholder", "ShareholderUnBlock");
                            break;
                        case "ProxyID":
                            folderPath = Path.Combine(baseFolder, "Proxy", "ID");
                            break;
                        case "DeligationLetter":
                            folderPath = Path.Combine(baseFolder, "Proxy", "Delegation");
                            break;
                        case "Payment Slip":
                            folderPath = Path.Combine(baseFolder, "Payment", "PaymentSlip");
                            break;
                        case "Blocking Document":
                            folderPath = Path.Combine(baseFolder, "Blocking", "BlockingDocument");
                            break;
                        case "Transfer Document":

                            folderPath = Path.Combine(baseFolder, "Transfer", "TransferDocument");
                            break;
                        default:
                            throw new Exception("Invalid document type.");
                    }

                    // Ensure the folder exists, if not create it
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    // Initialize the final file name
                    string fileName = $"{baseFileName}{fileExtension}";
                    string filePath = Path.Combine(folderPath, fileName);

                    // Check if the file exists and append a number if necessary
                    int counter = 1;
                    while (System.IO.File.Exists(filePath))
                    {
                        fileName = $"{baseFileName}_{counter}{fileExtension}";
                        filePath = Path.Combine(folderPath, fileName);
                        counter++;
                    }

                    // Save the file to the specified path
                    uploadedFile.SaveAs(filePath);

                    // Set the file path and name in the document model
                    document.DocPath = $"~/{folderPath.Replace(Server.MapPath("~/"), "").Replace("\\", "/")}/{fileName}";
                    document.DocName = fileName;

                    document.CreatedDate = DateTime.Now;
                    // Add the document to the database
                    db.Documents.Add(document);
                    db.SaveChanges();

                    // Return the ID of the newly created document
                    return document.DocID;
                }
                else
                {
                    throw new Exception("No file was uploaded.");
                }
            }

            // If the model state is invalid, return -1 to indicate an error
            return -1;
        }


        public JsonResult GetDocumentCounts()
        {
            var docCounts = db.Documents
                .GroupBy(d => d.DocType)
                .Select(g => new { DocType = g.Key, Count = g.Count() })
                .ToList();

            return Json(docCounts, JsonRequestBehavior.AllowGet);
        }
        // GET: Documents
        public ActionResult DocumentList(string docType)
        {
            IEnumerable<Document> documents = new List<Document>();

            documents = db.Documents.Where(d => d.DocType.Equals(docType)).Include(d => d.Proxy).Include(d => d.User).Include(d => d.User1).Include(d => d.Shareholder).ToList();

            return View(documents);

        }
        [HttpGet]
        public JsonResult GetPinnedDocuments()
        {
            try
            {
                // Fetch documents and sort by the number of files (Count)
                var documents = db.Documents
                    .GroupBy(d => d.DocType)
                    .Select(g => new
                    {
                        DocType = g.Key,
                        Count = g.Count(),
                        Documents = g.ToList()
                    })
                    .OrderByDescending(g => g.Count) // Sort by Count in descending order
                    .ToList();

                return Json(documents, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error fetching pinned documents: " + ex.Message);
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        public JsonResult GetDocumentPath(int id)
        {
            // Retrieve the document from the database using the document ID
            var document = db.Documents.FirstOrDefault(d => d.DocID == id);

            if (document == null)
            {
                // If the document does not exist, return an error response
                return Json(new { success = false, message = "Document not found." }, JsonRequestBehavior.AllowGet);
            }

            // Construct the URL path (relative to the web root) to access the file
            string webFilePath = Url.Content(document.DocPath);

            // Return the file path as a URL
            return Json(new { success = true, filePath = webFilePath }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetDocumentStatsByType(string docType)
        {
            // Get documents based on the DocType
            var documents = db.Documents.Where(d => d.DocType == docType);

            var docCount = documents.Count();
            long totalSizeInBytes = 0;

            // Loop through each document and sum up the sizes
            foreach (var doc in documents)
            {
                string filePath = doc.DocPath; // Ensure you have the correct path stored in the DocPath field

                if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                {

                    // Get the size of each file and sum them up
                    FileInfo fileInfo = new FileInfo(filePath);
                    totalSizeInBytes += fileInfo.Length;




                }
                else
                {
                    Console.WriteLine($"File not found or invalid path: {filePath}");
                }
            }

            // Convert total size to megabytes (MB)
            double totalSizeInMB = totalSizeInBytes / (1024.0 * 1024.0);

            return Json(new { Count = docCount, SizeInMB = totalSizeInMB }, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetShareholderDocuments(string doctype)
        {
            try
            {
                var documents = db.Documents
                    .Where(d => d.DocType == doctype) // Filter by the provided doctype
                    .Select(d => new
                    {
                        DocumentId = d.DocID,
                        DocumentName = d.DocName,
                        DocumentType = d.DocType,
                        DocumentPath = d.DocPath,
                        DocumentOwner = d.DocOwner,
                        CreatedBy = d.CreatedBy,
                        CreatedDate = d.CreatedDate,
                        AuthorizationStatus = d.DocAuthorizationStatus,
                        AuthorizationDate = d.DocAuthorizationDate,
                        Authorizer = d.DocAuthorizer
                    })
                    .ToList();

                return Json(documents, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error fetching documents: " + ex.Message);
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }


        // GET: Documents/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Document document = db.Documents.Find(id);
            if (document == null)
            {
                return HttpNotFound();
            }
            ViewBag.DocID = new SelectList(db.Documents, "DocID", "DocName", document.DocID);
            ViewBag.DocID = new SelectList(db.Documents, "DocID", "DocName", document.DocID);
            ViewBag.ProxyID = new SelectList(db.Proxies, "ProxyID", "FullName", document.ProxyID);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", document.CreatedBy);
            ViewBag.DocAuthorizer = new SelectList(db.Users, "UID", "FullName", document.DocAuthorizer);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", document.ShID);
            return View(document);
        }

        // POST: Documents/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "DocID,DocName,DocType,DocPath,DocOwner,ShID,ProxyID,CreatedBy,CreatedDate,DocAuthorizationStatus,DocAuthorizer,DocAuthorizationDate")] Document document)
        {
            if (ModelState.IsValid)
            {
                db.Entry(document).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.DocID = new SelectList(db.Documents, "DocID", "DocName", document.DocID);
            ViewBag.DocID = new SelectList(db.Documents, "DocID", "DocName", document.DocID);
            ViewBag.ProxyID = new SelectList(db.Proxies, "ProxyID", "FullName", document.ProxyID);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", document.CreatedBy);
            ViewBag.DocAuthorizer = new SelectList(db.Users, "UID", "FullName", document.DocAuthorizer);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", document.ShID);
            return View(document);
        }

        // GET: Documents/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Document document = db.Documents.Find(id);
            if (document == null)
            {
                return HttpNotFound();
            }
            return View(document);
        }

        // POST: Documents/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Document document = db.Documents.Find(id);
            db.Documents.Remove(document);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        public ActionResult DownloadDocument(int id)
        {
            // Retrieve the document from the database using the document ID
            var document = db.Documents.FirstOrDefault(d => d.DocID == id);

            if (document == null)
            {
                // If the document does not exist, return a 404 error
                return HttpNotFound();
            }

            // Resolve the document's path (DocPath should be the relative path)
            string filePath = Server.MapPath(document.DocPath);

            // Check if the file exists on the server
            if (!System.IO.File.Exists(filePath))
            {
                // If the file does not exist, return a 404 error
                return HttpNotFound();
            }

            // Get the file name (you can use document.DocName if you want to return the unique file name)
            string fileName = Path.GetFileName(filePath);

            // Return the file as a downloadable response
            return File(filePath, "application/octet-stream", fileName);
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
