﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Shareholder_Management_System.Models;

namespace Shareholder_Management_System.Controllers
{
    public class SimulationDividenedsController : Controller
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        // GET: SimulationDivideneds
        public ActionResult Index()
        {
            var simulationDivideneds = db.SimulationDivideneds.Include(s => s.Branch1).Include(s => s.Dividend).Include(s => s.Payment1).Include(s => s.Shareholder).Include(s => s.Subscribtion).Include(s => s.User);
            return View(simulationDivideneds.ToList());
        }

        // GET: SimulationDivideneds/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SimulationDividened simulationDividened = db.SimulationDivideneds.Find(id);
            if (simulationDividened == null)
            {
                return HttpNotFound();
            }
            return View(simulationDividened);
        }

        // GET: SimulationDivideneds/Create
        public ActionResult Create()
        {
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode");
            ViewBag.DivID = new SelectList(db.Dividends, "DivID", "FiscalYear");
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode");
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID");
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus");
            ViewBag.RequestHandler = new SelectList(db.Users, "UID", "FullName");
            return View();
        }

        // POST: SimulationDivideneds/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "SimID,DivID,ShID,PayID,SubID,OutstandingDays,WASA,DividenedAmount,TaxedDividened,Payment,Capitalization,PaymentStatus,RequestHandler,Branch,DateApplication,SettlementDate,DividenedYear")] SimulationDividened simulationDividened)
        {
            if (ModelState.IsValid)
            {
                db.SimulationDivideneds.Add(simulationDividened);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", simulationDividened.Branch);
            ViewBag.DivID = new SelectList(db.Dividends, "DivID", "FiscalYear", simulationDividened.DivID);
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode", simulationDividened.PayID);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", simulationDividened.ShID);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus", simulationDividened.SubID);
            ViewBag.RequestHandler = new SelectList(db.Users, "UID", "FullName", simulationDividened.RequestHandler);
            return View(simulationDividened);
        }

        // GET: SimulationDivideneds/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SimulationDividened simulationDividened = db.SimulationDivideneds.Find(id);
            if (simulationDividened == null)
            {
                return HttpNotFound();
            }
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", simulationDividened.Branch);
            ViewBag.DivID = new SelectList(db.Dividends, "DivID", "FiscalYear", simulationDividened.DivID);
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode", simulationDividened.PayID);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", simulationDividened.ShID);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus", simulationDividened.SubID);
            ViewBag.RequestHandler = new SelectList(db.Users, "UID", "FullName", simulationDividened.RequestHandler);
            return View(simulationDividened);
        }

        // POST: SimulationDivideneds/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "SimID,DivID,ShID,PayID,SubID,OutstandingDays,WASA,DividenedAmount,TaxedDividened,Payment,Capitalization,PaymentStatus,RequestHandler,Branch,DateApplication,SettlementDate,DividenedYear")] SimulationDividened simulationDividened)
        {
            if (ModelState.IsValid)
            {
                db.Entry(simulationDividened).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", simulationDividened.Branch);
            ViewBag.DivID = new SelectList(db.Dividends, "DivID", "FiscalYear", simulationDividened.DivID);
            ViewBag.PayID = new SelectList(db.Payments, "PayID", "PaymentMode", simulationDividened.PayID);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", simulationDividened.ShID);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus", simulationDividened.SubID);
            ViewBag.RequestHandler = new SelectList(db.Users, "UID", "FullName", simulationDividened.RequestHandler);
            return View(simulationDividened);
        }

        // GET: SimulationDivideneds/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SimulationDividened simulationDividened = db.SimulationDivideneds.Find(id);
            if (simulationDividened == null)
            {
                return HttpNotFound();
            }
            return View(simulationDividened);
        }

        // POST: SimulationDivideneds/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            SimulationDividened simulationDividened = db.SimulationDivideneds.Find(id);
            db.SimulationDivideneds.Remove(simulationDividened);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult ComputeWASA(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            DateTime divEndDate = new DateTime(DateTime.Now.Year - 1, 6, 30);
            Dividend currentDividened = db.Dividends.Find(id);

            if (currentDividened == null)
            {
                return HttpNotFound("Dividend not found.");
            }

            int totalOutsandingDays = currentDividened.NumOutstandingDays ?? 0;
            decimal totalWASA = 0;
            List<SimulationDividened> simulationDivideneds = new List<SimulationDividened>();

            IEnumerable<Payment> payments = db.Payments
                             .Where(p => p.PaymentAuthorizationStatus == "Approved")
                             .Include(s => s.Branch1)
                             .Include(s => s.Payment1)
                             .Include(s => s.Shareholder)
                             .Include(s => s.Subscribtion)
                             .Include(s => s.User)
                             .OrderBy(p => p.PayID)
                             .ToList();

            foreach (var payment in payments)
            {
                if (payment.PaidAmount == 0)
                {
                    continue;
                }

                DateTime? paymentDate = payment.PaymentDate;

                if (string.IsNullOrEmpty(payment.TransferID?.ToString()))
                {
                    int outStandingDays = ComputeOutstandingDays(paymentDate, divEndDate, totalOutsandingDays);
                    totalWASA += AddSimulation(payment, id.Value, outStandingDays, totalOutsandingDays, simulationDivideneds);
                }
                else if (payment.ShareTransfer != null)
                {
                    HandleTransfer(payment, id.Value, divEndDate, totalOutsandingDays, simulationDivideneds, ref totalWASA);
                }
            }

            db.SimulationDivideneds.AddRange(simulationDivideneds);
            db.SaveChanges();

            decimal? perShare = currentDividened.Profit / totalWASA;


            currentDividened.TotalWASA = totalWASA;
            currentDividened.DivPerShare = perShare;
            currentDividened.RunningDate = DateTime.Now;
            db.Entry(currentDividened).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private int ComputeOutstandingDays(DateTime? paymentDate, DateTime divEndDate, int totalOutsandingDays)
        {
            if (!paymentDate.HasValue) return 0;
            int outStandingDays = (divEndDate - paymentDate.Value).Days;
            return Math.Min(outStandingDays, totalOutsandingDays);
        }
        private int ComputeOutstandingDaysBoth(DateTime? paymentDate, DateTime divEndDate, int totalOutsandingDays)
        {
            if (!paymentDate.HasValue) return 0;
            int outStandingDays = (divEndDate - paymentDate.Value).Days;
            DateTime fiscalYearStart = new DateTime(DateTime.Now.Year - 2, 7, 1);

            if (outStandingDays >= 365)
            {
                outStandingDays = (divEndDate - fiscalYearStart).Days;
            }
            return Math.Min(outStandingDays, totalOutsandingDays);
        }

        private decimal AddSimulation(
            Payment payment,
            int divId,
            int outStandingDays,
            int totalOutsandingDays,
            List<SimulationDividened> simulationDivideneds,
            String type = "")
        {
            decimal wasa = (outStandingDays * (payment.PaidAmount ?? 0)) / totalOutsandingDays;
            int shid = payment.ShID ?? 0;

            if (!String.IsNullOrEmpty(type) && (type.Equals("Transferor") || type.Equals("Both")))
            {
                wasa = (outStandingDays * (payment.TransferAmount ?? 0)) / totalOutsandingDays;
                shid = payment.ShareTransfer.TransferrorShID ?? 0;
            }
            simulationDivideneds.Add(new SimulationDividened
            {
                DivID = divId,
                ShID = shid,
                PayID = payment.PayID,
                SubID = payment.SubID,
                OutstandingDays = outStandingDays,
                WASA = wasa,
                PaymentStatus = "Pending",
                DividenedYear = DateTime.Now.Year.ToString()
            });

            return wasa;
        }

        private void HandleTransfer(
            Payment payment,
            int divId,
            DateTime divEndDate,
            int totalOutsandingDays,
            List<SimulationDividened> simulationDivideneds,
            ref decimal totalWASA)
        {
            string dividenedFor = payment.ShareTransfer.DividenedFor;

            if (dividenedFor == "Both")
            {
                // Seller
                DateTime? paymentDate = payment.Payment2?.PaymentDate;
                DateTime? transferDate = payment.ShareTransfer.TransferDate;

                if (paymentDate.HasValue && transferDate.HasValue)
                {
                    int outStandingDaysTransferor = ComputeOutstandingDaysBoth(paymentDate, transferDate.Value, totalOutsandingDays);
                    totalWASA += AddSimulation(payment, divId, outStandingDaysTransferor, totalOutsandingDays, simulationDivideneds, "Both");

                    // Buyer
                    int outStandingDaysTransferee = ComputeOutstandingDays(transferDate, divEndDate, totalOutsandingDays);
                    totalWASA += AddSimulation(payment, divId, outStandingDaysTransferee, totalOutsandingDays, simulationDivideneds);
                }
            }
            else if (dividenedFor == "Transferor")
            {
                DateTime? paymentDate = payment.Payment2?.PaymentDate;
                if (paymentDate.HasValue)
                {
                    int outStandingDays = ComputeOutstandingDays(paymentDate, divEndDate, totalOutsandingDays);
                    totalWASA += AddSimulation(payment, divId, outStandingDays, totalOutsandingDays, simulationDivideneds, "Transferor");
                }
            }
            else if (dividenedFor == "Transferee")
            {
                DateTime? paymentDate = payment.Payment2?.PaymentDate;
                if (paymentDate.HasValue)
                {
                    int outStandingDays = ComputeOutstandingDays(paymentDate, divEndDate, totalOutsandingDays);
                    totalWASA += AddSimulation(payment, divId, outStandingDays, totalOutsandingDays, simulationDivideneds);
                }
            }
        }

        public ActionResult ExportToExcel()
        {
            // Fetch data from the model (replace this with your actual data fetching logic)
            IEnumerable<SimulationDividened> data = db.SimulationDivideneds
                .Include(s => s.Shareholder)
                .Include(s => s.Payment1)
                .Include(s => s.Dividend)
                .ToList();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Dividend Report");

                // Define the desired properties for the header
                string[] headers = {
                                    "Shareholder ID", "Shareholder Name", "Category", "Payment Amount",
                                    "Payment Mode", "Payment Date", "Fiscal Year", "Outstanding Days",
                                    "WASA", "Rate", "Dividend Payable", "Transferror", "Transferee",
                                    "Dividend Agreement", "Transfer Date"
                                };

                ExcelRange headerCell1 = worksheet.Cells["A2:B2"];
                headerCell1.Merge = true;
                string imagePath = Server.MapPath("~/assets/images/coop_logo.png");

                // Add the company logo
                var picture = worksheet.Drawings.AddPicture("Logo", new FileInfo(imagePath));

                // Set the position of the image to span across columns A to D and start from row 1
                picture.SetPosition(0, 2, 0, 0);

                picture.SetSize(170, 80);
                //headerCell1.Value = "~/dist/img/coop1.gif";
                headerCell1.Style.Font.Bold = true;
                headerCell1.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell1.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell1.Style.Font.Size = 16;
                headerCell1.Style.Font.Name = "Calibri";
                headerCell1.Style.Font.Color.SetColor(Color.White);
                headerCell1.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell1.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                ExcelRange headerCell5 = worksheet.Cells["A1:B1"];
                headerCell5.Merge = true;
                //headerCell5.Value = "Baankii Hojii Gamtaa Oromiyaa";
                headerCell5.Style.Font.Bold = true;
                headerCell5.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell5.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell5.Style.Font.Size = 16;
                headerCell5.Style.Font.Name = "Calibri";
                headerCell5.Style.Font.Color.SetColor(Color.White);
                headerCell5.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell5.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));


                ExcelRange headerCell6 = worksheet.Cells["A3:B3"];
                headerCell6.Merge = true;
                //headerCell5.Value = "Baankii Hojii Gamtaa Oromiyaa";
                headerCell6.Style.Font.Bold = true;
                headerCell6.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell6.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell6.Style.Font.Size = 16;
                headerCell6.Style.Font.Name = "Calibri";
                headerCell6.Style.Font.Color.SetColor(Color.White);
                headerCell6.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell6.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                ExcelRange headerCell = worksheet.Cells["C1:" + worksheet.Cells[1, headers.Length].Address];

                headerCell.Merge = true;
                headerCell.Value = "Baankii Hojii Gamtaa Oromiyaa";
                headerCell.Style.Font.Bold = true;
                headerCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell.Style.Font.Size = 16;
                headerCell.Style.Font.Name = "Calibri";
                headerCell.Style.Font.Color.SetColor(Color.White);
                headerCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));


                ExcelRange headerCell3 = worksheet.Cells["C2:" + worksheet.Cells[2, headers.Length].Address];

                headerCell3.Merge = true;
                headerCell3.Value = "Cooperative Bank of Oromia";
                headerCell3.Style.Font.Bold = true;
                headerCell3.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell3.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell3.Style.Font.Size = 16;
                headerCell3.Style.Font.Name = "Calibri";
                headerCell3.Style.Font.Color.SetColor(Color.White);
                headerCell3.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell3.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                ExcelRange headerCell4 = worksheet.Cells["C3:" + worksheet.Cells[3, headers.Length].Address];

                headerCell4.Merge = true;
                headerCell4.Value = "ኦሮሚያ ኅብራት ሥራ ባንክ";
                headerCell4.Style.Font.Bold = true;
                headerCell4.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell4.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell4.Style.Font.Size = 16;
                headerCell4.Style.Font.Name = "Calibri";
                headerCell4.Style.Font.Color.SetColor(Color.White);
                headerCell4.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell4.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                int rowIndex = 4;

                // Add Table Headers
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[rowIndex, i + 1].Value = headers[i];
                    worksheet.Cells[rowIndex, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[rowIndex, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    worksheet.Cells[rowIndex, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[rowIndex, i + 1].Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                }

                // Add Data Rows
                rowIndex++;
                foreach (var item in data)
                {
                    worksheet.Cells[rowIndex, 1].Value = item.Shareholder?.ShareID;
                    worksheet.Cells[rowIndex, 2].Value = item.Shareholder?.FullNameEng;
                    worksheet.Cells[rowIndex, 3].Value = item.Shareholder?.SHCategory;
                    worksheet.Cells[rowIndex, 4].Value = item.Payment1?.PaidAmount;
                    worksheet.Cells[rowIndex, 5].Value = item.Payment1?.PaymentMode;
                    worksheet.Cells[rowIndex, 6].Value = item.Payment1?.PaymentDate?.ToString("yyyy-MM-dd");
                    worksheet.Cells[rowIndex, 7].Value = item.Dividend?.FiscalYear;
                    worksheet.Cells[rowIndex, 8].Value = item.OutstandingDays;
                    worksheet.Cells[rowIndex, 9].Value = item.WASA;

                    // Calculate Rate and Dividend
                    var rate = item.Dividend?.DivPerShare / 100;
                    var dividendPayable = item.WASA * rate;
                    worksheet.Cells[rowIndex, 10].Value = rate;
                    worksheet.Cells[rowIndex, 11].Value = dividendPayable;

                    worksheet.Cells[rowIndex, 12].Value = item.Payment1?.TransferID != null ? item.Payment1.ShareTransfer.Shareholder?.FullNameEng : "";
                    worksheet.Cells[rowIndex, 13].Value = item.Payment1?.TransferID != null ? item.Payment1.ShareTransfer.Shareholder1?.FullNameEng : "";
                    worksheet.Cells[rowIndex, 14].Value = item.Payment1?.TransferID != null ? item.Payment1.ShareTransfer.DividenedFor : "";
                    worksheet.Cells[rowIndex, 15].Value = item.Payment1?.TransferID != null ? item.Payment1.ShareTransfer.TransferDate?.ToString("yyyy-MM-dd") : "";

                    rowIndex++;
                }

                // Format the table
                worksheet.Cells[4, 1, rowIndex - 1, headers.Length].AutoFitColumns();
                var tableRange = worksheet.Cells[4, 1, rowIndex - 1, headers.Length];
                var table = worksheet.Tables.Add(tableRange, "DividendTable");
                table.TableStyle = OfficeOpenXml.Table.TableStyles.Light6;

                // Return the Excel file
                byte[] fileBytes = package.GetAsByteArray();
                string fileName = "DividendReport.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

        public ActionResult ComputeDividend()
        {
            // Fetch all records from SimulationDividened
            var simulationDividends = db.SimulationDivideneds.Include(s => s.Dividend).ToList();

            // Check if there are records to process
            if (!simulationDividends.Any())
            {
                return Json(new { success = false, message = "No simulation dividends found to process." });
            }

            // Get the profit per share from the first record (assuming it's consistent across all records)
            decimal? profitPerShare = simulationDividends.FirstOrDefault()?.Dividend?.DivPerShare;
            profitPerShare = profitPerShare / 100;

            if (profitPerShare == null)
            {
                return Json(new { success = false, message = "Profit per share is not defined." });
            }

            // Create a list to hold DividenedDetail records
            List<DividenedDetail> dividenedDetails = new List<DividenedDetail>();

            // Map each SimulationDividened record to DividenedDetail
            foreach (var simDividend in simulationDividends)
            {
                // Calculate DividenedAmount
                decimal? dividenedAmount = simDividend.WASA * profitPerShare;

                // Create a new DividenedDetail record
                DividenedDetail divDetail = new DividenedDetail
                {
                    DivID = simDividend.DivID,
                    ShID = simDividend.ShID,
                    PayID = simDividend.PayID,
                    SubID = simDividend.SubID,
                    OutstandingDays = simDividend.OutstandingDays,
                    WASA = simDividend.WASA,
                    DividenedAmount = dividenedAmount,
                    TaxedDividened = simDividend.TaxedDividened, // Copy existing values
                    Payment = simDividend.Payment,
                    Capitalization = simDividend.Capitalization,
                    PaymentStatus = simDividend.PaymentStatus,
                    RequestHandler = simDividend.RequestHandler,
                    Branch = simDividend.Branch,
                    DateApplication = simDividend.DateApplication,
                    SettlementDate = simDividend.SettlementDate,
                    DividenedYear = simDividend.DividenedYear
                };

                dividenedDetails.Add(divDetail);
            }

            // Add all mapped records to the DividenedDetail table
            db.DividenedDetails.AddRange(dividenedDetails);

            // Remove all records from the SimulationDividened table
            db.SimulationDivideneds.RemoveRange(simulationDividends);

            // Save changes to the database
            db.SaveChanges();

            // Return a success response
            TempData["Message"] = "Total WASA has been successfully computed and updated.";
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