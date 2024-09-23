using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Shareholder_Management_System.Models;

namespace Shareholder_Management_System.Controllers
{
    public class ShareTransfersController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        public JsonResult GetTableData(string transferType, int transferrorShID)
        {
            var subscriptionData = "";
            var paymentData = "";

            try
            {
                if (transferType == "Subscription" || transferType == "Share")
                {
                    // Fetch subscription data
                    subscriptionData = GetSubscriptionData(transferrorShID);
                }

                if (transferType == "Payment" || transferType == "Share")
                {
                    // Fetch payment data
                    paymentData = GetPaymentData(transferrorShID);
                }
            }
            catch (Exception ex)
            {
                // Log the exception details
                // e.g., Log.Error("Error fetching data", ex);
                return Json(new { error = "An error occurred while fetching the data." }, JsonRequestBehavior.AllowGet);
            }

            return Json(new
            {
                subscriptionData = subscriptionData,
                paymentData = paymentData
            }, JsonRequestBehavior.AllowGet);
        }


        private string GetSubscriptionData(int transferrorShID)
        {
            try
            {
                // Example data fetching logic (replace with actual database query)
                var subscriptions = db.Subscribtions.Where(s => s.ShID == transferrorShID).ToList();

                var sb = new StringBuilder();
                foreach (var sub in subscriptions)
                {
                    sb.AppendLine($"<tr><td>{sub.ShID}</td><td>{sub.SubID}</td><td>{sub.SubNumShares}</td><td>{sub.PaidSubscription}</td><td>{sub.UnpaidSubscription}</td></tr>");
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                // Log the exception details
                // e.g., Log.Error("Error fetching subscription data", ex);
                return "<tr><td colspan='2'>Error fetching data</td></tr>";
            }
        }

        private string GetPaymentData(int transferrorShID)
        {
            try
            {
                // Example data fetching logic (replace with actual database query)
                var payments = db.Payments.Where(p => p.ShID == transferrorShID).ToList();

                var sb = new StringBuilder();
                foreach (var pay in payments)
                {
                    sb.AppendLine($"<tr><td>{pay.PayID}</td><td>{pay.ShID}</td><td>{pay.SubID}</td><td>{pay.PaidAmount}</td><td>{pay.PaymentMode}</td></tr>");
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                // Log the exception details
                // e.g., Log.Error("Error fetching payment data", ex);
                return "<tr><td colspan='2'>Error fetching data</td></tr>";
            }
        }



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
        public ActionResult Create()
        {
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode");
            ViewBag.TransferrorShID = new SelectList(db.Shareholders.OrderBy(s => s.FullNameEng), "ShID", "FullNameEng");
            ViewBag.TransfareeShID = new SelectList(db.Shareholders.OrderBy(s => s.FullNameEng), "ShID", "FullNameEng");
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus");
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");
            ViewBag.TransferAuthorizer = new SelectList(db.Users, "UID", "FullName");
            return View();
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
            ViewBag.TransferrorShID = new SelectList(db.Shareholders, "ShID", "FullNameEng", shareTransfer.TransferrorShID);
            ViewBag.TransfareeShID = new SelectList(db.Shareholders, "ShID", "FullNameEng", shareTransfer.TransfareeShID);
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
