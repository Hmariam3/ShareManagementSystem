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
    public class ShareholdersController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        // GET: Shareholders
        public ActionResult Index()
        {

            var shareholders = db.Shareholders.Include(s => s.Branch1).Include(s => s.User).Include(s => s.User1).Include(s => s.Document);
            return View(shareholders.ToList());
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
            return View(shareholder);
        }

        // GET: Shareholders/Create
        public ActionResult Create()
        {
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode");
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");
            ViewBag.Authorizer = new SelectList(db.Users, "UID", "FullName");
            ViewBag.ShCategories = new SelectList(db.ShCategories, "ShCategories", "ShCategories");

            return View();
        }

        // POST: Shareholders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Shareholder shareholder, HttpPostedFileBase shFile)
        {
            int userId = Convert.ToInt32(Session["ID"]);
            int branchId = Convert.ToInt32(Session["Branch"]);

            if (ModelState.IsValid)
            {
                shareholder.Branch = branchId;
                shareholder.CreatedBy = userId;
                shareholder.Status = "InActive";
                shareholder.CreatedDate = DateTime.Now;
                shareholder.AuthorizationStatus = "Pending";

                if (shFile == null)
                {
                    throw new ArgumentNullException(nameof(shFile));
                }

                // Handle document creation
                if (shFile != null && shFile.ContentLength > 0)
                {
                    db.Shareholders.Add(shareholder);
                    db.SaveChanges();

                    Document document = new Document
                    {
                        DocOwner = "Shareholder",
                        DocType = "ShareholderInfo",
                        ShID = shareholder.ShID,
                        CreatedBy = userId,
                        DocAuthorizationStatus = "Pending",
                        CreatedDate = DateTime.Now,
                    };

                    // Instantiate the DocumentsController to save the document
                    DocumentsController documentsController = new DocumentsController();
                    documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

                    int documentId = documentsController.Create(document, shFile);
                    // Update the ShDocument field of the shareholder with the documentId
                    shareholder.ShDocument = documentId;

                    //// Update the shareholder record in the database
                    db.Entry(shareholder).State = EntityState.Modified;
                    db.SaveChanges();
                }

                return RedirectToAction("Index");
            }

            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", shareholder.Branch);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", shareholder.CreatedBy);
            ViewBag.Authorizer = new SelectList(db.Users, "UID", "FullName", shareholder.Authorizer);
            ViewBag.ShCategories = new SelectList(db.ShCategories, "ShCategories", "ShCategories");

            return View(shareholder);
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

            return View(shareholder);
        }


        [HttpPost]
        public ActionResult UpdateShDocument(int shID, HttpPostedFileBase shFile)
        {
            int userId = Convert.ToInt32(Session["ID"]);
            int branchId = Convert.ToInt32(Session["Branch"]);

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
                DocType = "ShareholderInfo",
                ShID = shareholder.ShID,
                CreatedBy = userId,
                DocAuthorizationStatus = "Pending",
                CreatedDate = DateTime.Now,
            };

            DocumentsController documentsController = new DocumentsController();
            documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

            int documentId = documentsController.Create(document, shFile);
            shareholder.ShDocument = documentId;
            shareholder.Branch = branchId;
            shareholder.CreatedBy = userId;
            shareholder.Status = "InActive";
            shareholder.CreatedDate = DateTime.Now;
            shareholder.AuthorizationStatus = "Pending";

            // Update the proxy record in the database
            db.Entry(shareholder).State = EntityState.Modified;
            db.SaveChanges();

            return Json(new { success = true });
        }


        [HttpPost]
        public ActionResult BlockSh(int shID, string reason, HttpPostedFileBase shFile)
        {
            int userId = Convert.ToInt32(Session["ID"]);
            int branchId = Convert.ToInt32(Session["Branch"]);


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

            shareholder.Branch = branchId;
            shareholder.CreatedBy = userId;
            shareholder.Status = "Blocked";
            shareholder.CreatedDate = DateTime.Now;
            shareholder.AuthorizationStatus = "Pending";
            shareholder.Remark = reason;

            // Update the proxy record in the database
            db.Entry(shareholder).State = EntityState.Modified;
            db.Entry(shareholder).Property(x => x.ShDocument).IsModified = false;

            db.SaveChanges();

            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult UnBlockSh(int shID, string reason, HttpPostedFileBase shFile)
        {
            int userId = Convert.ToInt32(Session["ID"]);
            int branchId = Convert.ToInt32(Session["Branch"]);

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

            return Json(new { success = true });
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

            if (ModelState.IsValid)
            {
                shareholder.Branch = branchId;
                shareholder.CreatedBy = userId;
                shareholder.Status = "InActive";
                shareholder.CreatedDate = DateTime.Now;
                shareholder.AuthorizationStatus = "Pending";

                db.Entry(shareholder).State = EntityState.Modified;
                db.Entry(shareholder).Property(x => x.ShDocument).IsModified = false;

                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", shareholder.Branch);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", shareholder.CreatedBy);
            ViewBag.Authorizer = new SelectList(db.Users, "UID", "FullName", shareholder.Authorizer);
            ViewBag.ShCategories = new SelectList(db.ShCategories, "ShCategories", "ShCategories", shareholder.SHCategory);

            return View(shareholder);
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
