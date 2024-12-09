using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
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


        public ActionResult ExportToExcel()
        {
            // Fetch data from the model (replace this with your actual data fetching logic)
            IEnumerable<DividenedDetail> data = db.DividenedDetails
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