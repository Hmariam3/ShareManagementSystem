using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml;
using Shareholder_Management_System.Models;
using Shareholder_Management_System.ViewModel;

namespace Shareholder_Management_System.Controllers
{
    public class ReportsController : BaseController
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        public ActionResult ShareholderSubscriptions()
        {
            var resultQuery = (from sh in db.Shareholders
                               join sub in db.Subscribtions on sh.ShID equals sub.ShID
                               where sh.AuthorizationStatus == "Approved" && sub.SubAuthorizationStatus == "Approved"
                               select new ShareholderSubscriptionReportViewModel
                               {
                                   ShID = sh.ShID,
                                   ShareID = sh.ShareID,
                                   ShareholderName = sh.FullNameEng,
                                   SubID = sub.SubID,
                                   SubscribedShare = sub.SubNumShares ?? 0,
                                   SubscriptionAmount = sub.SubAmount ?? 0,
                                   PaidSubscription = sub.PaidSubscription ?? 0,
                                   UnpaidSubscription = sub.UnpaidSubscription ?? 0,
                                   PaidAmount = db.Payments
                                                  .Where(p => p.SubID == sub.SubID && p.PaymentAuthorizationStatus == "Approved")
                                                  .Sum(p => (decimal?)p.PaidAmount) ?? 0,
                                   BlockedAmount = db.Payments
                                                  .Where(p => p.SubID == sub.SubID && p.PaymentAuthorizationStatus == "Approved")
                                                  .Sum(p => (decimal?)p.BlockedAmount) ?? 0
                               }).OrderBy(x => x.ShID).ThenBy(x => x.SubID).ToList();

            return View(resultQuery);
        }

        public ActionResult ExportShareholderSubscriptions()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var resultQuery = (from sh in db.Shareholders
                               join sub in db.Subscribtions on sh.ShID equals sub.ShID
                               where sh.AuthorizationStatus == "Approved" && sub.SubAuthorizationStatus == "Approved"
                               select new ShareholderSubscriptionReportViewModel
                               {
                                   ShID = sh.ShID,
                                   ShareID = sh.ShareID,
                                   ShareholderName = sh.FullNameEng,
                                   SubID = sub.SubID,
                                   SubscribedShare = sub.SubNumShares ?? 0,
                                   SubscriptionAmount = sub.SubAmount ?? 0,
                                   PaidSubscription = sub.PaidSubscription ?? 0,
                                   UnpaidSubscription = sub.UnpaidSubscription ?? 0,
                                   PaidAmount = db.Payments
                                                  .Where(p => p.SubID == sub.SubID && p.PaymentAuthorizationStatus == "Approved")
                                                  .Sum(p => (decimal?)p.PaidAmount) ?? 0,
                                   BlockedAmount = db.Payments
                                                  .Where(p => p.SubID == sub.SubID && p.PaymentAuthorizationStatus == "Approved")
                                                  .Sum(p => (decimal?)p.BlockedAmount) ?? 0
                               }).OrderBy(x => x.ShID).ThenBy(x => x.SubID).ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Subscriptions Report");
                
                // Add Headers
                worksheet.Cells[1, 1].Value = "ShID";
                worksheet.Cells[1, 2].Value = "Share ID";
                worksheet.Cells[1, 3].Value = "Shareholder Name";
                worksheet.Cells[1, 4].Value = "Sub ID";
                worksheet.Cells[1, 5].Value = "Subscribed Share";
                worksheet.Cells[1, 6].Value = "Subscription Amount";
                worksheet.Cells[1, 7].Value = "Paid Subscription";
                worksheet.Cells[1, 8].Value = "Unpaid Subscription";
                worksheet.Cells[1, 9].Value = "Paid Amount";
                worksheet.Cells[1, 10].Value = "Blocked Amount";

                worksheet.Cells["A1:J1"].Style.Font.Bold = true;

                // Add Data
                int row = 2;
                decimal totalShare = 0;
                decimal totalSub = 0;
                decimal totalPaidSub = 0;
                decimal totalUnpaidSub = 0;
                decimal totalPaid = 0;
                decimal totalBlocked = 0;

                foreach (var item in resultQuery)
                {
                    worksheet.Cells[row, 1].Value = item.ShID;
                    worksheet.Cells[row, 2].Value = item.ShareID;
                    worksheet.Cells[row, 3].Value = item.ShareholderName;
                    worksheet.Cells[row, 4].Value = item.SubID;
                    worksheet.Cells[row, 5].Value = item.SubscribedShare;
                    worksheet.Cells[row, 6].Value = item.SubscriptionAmount;
                    worksheet.Cells[row, 7].Value = item.PaidSubscription;
                    worksheet.Cells[row, 8].Value = item.UnpaidSubscription;
                    worksheet.Cells[row, 9].Value = item.PaidAmount;
                    worksheet.Cells[row, 10].Value = item.BlockedAmount;
                    
                    totalShare += item.SubscribedShare;
                    totalSub += item.SubscriptionAmount;
                    totalPaidSub += item.PaidSubscription;
                    totalUnpaidSub += item.UnpaidSubscription;
                    totalPaid += item.PaidAmount;
                    totalBlocked += item.BlockedAmount;
                    
                    row++;
                }

                // Add Total Row
                worksheet.Cells[row, 1].Value = "Total";
                worksheet.Cells[row, 1, row, 4].Merge = true;
                worksheet.Cells[row, 1].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;
                worksheet.Cells[row, 1].Style.Font.Bold = true;
                
                worksheet.Cells[row, 5].Value = totalShare;
                worksheet.Cells[row, 6].Value = totalSub;
                worksheet.Cells[row, 7].Value = totalPaidSub;
                worksheet.Cells[row, 8].Value = totalUnpaidSub;
                worksheet.Cells[row, 9].Value = totalPaid;
                worksheet.Cells[row, 10].Value = totalBlocked;
                worksheet.Cells[row, 5, row, 10].Style.Font.Bold = true;

                // Add Borders to all cells
                var range = worksheet.Cells[1, 1, row, 10];
                range.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                // Format numbers (comma separated, decimals)
                worksheet.Cells[2, 5, row, 5].Style.Numberformat.Format = "#,##0"; // Subscribed Share (Integer)
                worksheet.Cells[2, 6, row, 10].Style.Numberformat.Format = "#,##0.00"; // Amounts (Decimals)

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new System.IO.MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                string excelName = $"ShareholderSubscriptions_{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
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
