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

        // GET: ShareTransfers
        public ActionResult Index()
        {
            var shareTransfers = db.ShareTransfers.Include(s => s.Payment).Include(s => s.Shareholder).Include(s => s.Shareholder1).Include(s => s.Subscribtion).Include(s => s.User).Include(s => s.User1);
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

        // GET: ShareTransfers/Create
        // GET: ShareTransfers/Create
        public ActionResult Create()
        {
            // Fetch the subscriptions for the dropdowns
            var subscriptions = db.Subscribtions.ToList();

            // Fetch the shareholder for the dropdowns
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

            // Pass subscriptions data to the view
            ViewBag.Subscriptions = subscriptions;

            // Populate dropdowns for Shareholders, Users, etc.
            ViewBag.TransferrorShID = new SelectList(db.Shareholders, "ShID", "FullNameEng");
            ViewBag.TransfareeShID = new SelectList(db.Shareholders, "ShID", "FullNameEng");
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");
            ViewBag.TransferAuthorizer = new SelectList(db.Users, "UID", "FullName");

            return View();
        }



        // GET: ShareTransfers/GetShareholderData
        public JsonResult GetShareholderData(int shareholderId)
        {
            // Fetch subscriptions for the selected shareholder
            var subscriptions = db.Subscribtions
                .Where(s => s.ShID == shareholderId)
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
            // Fetch payments associated with the selected subscription
            var payments = db.Payments
                .Where(p => p.SubID == subscriptionId)
                .Select(p => new
                {
                    p.PayID,
                    p.PaidAmount
                })
                .ToList();

            return Json(new { payments }, JsonRequestBehavior.AllowGet);
        }



        // POST: ShareTransfers/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "TransferID,TransferrorShID,TransfareeShID,TransferType,SubID,PayID,NumSharesTransferred,AmountPerShare,PaidAmountForTransfer,TransferReason,DividenedFor,TransferDoc,TransferDate,CreatedBy,CreationDate,TransferAuthorizationStatus,TransferAuthorizer,TransferAuthorizationDate,Remark")] ShareTransfer shareTransfer)
        {
            if (ModelState.IsValid)
            {
                db.ShareTransfers.Add(shareTransfer);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode", shareTransfer.PayID);
            ViewBag.TransferrorShID = new SelectList(db.Shareholders, "ShID", "ShareID", shareTransfer.TransferrorShID);
            ViewBag.TransfareeShID = new SelectList(db.Shareholders, "ShID", "ShareID", shareTransfer.TransfareeShID);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus", shareTransfer.SubID);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", shareTransfer.CreatedBy);
            ViewBag.TransferAuthorizer = new SelectList(db.Users, "UID", "FullName", shareTransfer.TransferAuthorizer);
            return View(shareTransfer);
        }

        // GET: ShareTransfers/Edit/5
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
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode", shareTransfer.PayID);
            ViewBag.TransferrorShID = new SelectList(db.Shareholders, "ShID", "ShareID", shareTransfer.TransferrorShID);
            ViewBag.TransfareeShID = new SelectList(db.Shareholders, "ShID", "ShareID", shareTransfer.TransfareeShID);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus", shareTransfer.SubID);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", shareTransfer.CreatedBy);
            ViewBag.TransferAuthorizer = new SelectList(db.Users, "UID", "FullName", shareTransfer.TransferAuthorizer);
            return View(shareTransfer);
        }

        // POST: ShareTransfers/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "TransferID,TransferrorShID,TransfareeShID,TransferType,SubID,PayID,NumSharesTransferred,AmountPerShare,PaidAmountForTransfer,TransferReason,DividenedFor,TransferDoc,TransferDate,CreatedBy,CreationDate,TransferAuthorizationStatus,TransferAuthorizer,TransferAuthorizationDate,Remark")] ShareTransfer shareTransfer)
        {
            if (ModelState.IsValid)
            {
                db.Entry(shareTransfer).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode", shareTransfer.PayID);
            ViewBag.TransferrorShID = new SelectList(db.Shareholders, "ShID", "ShareID", shareTransfer.TransferrorShID);
            ViewBag.TransfareeShID = new SelectList(db.Shareholders, "ShID", "ShareID", shareTransfer.TransfareeShID);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus", shareTransfer.SubID);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", shareTransfer.CreatedBy);
            ViewBag.TransferAuthorizer = new SelectList(db.Users, "UID", "FullName", shareTransfer.TransferAuthorizer);
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
