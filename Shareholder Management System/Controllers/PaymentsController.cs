using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml;           // For ExcelPackage
using OfficeOpenXml.Core;      // For NonCommercialLicense
using OfficeOpenXml.Style;
using OfficeOpenXml.Table;
using Shareholder_Management_System.Models;

namespace Shareholder_Management_System.Controllers
{
    public class PaymentsController : BaseController
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();


        // GET: Payments
        public ActionResult FilterPending()
        {

            var pendingPayments = db.Payments.Where(p => p.PaymentAuthorizationStatus == "Pending").ToList();
            return View(pendingPayments);
        }
        // GET: Payments
        public ActionResult Index(int? shId)
        {
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            ViewBag.SelectedShId = shId?.ToString();

            if (shId.HasValue)
            {
                var selectedShareholder = db.Shareholders
                    .Where(s => s.ShID == shId && s.Status == "Active" && s.AuthorizationStatus == "Approved")
                    .Select(s => new { s.FullNameEng, s.ShareID })
                    .FirstOrDefault();
                if (selectedShareholder != null)
                {
                    ViewBag.SelectedShName = $"{selectedShareholder.FullNameEng} / {selectedShareholder.ShareID}";
                }
            }

            if (!shId.HasValue)
            {
                return View(new List<Payment>());
            }

            var payments = db.Payments
                .Include(p => p.Branch1)
                .Include(p => p.Shareholder)
                .Include(p => p.Shareholder1)
                .Include(p => p.Subscribtion)
                .Include(p => p.User)
                .Include(p => p.User1)
                .Where(p => p.ShID == shId)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();

            return View(payments);
        }

        [HttpGet]
        public JsonResult SearchShareholders(string term, int page = 1)
        {
            const int pageSize = 10;
            var query = db.Shareholders
                .Where(s => s.Status == "Active" && s.AuthorizationStatus == "Approved");

            if (!string.IsNullOrEmpty(term))
            {
                var searchTerm = term.ToLower().Trim();
                query = query.Where(s =>
                    //s.FullNameEng.ToLower().Contains(searchTerm) ||
                    //s.ShareID.ToLower().Contains(searchTerm));
                    s.FullNameEng.ToLower().StartsWith(term.ToLower()) ||
                           s.ShareID.ToLower().StartsWith(term.ToLower()));
            }

            var total = query.Count();
            var shareholders = query
                .OrderBy(s => s.FullNameEng)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    id = s.ShID,
                    text = s.FullNameEng + " ( " + s.ShareID + " ) "
                })
                .ToList();

            return Json(new
            {
                items = shareholders,
                hasMore = total > page * pageSize
            }, JsonRequestBehavior.AllowGet);
        }



        // GET: Payments/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Payment payment = db.Payments.Find(id);
            if (payment == null)
            {
                return HttpNotFound();
            }
            return View(payment);
        }

        // GET: Payments/Create
        public async Task<ActionResult> Create(int? subId)
        {
            // Initialize model
            var model = new Payment();

            // Handle subId for pre-selection
            if (subId.HasValue)
            {
                var subscription = await db.Subscribtions
                    .Include(s => s.Shareholder)
                    .Where(s => s.SubID == subId && s.SubStatus == "Active" && s.SubAuthorizationStatus == "Approved")
                    .Select(s => new
                    {
                        s.SubID,
                        s.ShID,
                        s.UnpaidSubscription,
                        ShareholderName = s.Shareholder.FullNameEng + " / " + s.Shareholder.ShareID
                    })
                    .FirstOrDefaultAsync();

                if (subscription != null)
                {
                    model.SubID = subscription.SubID;
                    model.ShID = subscription.ShID;
                    ViewBag.SelectedShId = subscription.ShID;
                    ViewBag.SelectedShName = subscription.ShareholderName;
                    ViewBag.UnpaidSubscription = subscription.UnpaidSubscription;
                }
            }

            return View(model);
        }

        // GET: Payments/GetSubscriptionsByShareholder
        // GET: Payments/GetSubscriptionsByShareholder
        public JsonResult GetSubscriptionsByShareholder(int shId)
        {
            var subscriptions = db.Subscribtions
                .Where(s => s.ShID == shId &&
                            s.SubAuthorizationStatus == "Approved" &&
                            s.UnpaidSubscription > 0 &&
                            s.PaymentDueDate > DateTime.Now)
                .Select(sub => new
                {
                    sub.SubID,
                    sub.ShID,
                    sub.SubNumShares,
                    sub.SubAmount,
                    sub.PaidSubscription,
                    sub.UnpaidSubscription,
                    sub.SubAuthorizationStatus,
                    sub.SubStatus,
                    HasPendingOrRejected = db.Payments.Any(p => p.SubID == sub.SubID &&
                                                               (p.PaymentAuthorizationStatus == "Pending" ||
                                                                p.PaymentAuthorizationStatus == "Rejected"))
                }).ToList();

            return Json(subscriptions, JsonRequestBehavior.AllowGet);
        }
        //[HttpGet]
        //public JsonResult CheckExistingPayment(int subId)
        //{
        //    Console.WriteLine($"Checking SubID: {subId}"); // Debug
        //    try
        //    {
        //        var hasPendingOrRejected = db.Payments
        //            .Any(p => p.SubID == subId &&
        //                      (p.PaymentAuthorizationStatus == "Pending" || p.PaymentAuthorizationStatus == "Rejected"));
        //        Console.WriteLine($"Result for SubID {subId}: {hasPendingOrRejected}"); // Debug
        //        return Json(new { hasPendingOrRejected }, JsonRequestBehavior.AllowGet);
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Error checking payment: {ex.Message}"); // Debug
        //        return Json(new { hasPendingOrRejected = false }, JsonRequestBehavior.AllowGet);
        //    }
        //}

        // POST: Payments/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        // GET: Payments/DailyTransactions
        public ActionResult DailyTransactions(DateTime? date, int? branchId, bool export = false)
        {
            var selectedDate = date ?? DateTime.Today;

            // Get current user's branch
            int? currentUserBranch = Session["Branch"] != null ? Convert.ToInt32(Session["Branch"]) : (int?)null;

            // Get all branches for filter dropdown
            var branches = db.Branches.OrderBy(b => b.BranchName).ToList();
            ViewBag.Branches = branches;
            ViewBag.SelectedBranch = branchId;
            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");

            // Query payments for the selected date and branch
            var paymentsQuery = db.Payments
                .Include(p => p.Shareholder)
                .Include(p => p.Branch1)
                .Include(p => p.Subscribtion)
                .Include(p => p.User)
                .Where(p => DbFunctions.TruncateTime(p.CreationDate) == selectedDate);

            if (branchId.HasValue && branchId.Value > 0)
            {
                paymentsQuery = paymentsQuery.Where(p => p.Branch == branchId.Value);
            }
            else if (currentUserBranch.HasValue)
            {
                // If no specific branch filter and user has branch restriction, apply it
                paymentsQuery = paymentsQuery.Where(p => p.Branch == currentUserBranch);
            }

            var payments = paymentsQuery.ToList();

            // Compute summary statistics
            ViewBag.TotalPayments = payments.Count;
            ViewBag.TotalAmount = payments.Sum(p => p.PaidAmount ?? 0);
            ViewBag.ApprovedPayments = payments.Count(p => p.PaymentAuthorizationStatus == "Approved");
            ViewBag.PendingPayments = payments.Count(p => p.PaymentAuthorizationStatus == "Pending");
            ViewBag.RejectedPayments = payments.Count(p => p.PaymentAuthorizationStatus == "Rejected");
            ViewBag.ApprovedAmount = payments.Where(p => p.PaymentAuthorizationStatus == "Approved").Sum(p => p.PaidAmount ?? 0);
            ViewBag.PendingAmount = payments.Where(p => p.PaymentAuthorizationStatus == "Pending").Sum(p => p.PaidAmount ?? 0);
            ViewBag.RejectedAmount = payments.Where(p => p.PaymentAuthorizationStatus == "Rejected").Sum(p => p.PaidAmount ?? 0);

            if (export)
            {
                // Export to Excel logic
                var grid = new System.Web.UI.WebControls.GridView();
                var exportList = payments.Select(p => new {
                    FullName = p.Shareholder.FullNameEng,
                    ShareID = p.Shareholder.ShareID,
                    PaymentDate = p.PaymentDate,
                    District = p.Branch1.SubProcess,
                    BranchName = p.Branch1.BranchName,
                    CreationDate = p.CreationDate,
                    PaidAmount = p.PaidAmount,
                    ReferenceNum = p.ReferenceNum,
                    PaymentMode = p.PaymentMode,
                    BlockedAmount = p.BlockedAmount,
                    TransferedAmount = p.TransferAmount,
                    Status = p.PaymentAuthorizationStatus,
                    Remark = p.Remark
                }).ToList();
                grid.DataSource = exportList;
                grid.DataBind();

                // Prepare summary row as HTML
                string summaryHtml = $@"<tr style='font-weight:bold;background:#f0f0f0;'>
                    <td colspan='2'>Totals</td>
                    <td colspan='2'>Total Payments: {payments.Count}</td>
                    <td colspan='2'>Total Amount: {payments.Sum(p => p.PaidAmount ?? 0):N2}</td>
                    <td colspan='2'>Approved: {payments.Count(p => p.PaymentAuthorizationStatus == "Approved")}, {payments.Where(p => p.PaymentAuthorizationStatus == "Approved").Sum(p => p.PaidAmount ?? 0):N2}</td>
                    <td colspan='2'>Pending: {payments.Count(p => p.PaymentAuthorizationStatus == "Pending")}, {payments.Where(p => p.PaymentAuthorizationStatus == "Pending").Sum(p => p.PaidAmount ?? 0):N2}</td>
                    <td colspan='2'>Rejected: {payments.Count(p => p.PaymentAuthorizationStatus == "Rejected")}, {payments.Where(p => p.PaymentAuthorizationStatus == "Rejected").Sum(p => p.PaidAmount ?? 0):N2}</td>
                </tr>";

                Response.ClearContent();
                Response.Buffer = true;
                Response.AddHeader("content-disposition", $"attachment; filename=DailyTransactions_{selectedDate:yyyyMMdd}.xls");
                Response.ContentType = "application/ms-excel";
                Response.Charset = "";
                System.IO.StringWriter sw = new System.IO.StringWriter();
                System.Web.UI.HtmlTextWriter htw = new System.Web.UI.HtmlTextWriter(sw);
                grid.RenderControl(htw);
                // Insert summary row before closing table
                string html = sw.ToString();
                int idx = html.LastIndexOf("</table>", StringComparison.OrdinalIgnoreCase);
                if (idx > 0)
                {
                    html = html.Insert(idx, summaryHtml);
                }
                Response.Output.Write(html);
                Response.Flush();
                Response.End();
                return null;
            }

            return View(payments);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "PayID,ShID,SubID,PaymentMode,Branch,PaidAmount,BlockedAmount,ReferenceNum," +
            "PaymentSlip,PaymentDate,PaymentTransferFrom,CreatedBy,CreationDate,PaymentAuthorizationStatus,PaymentAuthorizer," +
            "AuthorizationDate,Remark")] Payment payment, HttpPostedFileBase uploadedFile, int[] selectedSubscriptions,
            string cashAmount, string cpoAmount, string dividendAmount, string chequeAmount, string AccountAmount, string SourceOfFunds,
            string RTGSamount, string Bonusamount)
        {
            // Check for duplicate ReferenceNum
            var existingPayment = db.Payments.FirstOrDefault(p => p.ReferenceNum == payment.ReferenceNum);
            if (existingPayment != null)
            {
                ModelState.AddModelError("ReferenceNum", "The Reference Number must be unique. This value is already in use.");
            }

            if (ModelState.IsValid)
            {
                // Prepare a string to hold payment details
                var PaymentMode = new List<string>();

                if (!string.IsNullOrEmpty(cashAmount))
                {
                    PaymentMode.Add($"Cash = {cashAmount}");
                }
                if (!string.IsNullOrEmpty(AccountAmount))
                {
                    PaymentMode.Add($"Account = {AccountAmount}");
                }
                if (!string.IsNullOrEmpty(cpoAmount))
                {
                    PaymentMode.Add($"CPO = {cpoAmount}");
                }
                if (!string.IsNullOrEmpty(dividendAmount))
                {
                    PaymentMode.Add($"Dividend = {dividendAmount}");
                }
                if (!string.IsNullOrEmpty(chequeAmount))
                {
                    PaymentMode.Add($"Cheque = {chequeAmount}");
                }
                if (!string.IsNullOrEmpty(RTGSamount))
                {
                    PaymentMode.Add($"RTGS = {RTGSamount}");
                }
                if (!string.IsNullOrEmpty(Bonusamount))
                {
                    PaymentMode.Add($"Bonus = {Bonusamount}");
                }

                // Join all payment details into a single string
                payment.PaymentMode = string.Join(", ", PaymentMode);
                payment.SourceOfFunds = SourceOfFunds;
                int userId = Convert.ToInt32(Session["ID"]);
                int branchId = Convert.ToInt32(Session["Branch"]);

                // Handle document creation
                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {

                    Document document = new Document
                    {

                        DocOwner = "Shareholder",
                        DocType = "Payment Slip",
                        ShID = payment.ShID,
                        CreatedBy = userId,
                        DocAuthorizationStatus = "Pending",
                        CreatedDate = DateTime.Now,
                    };

                    // Instantiate the DocumentsController to save the document
                    DocumentsController documentsController = new DocumentsController();
                    documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);

                    int documentId = documentsController.Create(document, uploadedFile);

                    if (documentId > 0)
                    {

                        payment.CreationDate = DateTime.UtcNow;
                        payment.CreatedBy = userId;
                        payment.PaymentAuthorizationStatus = "Pending";
                        payment.Branch = branchId;
                        payment.PaymentSlip = documentId;


                        // Save the payment
                        db.Payments.Add(payment);
                        db.SaveChanges();

                        //string branchName = db.Branches.FirstOrDefault(b => b.ID == payment.Branch)?.BranchName ?? "Unknown Branch";

                        // Call RecordLog method with null-safe value for CreatedBy
                        AuditLogsController auditLogsController = new AuditLogsController();
                        auditLogsController.RecordLog("Registration", payment.PayID, "Payment", payment.CreatedBy ?? 0, Session["BranchName"].ToString());

                    }
                    else
                    {
                        // Handle the error case where the document was not created successfully
                        ModelState.AddModelError("", "Document could not be created. Please try again.");
                    }
                }
                TempData["SuccessMessage"] = "Payment successfully created!";
                return RedirectToAction("Index");
            }
            var shareholders1 = db.Shareholders.Select(s => new SelectListItem
            {
                Value = s.ShID.ToString(),
                Text = s.FullNameEng
            }).ToList();
            shareholders1.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "Select a Shareholder"
            });

            ViewBag.Shareholders1 = shareholders1;


            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", payment.Branch);
            //ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "ShareID", payment.ShID);

            // Assuming you have a method that retrieves the list of shareholders
            var shareholders = db.Shareholders.Select(sh => new SelectListItem
            {
                Value = sh.ShID.ToString(),
                Text = sh.FullNameEng
            }).ToList();

            ViewBag.ShID = shareholders;

            ViewBag.PaymentTransferFrom = new SelectList(db.Shareholders, "ShID", "ShareID", payment.PaymentTransferFrom);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "SubStatus", payment.SubID);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", payment.CreatedBy);
            ViewBag.PaymentAuthorizer = new SelectList(db.Users, "UID", "FullName", payment.PaymentAuthorizer);


            return View(payment);
        }

        //private static bool _licenseInitialized = false;

        //public PaymentsController()
        //{
        //    if (!_licenseInitialized)
        //    {
        //        ExcelPackage.License = new NonCommercialLicense(); // Set license for EPPlus 8+
        //        _licenseInitialized = true;
        //    }
        //}
        public JsonResult CheckReferenceNum(string referenceNum)
        {
            var isUnique = !db.Payments.Any(p => p.ReferenceNum == referenceNum);
            return Json(new { isUnique }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ExportPaymentsToExcel(FormCollection form)
        {
            // Fetch data from the database
            List<Payment> data = new List<Payment>();
            int? branchId = Session["Branch"] != null ? Convert.ToInt32(Session["Branch"]) : (int?)null;
            string branchName = Session["BranchName"]?.ToString();

            if (form != null && form.Count > 0)
            {
                data = db.Payments
                    .Include(p => p.Shareholder)
                    .Include(p => p.Branch1)
                    .Where(p => (p.PaymentAuthorizationStatus == "Approved") && (branchId == null || p.Branch == branchId))
                    .ToList();
            }
            else
            {
                data = db.Payments
                    .Include(p => p.Shareholder)
                    .Include(p => p.Branch1)
                    .Where(p => (p.PaymentAuthorizationStatus == "Approved") && (branchId == null || p.Branch == branchId))
                    .ToList();
            }

            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Payment Details");

                // Define desired properties (columns) to export, matching the thead
                string[] desiredProperties = {
                "FullNameEng",
                "ShareID",
                "PaymentDate",
                "SubProcess", // Maps to "District"
                "BranchName",
                "CreationDate",
                "PaidAmount",
                "ReferenceNum",
                "PaymentMode",
                "BlockedAmount", // New column
                "TransferedAmount", // New column
                "PaymentAuthorizationStatus",
                "Remark"
            };

                // Add the company logo
                string imagePath = Server.MapPath("~/assets/images/images.png");
                var picture = worksheet.Drawings.AddPicture("Logo", new FileInfo(imagePath));
                picture.SetPosition(0, 2, 0, 0);
                picture.SetSize(130, 75);

                // Header formatting
                ExcelRange headerCell1 = worksheet.Cells["A2:B2"];
                headerCell1.Merge = true;
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
                headerCell6.Style.Font.Bold = true;
                headerCell6.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                headerCell6.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                headerCell6.Style.Font.Size = 16;
                headerCell6.Style.Font.Name = "Calibri";
                headerCell6.Style.Font.Color.SetColor(Color.White);
                headerCell6.Style.Fill.PatternType = ExcelFillStyle.Solid;
                headerCell6.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));

                ExcelRange headerCell = worksheet.Cells["C1:" + worksheet.Cells[1, desiredProperties.Length].Address];
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

                ExcelRange headerCell3 = worksheet.Cells["C2:" + worksheet.Cells[2, desiredProperties.Length].Address];
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

                ExcelRange headerCell4 = worksheet.Cells["C3:" + worksheet.Cells[3, desiredProperties.Length].Address];
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

                // Add column headers with user-friendly names
                int rowIndex = 4;
                int columnIndex = 1;
                string[] headerNames = {
                "Full Name",
                "Share ID",
                "Payment Date",
                "District",
                "Branch Name",
                "Creation Date",
                "Paid Amount",
                "Reference Num",
                "Mode of Payment",
                "Blocked Amount",
                "Transfered Amount",
                "Status",
                "Remark"
            };

                foreach (var headerName in headerNames)
                {
                    worksheet.Cells[rowIndex, columnIndex].Value = headerName;
                    ExcelRange header = worksheet.Cells[rowIndex, columnIndex];
                    header.Style.Font.Bold = true;
                    header.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    header.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    header.Style.Font.Size = 12;
                    header.Style.Font.Name = "Arial";
                    header.Style.Font.Color.SetColor(Color.White);
                    header.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    header.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(79, 129, 189));
                    columnIndex++;
                }

                // Add data rows
                rowIndex++;
                foreach (var payment in data)
                {
                    columnIndex = 1;
                    foreach (var property in desiredProperties)
                    {
                        ExcelRange cell = worksheet.Cells[rowIndex, columnIndex];
                        switch (property)
                        {
                            case "FullNameEng":
                                cell.Value = payment.Shareholder?.FullNameEng ?? "N/A";
                                break;
                            case "ShareID":
                                cell.Value = payment.Shareholder?.ShareID ?? "N/A";
                                break;
                            case "PaymentDate":
                                cell.Value = payment.PaymentDate?.ToString("yyyy-MM-dd") ?? "N/A";
                                break;
                            case "SubProcess":
                                cell.Value = payment.Branch1?.SubProcess ?? "N/A";
                                break;
                            case "BranchName":
                                cell.Value = payment.Branch1?.BranchName ?? "N/A";
                                break;
                            case "CreationDate":
                                cell.Value = payment.CreationDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
                                break;
                            case "PaidAmount":
                                cell.Value = payment.PaidAmount ?? 0;
                                cell.Style.Numberformat.Format = "#,##0.00";
                                break;
                            case "ReferenceNum":
                                cell.Value = payment.ReferenceNum ?? "N/A";
                                break;
                            case "PaymentMode":
                                cell.Value = payment.PaymentMode ?? "N/A";
                                break;
                            case "BlockedAmount":
                                cell.Value = payment.BlockedAmount ?? 0; // Assuming BlockedAmount is a property
                                cell.Style.Numberformat.Format = "#,##0.00";
                                break;
                            case "TransferedAmount":
                                cell.Value = payment.TransferAmount ?? 0; // Assuming TransferedAmount is a property
                                cell.Style.Numberformat.Format = "#,##0.00";
                                break;
                            case "PaymentAuthorizationStatus":
                                cell.Value = payment.PaymentAuthorizationStatus ?? "N/A";
                                break;
                            case "Remark":
                                cell.Value = payment.Remark ?? "N/A";
                                break;
                        }
                        worksheet.Cells[rowIndex, columnIndex].Style.WrapText = true;
                        columnIndex++;
                    }
                    worksheet.Row(rowIndex).Height = 60;
                    rowIndex++;
                }

                // Add table style
                ExcelTable table = worksheet.Tables.Add(worksheet.Cells["A4:" + worksheet.Cells[rowIndex - 1, desiredProperties.Length].Address], "PaymentTable");
                table.TableStyle = OfficeOpenXml.Table.TableStyles.Light6;

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Return the Excel file
                byte[] fileBytes = package.GetAsByteArray();
                string fileName = "PaymentDetails.xlsx";
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }


        //GET: Payments/Edit/5 

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Find the payment record with Shareholder included
            Payment payment = db.Payments
                .Include(p => p.Shareholder)
                .FirstOrDefault(p => p.PayID == id);
            if (payment == null)
            {
                return HttpNotFound();
            }

            // Retrieve Subscriptions related to the Shareholder for the table
            var subscriptions = db.Subscribtions // Fixed typo: Subscribtions -> Subscriptions
                .Where(s => s.ShID == payment.ShID
                    && s.SubAuthorizationStatus == "Approved"
                    && s.UnpaidSubscription > 0
                    && s.PaymentDueDate > DateTime.Now)
                .ToList();

            // Retrieve the UnpaidSubscription for the pre-selected subscription
            var selectedSubscription = db.Subscribtions // Fixed typo
                .FirstOrDefault(s => s.SubID == payment.SubID);
            decimal unpaidSubscription = selectedSubscription?.UnpaidSubscription ?? 0;

            // Pass the selected shareholder's name
            ViewBag.ShareholderName = payment.Shareholder?.FullNameEng ?? "Unknown";

            // Parse PaymentMode
            var paymentModeDict = payment.PaymentMode?
                .Split(',')
                .Select(x => x.Split('='))
                .ToDictionary(x => x[0].Trim().ToLower(), x => Convert.ToDecimal(x[1])) ?? new Dictionary<string, decimal>();

            // Fetch document details
            if (payment.PaymentSlip.HasValue)
            {
                var document = db.Documents.Find(payment.PaymentSlip);
                if (document != null)
                {
                    ViewBag.ExistingDocumentName = document.DocName;
                    ViewBag.DocumentId = document.DocID;
                }
            }

            // Pass parsed payment modes (fixed typos in keys and assignments)
            ViewBag.CashAmount = paymentModeDict.ContainsKey("cash") ? (decimal?)paymentModeDict["cash"] : null;
            ViewBag.AccountAmount = paymentModeDict.ContainsKey("account") ? (decimal?)paymentModeDict["account"] : null;
            ViewBag.CpoAmount = paymentModeDict.ContainsKey("cpo") ? (decimal?)paymentModeDict["cpo"] : null;
            ViewBag.DividendAmount = paymentModeDict.ContainsKey("dividend") ? (decimal?)paymentModeDict["dividend"] : null;
            ViewBag.ChequeAmount = paymentModeDict.ContainsKey("cheque") ? (decimal?)paymentModeDict["cheque"] : null;
            ViewBag.RTGSamount = paymentModeDict.ContainsKey("rtgs") ? (decimal?)paymentModeDict["rtgs"] : null; // Fixed key and variable
            ViewBag.Bonusamount = paymentModeDict.ContainsKey("bonus") ? (decimal?)paymentModeDict["bonus"] : null; // Fixed key and variable

            // Pass the necessary data to the view model
            var viewModel = new Payment
            {
                PayID = payment.PayID,
                SubID = payment.SubID,
                ReferenceNum = payment.ReferenceNum,
                Remark = payment.Remark,
                PaidAmount = payment.PaidAmount,
                SourceOfFunds = payment.SourceOfFunds,
                PaymentDate = payment.PaymentDate,
                ShID = payment.ShID,
            };

            ViewBag.Subscriptions = subscriptions;
            ViewBag.UnpaidSubscription = unpaidSubscription;

            return View(viewModel);
        }


        // POST: Payments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, [Bind(Include = "PayID,ShID,SubID,PaymentMode,PaidAmount,BlockedAmount,ReferenceNum,PaymentSlip,PaymentDate,PaymentTransferFrom,CreatedBy,CreationDate,PaymentAuthorizationStatus,PaymentAuthorizer,AuthorizationDate,Remark")] Payment payment, HttpPostedFileBase uploadedFile, int[] selectedSubscriptions,
     string cashAmount, string cpoAmount, string dividendAmount, string chequeAmount, string accountAmount, string SourceOfFunds,
     string RTGSamount, string Bonusamount)
        {
            var existingPayment = db.Payments.Find(id);
            if (existingPayment == null)
            {
                return HttpNotFound();
            }

            // Check for duplicate ReferenceNum, excluding the current payment
            var duplicatePayment = db.Payments.FirstOrDefault(p => p.ReferenceNum == payment.ReferenceNum && p.PayID != id);
            if (duplicatePayment != null)
            {
                ModelState.AddModelError("ReferenceNum", "The Reference Number must be unique. This value is already in use.");
            }

            if (ModelState.IsValid)
            {
                // Update payment properties
                existingPayment.ShID = payment.ShID;
                existingPayment.SubID = payment.SubID;
                existingPayment.PaidAmount = payment.PaidAmount;
                existingPayment.SourceOfFunds = payment.SourceOfFunds;
                existingPayment.BlockedAmount = payment.BlockedAmount;
                existingPayment.ReferenceNum = payment.ReferenceNum;
                existingPayment.PaymentDate = payment.PaymentDate;
                existingPayment.PaymentTransferFrom = payment.PaymentTransferFrom;
                existingPayment.Remark = payment.Remark;

                // Handle payment modes
                var paymentModes = new List<string>();
                if (!string.IsNullOrEmpty(cashAmount)) paymentModes.Add($"cash={cashAmount}");
                if (!string.IsNullOrEmpty(accountAmount)) paymentModes.Add($"account={accountAmount}");
                if (!string.IsNullOrEmpty(cpoAmount)) paymentModes.Add($"cpo={cpoAmount}");
                if (!string.IsNullOrEmpty(dividendAmount)) paymentModes.Add($"dividend={dividendAmount}");
                if (!string.IsNullOrEmpty(chequeAmount)) paymentModes.Add($"cheque={chequeAmount}");
                if (!string.IsNullOrEmpty(RTGSamount)) paymentModes.Add($"RTGS={RTGSamount}");
                if (!string.IsNullOrEmpty(Bonusamount)) paymentModes.Add($"Bonus={Bonusamount}");

                existingPayment.PaymentMode = string.Join(",", paymentModes);
                existingPayment.SourceOfFunds = SourceOfFunds;

                int userId = Convert.ToInt32(Session["ID"]);
                existingPayment.CreatedBy = userId;
                existingPayment.Branch = Convert.ToInt32(Session["Branch"]);
                existingPayment.CreationDate = DateTime.Now;
                existingPayment.PaymentAuthorizationStatus = "Pending";

                if (uploadedFile != null && uploadedFile.ContentLength > 0)
                {
                    Document document = new Document
                    {
                        DocOwner = "Shareholder",
                        DocType = "Payment Slip",
                        ShID = existingPayment.ShID,
                        CreatedBy = userId,
                        DocAuthorizationStatus = "Pending",
                        CreatedDate = DateTime.Now
                    };

                    DocumentsController documentsController = new DocumentsController();
                    documentsController.ControllerContext = new ControllerContext(this.Request.RequestContext, documentsController);
                    int documentId = documentsController.Create(document, uploadedFile);
                    if (documentId > 0)
                    {
                        existingPayment.PaymentSlip = documentId;
                    }
                    else
                    {
                        ModelState.AddModelError("", "Document could not be created. Please try again.");
                    }
                }

                db.Entry(existingPayment).State = EntityState.Modified;
                db.SaveChanges();

                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("Edit", existingPayment.PayID, "Payment", existingPayment.CreatedBy ?? 0, Session["BranchName"].ToString());

                return RedirectToAction("Index");
            }

            // Populate ViewBag for dropdowns if validation fails
            var shareholders1 = db.Shareholders.Select(s => new SelectListItem
            {
                Value = s.ShID.ToString(),
                Text = s.FullNameEng
            }).ToList();
            shareholders1.Insert(0, new SelectListItem { Value = "", Text = "Select a Shareholder" });

            ViewBag.Shareholders1 = shareholders1;
            ViewBag.Branch = new SelectList(db.Branches, "ID", "BranchCode", payment.Branch);
            ViewBag.ShID = new SelectList(db.Shareholders, "ShID", "FullNameEng", payment.ShID);
            ViewBag.PaymentTransferFrom = new SelectList(db.Shareholders, "ShID", "FullNameEng", payment.PaymentTransferFrom);
            ViewBag.SubID = new SelectList(db.Subscribtions, "SubID", "UnpaidSubscription", payment.SubID);
            ViewBag.CreatedBy = new SelectList(db.Users, "UID", "FullName", payment.CreatedBy);
            ViewBag.PaymentAuthorizer = new SelectList(db.Users, "UID", "FullName", payment.PaymentAuthorizer);

            return View(payment);
        }


        // GET: Payments/Delete/5
        //public ActionResult Delete(int? id)
        //{
        //    if (id == null)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //    Payment payment = db.Payments.Find(id);
        //    if (payment == null)
        //    {
        //        return HttpNotFound();
        //    }
        //    return View(payment);
        //}

        // POST: Payments/Delete/5
        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                // Fetch the payment record
                Payment payment = db.Payments.Find(id);
                if (payment == null)
                {
                    return Json(new { success = false, message = "Payment not found." });
                }

                // Check conditions for approved payments
                if (payment.PaymentAuthorizationStatus == "Approved")
                {
                    // Check for transfer-related fields
                    if (payment.TransferedPayID != null)
                    {
                        return Json(new { success = false, message = "Cannot delete an approved payment with a transferred payment ID." });
                    }
                    if (payment.TransferID != null)
                    {
                        return Json(new { success = false, message = "Cannot delete an approved payment with a transfer ID." });
                    }
                    if (payment.TransferAmount != null && payment.TransferAmount != 0)
                    {
                        return Json(new { success = false, message = "Cannot delete an approved payment with a transfer amount." });
                    }
                    if (payment.BlockedAmount != null && payment.BlockedAmount != 0)
                    {
                        return Json(new { success = false, message = "Cannot delete an approved payment with a blocked amount." });
                    }

                    // Check if the payment is referenced in the Certificate table's PaymentIDs
                    var certificateWithPayment = db.Certificates
                        .FirstOrDefault(c => c.PaymentIDs != null && c.PaymentIDs.Contains(payment.PayID.ToString()));
                    if (certificateWithPayment != null)
                    {
                        return Json(new { success = false, message = "Cannot delete a payment referenced in a certificate." });
                    }

                    // Reverse the changes made during approval
                    var subscription = db.Subscribtions.Find(payment.SubID);
                    if (subscription != null)
                    {
                        // Revert PaidSubscription and UnpaidSubscription
                        subscription.PaidSubscription -= payment.PaidAmount;
                        subscription.UnpaidSubscription += payment.PaidAmount;

                        // Update SubStatus based on the new values
                        if (subscription.UnpaidSubscription <= 0.00m)
                        {
                            subscription.UnpaidSubscription = 0.00m;
                            subscription.SubStatus = "Fully Paid";
                        }
                        else if (subscription.PaidSubscription > 0.00m && subscription.UnpaidSubscription > 0.00m)
                        {
                            subscription.SubStatus = "Partial Paid";
                        }
                        else if (subscription.PaidSubscription <= 0.00m)
                        {
                            subscription.SubStatus = "Unpaid";
                        }

                        db.Entry(subscription).State = EntityState.Modified;
                    }
                }
                // Delete associated document if it exists
                if (payment.PaymentSlip.HasValue)
                {
                    var document = db.Documents.Find(payment.PaymentSlip);
                    if (document != null)
                    {
                        db.Documents.Remove(document);
                    }
                }



                // Log the deletion action
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("Delete", payment.PayID, "Payment", payment.CreatedBy ?? 0, Session["BranchName"]?.ToString() ?? "Unknown");

                // Remove the payment record
                db.Payments.Remove(payment);
                db.SaveChanges();


                // Return success
                return Json(new { success = true, message = "Payment deleted successfully." });
            }
            catch (Exception ex)
            {
                var baseException = ex.GetBaseException();
                return Json(new
                {
                    success = false,
                    message = "An error occurred: " + baseException.Message
                });
            }


        }



        public ActionResult Authorize(int? id, string action)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Payment paymets = db.Payments.Find(id);
            if (paymets == null)
            {
                return HttpNotFound();
            }
            return View(paymets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Authorize(int id, string action)
        {
            Payment payment = db.Payments.Find(id);
            if (payment == null)
            {
                return HttpNotFound();
            }
            var document = db.Documents.Find(payment.PaymentSlip);

            int userId = Convert.ToInt32(Session["ID"]);
            if (action == "approve")
            {
                document.DocAuthorizationDate = DateTime.Now;
                document.DocAuthorizer = userId;
                payment.PaymentAuthorizationStatus = "Approved";
                payment.PaymentAuthorizer = userId;
                payment.AuthorizationDate = DateTime.Now;

                var subscribtion = db.Subscribtions.Find(payment.SubID);
                if (subscribtion != null)
                {
                    if (payment.PaidAmount > subscribtion.UnpaidSubscription)
                    {
                        TempData["Message"] = "Paid Amount cannot be greater than Unpaid Subscription.";
                        return RedirectToAction("Create");
                    }

                    subscribtion.PaidSubscription += payment.PaidAmount;
                    subscribtion.UnpaidSubscription -= payment.PaidAmount;

                    if (subscribtion.UnpaidSubscription <= 0.00m)
                    {
                        subscribtion.UnpaidSubscription = 0.00m;
                        subscribtion.SubStatus = "Fully Paid";
                    }
                    else if (subscribtion.PaidSubscription > 0.00m && subscribtion.UnpaidSubscription > 0.00m)
                    {
                        subscribtion.SubStatus = "Partial Paid";
                    }

                    db.Entry(subscribtion).State = EntityState.Modified;
                }

                // Record approval log
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("Approval", payment.PayID, "Payment", payment.CreatedBy ?? 0, Session["BranchName"]?.ToString());

                // Set success message
                TempData["Message"] = "Payment approved successfully.";
            }
            else if (action == "reject")
            {
                payment.PaymentAuthorizationStatus = "Rejected";
                payment.PaymentAuthorizer = userId;
                payment.AuthorizationDate = DateTime.Now;

                db.Entry(payment).State = EntityState.Modified;
                db.SaveChanges();

                // Record rejection log
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("Rejection", payment.PayID, "Payment", payment.CreatedBy ?? 0, Session["BranchName"]?.ToString());

                // Set success message
                TempData["Message"] = "Payment rejected successfully.";
            }

            // Save changes
            db.SaveChanges();

            // Redirect to FilterPending view to ensure data is refreshed
            return RedirectToAction("FilterPending");
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
