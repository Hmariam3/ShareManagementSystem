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
            var shareholders = db.Shareholders.Select(s => new SelectListItem
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
                ViewBag.Shareholder = shareholder;
            }
            ViewBag.NewProxy = new Proxy();


            return View(proxies);
        }

        [HttpPost]
        public ActionResult UpdateProxyDocument(int proxyID, HttpPostedFileBase proxyFile)
        {
            if (proxyFile == null || proxyFile.ContentLength == 0)
            {
                return Json(new { success = false, message = "File is required." });
            }

            var proxy = db.Proxies.Find(proxyID);
            if (proxy == null)
            {
                return Json(new { success = false, message = "Proxy not found." });
            }

            // Handle document creation and file upload logic
            Document document = new Document
            {
                DocOwner = "Shareholder",
                DocType = "DelegationLetter",
                ShID = proxy.ShID,
                CreatedBy = 1,  // Assuming 1 is the user creating it, update as per your logic
                DocAuthorizationStatus = "Pending",
                CreatedDate = DateTime.Now,
            };

            DocumentsController documentsController = new DocumentsController();
            documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

            int documentId = documentsController.Create(document, proxyFile);
            proxy.ProxyDocument = documentId;
            proxy.CreatedDate = DateTime.Now;
            proxy.ProxyAuthorizationStatus = "Pending";
            proxy.ProxyStatus = "Active";

            // Update the proxy record in the database
            db.Entry(proxy).State = EntityState.Modified;
            db.SaveChanges();

            return Json(new { success = true });
        }


        [HttpPost]
        public ActionResult DeactivateProxy(int proxyID)
        {
            var proxy = db.Proxies.Find(proxyID);
            if (proxy != null)
            {
                proxy.ProxyStatus = "InActive"; // Mark as inactive
                db.SaveChanges();
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
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID");
            return View();
        }

        // POST: Proxies/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Proxy proxy, HttpPostedFileBase proxyFile)
        {

            if (ModelState.IsValid)
            {
                proxy.CreatedBy = 1;
                proxy.ProxyStatus = "Active";
                proxy.CreatedDate = DateTime.Now;
                proxy.ProxyAuthorizationStatus = "Pending";

                if (proxyFile == null)
                {
                    throw new ArgumentNullException(nameof(proxyFile));
                }

                // Handle document creation
                if (proxyFile != null && proxyFile.ContentLength > 0)
                {
                    db.Proxies.Add(proxy);
                    db.SaveChanges();

                    Document document = new Document
                    {
                        DocOwner = "Shareholder",
                        DocType = "ShareholderInfo",
                        ShID = proxy.ShID,
                        CreatedBy = 1,
                        DocAuthorizationStatus = "Pending",
                        CreatedDate = DateTime.Now,
                    };

                    // Instantiate the DocumentsController to save the document
                    DocumentsController documentsController = new DocumentsController();
                    documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

                    int documentId = documentsController.Create(document, proxyFile);
                    // Update the ShDocument field of the shareholder with the documentId
                    proxy.ProxyDocument = documentId;

                    //// Update the shareholder record in the database
                    db.Entry(proxy).State = EntityState.Modified;
                    db.SaveChanges();
                }
                return RedirectToAction("Index", new { ShID = proxy.ShID });
            }

            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", proxy.CreatedBy);
            ViewBag.ProxyAuthorizer = new SelectList(db.Users, "UID", "FullName", proxy.ProxyAuthorizer);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", proxy.ShID);
            return View(proxy);
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
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", proxy.ShID);
            return View(proxy);
        }

        // POST: Proxies/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Proxy proxy)
        {
            if (ModelState.IsValid)
            {
                proxy.CreatedBy = 1;
                proxy.ProxyStatus = "Active";
                proxy.CreatedDate = DateTime.Now;
                proxy.ProxyAuthorizationStatus = "Pending";

                db.Entry(proxy).State = EntityState.Modified;
                //db.Entry(proxy).Property(x => x.ShID).IsModified = false;
                db.Entry(proxy).Property(x => x.Remark).IsModified = false;
                db.Entry(proxy).Property(x => x.ProxyDocument).IsModified = false;

                db.SaveChanges();
                return RedirectToAction("Index", new { ShID = proxy.ShID });
            }
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", proxy.CreatedBy);
            ViewBag.ProxyAuthorizer = new SelectList(db.Users, "UID", "FullName", proxy.ProxyAuthorizer);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", proxy.ShID);
            return View(proxy);
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
