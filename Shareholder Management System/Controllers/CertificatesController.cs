using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Shareholder_Management_System.Models;

namespace Shareholder_Management_System.Controllers
{
    public class CertificatesController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        // GET: Certificates
        public ActionResult Index()
        {
            var certificates = db.Certificates.Include(c => c.Shareholder).ToList();

            //foreach (var certificate in certificates)
            //{
            //    // Fetch the payment amount for the given PaymentIDs
            //    var selectedPayments = certificate.PaymentIDs.Split(',').Select(int.Parse).ToList();
            //    var totalPaymentAmount = db.Payments
            //                               .Where(p => selectedPayments.Contains(p.PayID))
            //                               .Sum(p => p.PaidAmount);

            //    // Assuming you add a new property 'TotalPaidAmount' to your Certificate or Shareholder model
            //    certificate = totalPaymentAmount;
            //}

            return View(certificates);
        }
        // GET: Certificates/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Certificate certificate = db.Certificates.Find(id);
            if (certificate == null)
            {
                return HttpNotFound();
            }
            return View(certificate);
        }

        // GET: Certificates/Create
        public ActionResult Create()
        {
            // Fetch the last certificate number and convert it after retrieving it in memory
            int lastCertNum = db.Certificates
                                .Where(c => c.CertNum != null) // Filter out any null CertNum
                                .ToList() // Fetch into memory
                                .Select(c => int.Parse(c.CertNum)) // Parse CertNum as an integer
                                .DefaultIfEmpty(0) // Handle case where no CertNum exists
                                .Max(); // Get the max value

            // Set the new certificate number by incrementing the last one
            var newCertNum = (lastCertNum + 1).ToString();
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
            // Assign the certificate number to ViewBag
            ViewBag.NewCertNum = newCertNum;
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID");
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName");
            ViewBag.CertAuthorizer = new SelectList(db.Users, "UID", "FullName");
            return View();
        }

        // POST: Certificates/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CertID,ShID,CertNum,CreatedBy,CreatedDate,DeliveryStatus,DeliveredBy,DeliveryDate,CertAuthorizationStatus,CertGenerationDate,CertAuthorizer,Remark")] Certificate certificate, int[] selectedPayments)
        {
            if (ModelState.IsValid)
            {
                // Set the CreatedBy and CreatedDate fields
                certificate.CreatedBy = Convert.ToInt32(Session["ID"]);
                certificate.CreatedDate = System.DateTime.Now;

                // Join the selected payment IDs into a comma-separated string and assign it to the PaymentIDs field
                if (selectedPayments != null && selectedPayments.Any())
                {
                    certificate.PaymentIDs = string.Join(",", selectedPayments);
                }

                // Fetch the payment amount for the given PaymentIDs
                var totalPaymentAmount = db.Payments
                                           .Where(p => selectedPayments.Contains(p.PayID))
                                           .Sum(p => p.PaidAmount);

                //// Get the max EndingSerial from the Certificates table, or start from 0 if no entries exist
                //int lastEndingSerial = db.Certificates.Any() ? db.Certificates.Max(c => c.EndingSerial).GetValueOrDefault() : 0;

                // Fetch EndingSerial values from the database
                var endingSerials = db.Certificates
                    .Select(c => c.EndingSerial)
                    .ToList(); // Move data to memory

                // Filter valid integers and find the max, or default to 0
                int lastEndingSerial = endingSerials
                    .Where(e => int.TryParse(e, out _)) // Ensure valid integers
                    .Select(e => int.Parse(e)) // Parse valid strings to integers
                    .DefaultIfEmpty(0) // If no valid integers, default to 0
                    .Max();


                // Calculate BeginningSerial and EndingSerial
                certificate.BeginingSerial = (lastEndingSerial + 1).ToString();
                int numberOfShares = (int)(totalPaymentAmount / 1000); // Assuming each share is worth 1000
                certificate.EndingSerial = (int.Parse(certificate.BeginingSerial) + numberOfShares - 1).ToString();

                // Fetch the last certificate number and convert it after retrieving it in memory
                int lastCertNum = db.Certificates
                                    .Where(c => c.CertNum != null) // Filter out any null CertNum
                                    .ToList() // Fetch into memory
                                    .Select(c => int.Parse(c.CertNum)) // Parse CertNum as an integer
                                    .DefaultIfEmpty(0) // Handle case where no CertNum exists
                                    .Max(); // Get the max value

                // Set the new certificate number by incrementing the last one
                var newCertNum = (lastCertNum + 1).ToString();

                // Assign the certificate number to ViewBag
                ViewBag.NewCertNum = newCertNum;

                // Set the certificate number before saving it to the database
                certificate.CertNum = newCertNum;

                // Save the certificate to the database
                db.Certificates.Add(certificate);
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            // If the model state is invalid, return the view with the model errors
            return View(certificate);
        }

        public JsonResult GetShareholders(string term)
        {
            // Log or inspect the incoming term to ensure it's passed correctly
            if (string.IsNullOrWhiteSpace(term))
            {
                // If no search term, return an empty list
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            // Search for shareholders based on the term
            var shareholders = db.Shareholders
                                 .Where(s => s.FullNameEng.Contains(term) || s.PhoneNo.Contains(term))
                                 .Select(s => new
                                 {
                                     Value = s.ShID, // This will be the value in the dropdown
                                     Text = s.FullNameEng // This will be the displayed name in the dropdown
                                 })
                                 .ToList();

            // Check if any shareholders were found
            if (!shareholders.Any())
            {
                // Return a message if no shareholders were found
                return Json(new { message = "No shareholders found" }, JsonRequestBehavior.AllowGet);
            }

            // Return the result as JSON
            return Json(shareholders, JsonRequestBehavior.AllowGet);
        }
        public JsonResult GetPaymentsByShID(int shID)
        {
            // Fetch the shareholder details
            var shareholder = db.Shareholders
                .Where(s => s.ShID == shID)
                .Select(s => new
                {
                    SHName = s.FullNameEng,
                    ShareNum = s.ShareID,
                    Region = s.Region,
                    Zone = s.Zone,
                    City = s.City,
                    Subcity = s.Subcity,
                    Woreda = s.Woreda,
                    Kebele = s.Kebele,
                    HouseNo = s.HouseNo,
                    PhoneNo = s.PhoneNo,
                })
                .FirstOrDefault();

            // If the shareholder is not found, return an empty response
            if (shareholder == null)
            {
                return Json(new { message = "Shareholder not found" }, JsonRequestBehavior.AllowGet);
            }

            // Fetch payments related to the shareholder
            var payments = db.Payments
                .Where(p => p.ShID == shID)
                .Select(p => new
                {
                    SHName = p.Shareholder.FullNameEng,
                    PayID = p.PayID,
                    PaymentMode = p.PaymentMode,
                    Branch = p.Branch,
                    PaidAmount = p.PaidAmount,
                    BlockedAmount = p.BlockedAmount,
                    ReferenceNum = p.ReferenceNum,
                    PaymentDate = p.PaymentDate // Leave as DateTime here
                })
                .ToList();

            // Format the PaymentDate after retrieving the payments
            var formattedPayments = payments.Select(p => new
            {
                p.SHName,
                p.PayID,
                p.PaymentMode,
                p.Branch,
                p.PaidAmount,
                p.BlockedAmount,
                p.ReferenceNum,
                PaymentDate = p.PaymentDate.ToString() // Format here
            }).ToList();

            // Return the shareholder details along with their payments
            return Json(new
            {
                Shareholder = shareholder,
                Payments = formattedPayments
            }, JsonRequestBehavior.AllowGet);
        }



        public ActionResult GetPaymentDetails(IEnumerable<int> payIDs, int shID)
        {
            // Ensure both parameters are used correctly in your logic
            var payments = db.Payments
                             .Where(p => payIDs.Contains(p.PayID) && p.ShID == shID) // Example filter
                             .Select(p => new
                             {
                                 PayID = p.PayID,
                                 PerShareValue = 1000,
                                 PaidAmount = p.PaidAmount,
                                 NoofShares = p.PaidAmount / 1000,
                                 PaymentDate = p.PaymentDate.ToString(),
                                 ReferenceNum = p.ReferenceNum
                             })
                             .ToList();

            return Json(payments, JsonRequestBehavior.AllowGet);
        }




        // GET: Certificates/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Certificate certificate = db.Certificates.Find(id);
            if (certificate == null)
            {
                return HttpNotFound();
            }
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", certificate.ShID);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", certificate.CreatedBy);
            ViewBag.CertAuthorizer = new SelectList(db.Users, "UID", "FullName", certificate.CertAuthorizer);
            return View(certificate);
        }

        // POST: Certificates/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CertID,ShID,PaymentIDs,BeginingSerial,EndingSerial,CertNum,CreatedBy,CreatedDate,DeliveryStatus,DeliveredBy,DeliveryDate,CertAuthorizationStatus,CertGenerationDate,CertAuthorizer,Remark")] Certificate certificate)
        {
            if (ModelState.IsValid)
            {
                db.Entry(certificate).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", certificate.ShID);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", certificate.CreatedBy);
            ViewBag.CertAuthorizer = new SelectList(db.Users, "UID", "FullName", certificate.CertAuthorizer);
            return View(certificate);
        }

        // GET: Certificates/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Certificate certificate = db.Certificates.Find(id);
            if (certificate == null)
            {
                return HttpNotFound();
            }
            return View(certificate);
        }

        // POST: Certificates/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Certificate certificate = db.Certificates.Find(id);
            db.Certificates.Remove(certificate);
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
