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
    public class DividenedDetailsController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        // GET: DividenedDetails
        public ActionResult Index()
        {
            var dividenedDetails = db.DividenedDetails.Include(d => d.Branch1).Include(d => d.Dividend).Include(d => d.Payment1).Include(d => d.Shareholder).Include(d => d.Subscribtion).Include(d => d.User);
            return View(dividenedDetails.ToList());
        }

        // GET: DividenedDetails/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DividenedDetail dividenedDetail = db.DividenedDetails.Find(id);
            if (dividenedDetail == null)
            {
                return HttpNotFound();
            }
            return View(dividenedDetail);
        }

        // GET: DividenedDetails/Create
        public ActionResult Create()
        {
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode");
            ViewBag.DivID = new SelectList(db.Dividends, "DivID", "DivID");
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode");
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID");
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus");
            ViewBag.RequestHandler = new SelectList(db.Users, "UID", "FullName");
            return View();
        }

        // POST: DividenedDetails/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,DivID,ShID,PayID,SubID,OutstandingDays,WASA,DividenedAmount,TaxedDividened,Payment,Capitalization,PaymentStatus,RequestHandler,Branch,DateApplication,SettlementDate,DividenedYear")] DividenedDetail dividenedDetail)
        {
            if (ModelState.IsValid)
            {
                db.DividenedDetails.Add(dividenedDetail);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", dividenedDetail.Branch);
            ViewBag.DivID = new SelectList(db.Dividends, "DivID", "DivID", dividenedDetail.DivID);
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode", dividenedDetail.PayID);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", dividenedDetail.ShID);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus", dividenedDetail.SubID);
            ViewBag.RequestHandler = new SelectList(db.Users, "UID", "FullName", dividenedDetail.RequestHandler);
            return View(dividenedDetail);
        }

        // GET: DividenedDetails/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DividenedDetail dividenedDetail = db.DividenedDetails.Find(id);
            if (dividenedDetail == null)
            {
                return HttpNotFound();
            }
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", dividenedDetail.Branch);
            ViewBag.DivID = new SelectList(db.Dividends, "DivID", "DivID", dividenedDetail.DivID);
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode", dividenedDetail.PayID);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", dividenedDetail.ShID);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus", dividenedDetail.SubID);
            ViewBag.RequestHandler = new SelectList(db.Users, "UID", "FullName", dividenedDetail.RequestHandler);
            return View(dividenedDetail);
        }

        // POST: DividenedDetails/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,DivID,ShID,PayID,SubID,OutstandingDays,WASA,DividenedAmount,TaxedDividened,Payment,Capitalization,PaymentStatus,RequestHandler,Branch,DateApplication,SettlementDate,DividenedYear")] DividenedDetail dividenedDetail)
        {
            if (ModelState.IsValid)
            {
                db.Entry(dividenedDetail).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", dividenedDetail.Branch);
            ViewBag.DivID = new SelectList(db.Dividends, "DivID", "DivID", dividenedDetail.DivID);
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode", dividenedDetail.PayID);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", dividenedDetail.ShID);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus", dividenedDetail.SubID);
            ViewBag.RequestHandler = new SelectList(db.Users, "UID", "FullName", dividenedDetail.RequestHandler);
            return View(dividenedDetail);
        }

        // GET: DividenedDetails/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DividenedDetail dividenedDetail = db.DividenedDetails.Find(id);
            if (dividenedDetail == null)
            {
                return HttpNotFound();
            }
            return View(dividenedDetail);
        }

        // POST: DividenedDetails/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            DividenedDetail dividenedDetail = db.DividenedDetails.Find(id);
            db.DividenedDetails.Remove(dividenedDetail);
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