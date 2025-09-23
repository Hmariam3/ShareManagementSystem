using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Shareholder_Management_System.Models;

namespace Shareholder_Management_System.Controllers
{
    public class DocumentsController : BaseController
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        public List<Document> GetDocumentsFromDatabase(string ShID, string ShareID, string branch)
        {

            IQueryable<Document> query = db.Documents.Include(d => d.Shareholder);

            int shareholderId;
            if (int.TryParse(ShID, out shareholderId))
            {

                query = query.Where(d => d.ShID == shareholderId && d.ShID != null);
            }

            if (!string.IsNullOrEmpty(ShareID))
            {

                query = query.Where(d => d.Shareholder.ShID.ToString() == ShareID);
            }

            if (!string.IsNullOrEmpty(branch))
            {

                query = query.Where(d => d.Shareholder.Branch.Equals(branch));
            }

            return query.ToList();
        }

        // GET: Documents
        public ActionResult Index(string ShID = null, string shareID = null, string branch = null)
        {
            IEnumerable<Document> documents = new List<Document>();

            var shareholders = db.Documents.Select(s => new SelectListItem
            {
                Value = s.ShID.ToString(),
                Text = s.Shareholder.FullNameEng + " (" + s.Shareholder.ShareID + ")"
            }).ToList();
            shareholders.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "Select a Shareholder",
            });

            ViewBag.Shareholders = shareholders;


            if (String.IsNullOrEmpty(ShID) && String.IsNullOrEmpty(shareID))
            {
                documents = db.Documents.Where(s => s.ShID == 199999999);

            }
            else
            {
                int shID = int.Parse(ShID);

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
        public int Create(Document document, HttpPostedFileBase uploadedFile, int? transfreeID = null)
        {
            if (ModelState.IsValid)
            {
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    var shareholder = db.Shareholders.Find(document.ShID);
                    var transfer = db.ShareTransfers.Find(document.ShID);
                    var proxies = db.Proxies.Find(document.ProxyID);

                    string sanitizedFullNameEng = SanitizeFileName(shareholder.FullNameEng);

                    string baseFileName;
                    var fileExtension = Path.GetExtension(uploadedFile.FileName);

                    switch (document.DocType)
                    {
                        case "ShareholderAgreement":
                            baseFileName = $"{sanitizedFullNameEng}_ShareholderAgreement";
                            break;
                        case "ShareholderID":
                            baseFileName = $"{sanitizedFullNameEng}_ID";
                            break;
                        case "ShBlockLetter":
                            baseFileName = $"{sanitizedFullNameEng}_ShBlockLetter";
                            break;
                        case "ShUnBlockLetter":
                            baseFileName = $"{sanitizedFullNameEng}_ShUnBlockLetter";
                            break;
                        case "ProxyID":
                            baseFileName = $"{sanitizedFullNameEng}_ProxyID";
                            break;
                        case "DeligationLetter":
                            baseFileName = $"{sanitizedFullNameEng}_DelegationLetter";
                            break;
                        case "Payment Slip":
                            baseFileName = $"{sanitizedFullNameEng}_PaymentSlip";
                            break;
                        case "Blocking Document":
                            baseFileName = $"{sanitizedFullNameEng}_BlockingDocument";
                            break;
                        case "Transfer Document":
                            var shareholder1 = db.Shareholders.Find(transfreeID);
                            string sanitizedTransfreeFullNameEng = SanitizeFileName(shareholder1.FullNameEng);
                            baseFileName = $"{sanitizedFullNameEng}_{sanitizedTransfreeFullNameEng}_TransferDocument";
                            break;
                        default:
                            throw new Exception("Invalid document type.");
                    }

                    string baseFolder = Server.MapPath("~/Documents/");

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
                            folderPath = Path.Combine(baseFolder, "Shareholder", "Shareholder Block");
                            break;
                        case "ShUnBlockLetter":
                            folderPath = Path.Combine(baseFolder, "Shareholder", "Shareholder UnBlock");
                            break;
                        case "ProxyID":
                            folderPath = Path.Combine(baseFolder, "Proxy", "ID");
                            break;
                        case "DeligationLetter":
                            folderPath = Path.Combine(baseFolder, "Proxy", "Delegation");
                            break;
                        case "Payment Slip":
                            folderPath = Path.Combine(baseFolder, "Payment", "Payment Slip");
                            break;
                        case "Blocking Document":
                            folderPath = Path.Combine(baseFolder, "Blocking", "Blocking Document");
                            break;
                        case "Transfer Document":
                            folderPath = Path.Combine(baseFolder, "Transfer", "Transfer Document");
                            break;
                        default:
                            throw new Exception("Invalid document type.");
                    }

                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    string fileName = $"{baseFileName}{fileExtension}";
                    string filePath = Path.Combine(folderPath, fileName);

                    int counter = 1;
                    while (System.IO.File.Exists(filePath))
                    {
                        fileName = $"{baseFileName}_{counter}{fileExtension}";
                        filePath = Path.Combine(folderPath, fileName);
                        counter++;
                    }

                    uploadedFile.SaveAs(filePath);

                    document.DocPath = $"~/{folderPath.Replace(Server.MapPath("~/"), "").Replace("\\", "/")}/{fileName}";
                    document.DocName = fileName;

                    document.CreatedDate = DateTime.Now;

                    db.Documents.Add(document);
                    db.SaveChanges();

                    return document.DocID;
                }
                else
                {
                    throw new Exception("No file was uploaded.");
                }
            }

            return -1;
        }

        //private string SanitizedFileName(string fileName)
        //{
        //    if (string.IsNullOrEmpty(fileName))
        //    {
        //        return "Unknown";
        //    }

        //    // Replace '/' with '_' and remove other invalid filename characters
        //    string sanitized = fileName.Replace("/", "_");
        //    return string.Join("_", sanitized.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        //}

        private string SanitizeFileName(string fileName)
        {
            string sanitized = fileName.Replace("/", "_");
            Regex invalidCharsRegex = new Regex($"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]");
            return invalidCharsRegex.Replace(fileName, "_");
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

        public JsonResult GetDocumentPath(int id)
        {
            // Retrieve the document from the database using the document ID
            var document = db.Documents.FirstOrDefault(d => d.DocID == id);

            if (document == null)
            {
                // If the document does not exist, return an error response
                return Json(new { success = false, message = "Document not found." }, JsonRequestBehavior.AllowGet);
            }

            string webFilePath = Url.Content(document.DocPath);

            return Json(new { success = true, filePath = webFilePath }, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetDocumentStatsByType(string docType)
        {

            var documents = db.Documents.Where(d => d.DocType == docType);

            var docCount = documents.Count();
            long totalSizeInBytes = 0;

            foreach (var doc in documents)
            {
                string filePath = doc.DocPath;

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
                    .Where(d => d.DocType == doctype)
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

                return HttpNotFound();
            }

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
