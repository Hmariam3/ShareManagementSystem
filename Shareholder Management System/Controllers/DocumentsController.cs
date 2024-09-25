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

        // GET: Documents
        public ActionResult Index()
        {
            var documents = db.Documents.Include(d => d.Proxy).Include(d => d.User).Include(d => d.User1).Include(d => d.Shareholder);
            return View(documents.ToList());
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
        public int Create(Document document, HttpPostedFileBase uploadedFile)
        {
            if (ModelState.IsValid)
            {
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    // Generate unique file name
                    var fileName = Path.GetFileNameWithoutExtension(uploadedFile.FileName);
                    var fileExtension = Path.GetExtension(uploadedFile.FileName);
                    var uniqueFileName = $"{fileName}_{Guid.NewGuid()}{fileExtension}";

                    // Set the path where the file will be saved
                    var path = Path.Combine(Server.MapPath("~/UploadedFiles/"), uniqueFileName);

                    // Save the file
                    uploadedFile.SaveAs(path);

                    // Set the file path and name in the document model               
                    document.DocPath = "~/UploadedFiles/" + uniqueFileName;
                    document.DocName = uniqueFileName;
                }

                // Add document to the database
                db.Documents.Add(document);
                db.SaveChanges();

                // Return the ID of the newly created document
                return document.DocID;
            }

            // If the model state is invalid, return -1 to indicate an error
            return -1;
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
