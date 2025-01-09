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
    public class DividendsController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        // GET: Dividends
        public ActionResult Index()
        {
            var dividends = db.Dividends.Include(d => d.User);
            ViewBag.CurrentDividend = new Dividend();

            return View(dividends.ToList());
        }

        // GET: Dividends/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Dividend dividend = db.Dividends.Find(id);
            if (dividend == null)
            {
                return HttpNotFound();
            }
            return View(dividend);
        }


        [HttpPost]
        public ActionResult Approve(int id)
        {
            try
            {
                int AuthorizerId = Convert.ToInt32(Session["ID"]);
                // Retrieve the dividend record by its ID
                var dividend = db.Dividends.SingleOrDefault(d => d.DivID == id);
                if (dividend == null)
                {
                    return HttpNotFound(); // Return 404 if the record is not found
                }

                // Update the AuthorizationStatus field
                // Change status to Approved
                dividend.Authorizer = AuthorizerId;
                dividend.AuthorizationStatus = "Approved";
                dividend.AuthorizedDate = DateTime.Now;
                db.Entry(dividend).State = EntityState.Modified;
                db.SaveChanges();

                // Optionally, add a success message
                TempData["SuccessMessage"] = "Authorization status updated successfully.";

                // Redirect to an appropriate page
                return RedirectToAction("Index"); // Replace 'Index' with your desired view
            }
            catch (Exception ex)
            {
                // Log the error if necessary
                TempData["ErrorMessage"] = "An error occurred while updating the status.";
                return RedirectToAction("Index"); // Replace 'Index' with your desired view
            }
        }
    
    //// Approval Status
    //[HttpPost]
    //    public ActionResult Approve(int id)
    //    {
    //        using (var transaction = db.Database.BeginTransaction())
    //        {
    //            try
    //            {
    //                int AuthorizerId = Convert.ToInt32(Session["ID"]);
    //                var dividend = db.Dividends.Find(id);
    //                if (dividend == null)
    //                {
    //                    return HttpNotFound();
    //                }

    //                // Change status to Approved
    //                dividend.Authorizer = AuthorizerId;
    //                dividend.AuthorizationStatus = "Approved";
    //                dividend.AuthorizedDate = DateTime.Now;
    //                db.Entry(dividend).State = EntityState.Modified;
    //                db.SaveChanges();

    //                transaction.Commit();
    //                return RedirectToAction("Index");
    //            }
    //            catch (Exception ex)
    //            {
    //                transaction.Rollback();
    //                ModelState.AddModelError("", "Error approving the dividend: " + ex.Message);
    //            }
    //        }
    //        return View();
    //    }
        // GET: Dividends/Create
        public ActionResult Create()
        {
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");
            return View();
        }

        // POST: Dividends/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Dividend dividend)
        {
            if (ModelState.IsValid)
            {
                int userId = Convert.ToInt32(Session["ID"]);

                DateTime currentDate = DateTime.Now;
                int currentYear = currentDate.Year;
                int lastYear = currentDate.Year - 1;
                DateTime fiscalYearStart, fiscalYearEnd;

                fiscalYearStart = new DateTime(lastYear, 7, 1); // July 1 of the previous year
                fiscalYearEnd = new DateTime(currentYear, 6, 30); // June 30 of the current year

                // Check if the fiscal year includes a leap year
                bool isLeapYear = DateTime.IsLeapYear(fiscalYearStart.Year);

                // Determine total fiscal days
                int totalFiscalDays = isLeapYear ? 366 : 365;

                dividend.FiscalYear = lastYear + " - " + currentYear;
                dividend.NumOutstandingDays = totalFiscalDays;
                dividend.CreationDate = currentDate;
                dividend.CreatedBy = userId;
                dividend.AuthorizationStatus = "Pending";

                db.Dividends.Add(dividend);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", dividend.CreatedBy);
            return View(dividend);
        }

        // GET: Dividends/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Dividend dividend = db.Dividends.Find(id);
            if (dividend == null)
            {
                return HttpNotFound();
            }
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", dividend.CreatedBy);
            return View(dividend);
        }

        // POST: Dividends/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, [Bind(Include = "Profit, CreatedBy")] Dividend dividend)
        {
            if (ModelState.IsValid)
            {
                var existingDividend = db.Dividends.Find(id);
                int AuthorizerId = Convert.ToInt32(Session["ID"]);
                if (existingDividend == null)
                {
                    return HttpNotFound();
                }

                // Update only the Profit field
                existingDividend.Profit = dividend.Profit;
                existingDividend.CreatedBy = AuthorizerId;

                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(dividend);
        }


        // GET: Dividends/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Dividend dividend = db.Dividends.Find(id);
            if (dividend == null)
            {
                return HttpNotFound();
            }
            return View(dividend);
        }

        // POST: Dividends/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Dividend dividend = db.Dividends.Find(id);
            db.Dividends.Remove(dividend);
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