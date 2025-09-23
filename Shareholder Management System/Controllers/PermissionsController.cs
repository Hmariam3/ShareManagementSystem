using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml;
using Shareholder_Management_System.Models;

namespace Shareholder_Management_System.Controllers
{
    public class PermissionsController : BaseController
    {
        private Shareholder_Management_SystemEntities1 db = new Shareholder_Management_SystemEntities1();

        // GET: Permissions
        public ActionResult Index(string roleName = null)
        {
            var permissions = db.Permissions.AsQueryable();
            if (!string.IsNullOrEmpty(roleName))
            {
                permissions = permissions.Where(p => p.role_name == roleName);
            }
            ViewBag.RoleName = new SelectList(new[] { "All", "Administrator", "Initiator", "Authorizer" }, roleName ?? "All");
            return View(permissions.ToList());
        }

        // POST: Permissions/Filter
        [HttpPost]
        public ActionResult Filter(string roleName)
        {
            var permissions = db.Permissions.AsQueryable();
            if (!string.IsNullOrEmpty(roleName) && roleName != "All")
            {
                permissions = permissions.Where(p => p.role_name == roleName);
            }
            return PartialView("_PermissionsTable", permissions.ToList());
        }

        public ActionResult DownloadTemplate()
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Permissions");
                worksheet.Cells[1, 1].Value = "Permission Name";
                worksheet.Cells[1, 2].Value = "Controller";
                worksheet.Cells[1, 3].Value = "Action";
                worksheet.Cells[2, 1].Value = "View Users";
                worksheet.Cells[2, 2].Value = "Users";
                worksheet.Cells[2, 3].Value = "Index";

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PermissionsTemplate.xlsx");
            }
        }

        // GET: Permissions/Upload
        public ActionResult Upload()
        {
            ViewBag.RoleName = new SelectList(new[] { "Administrator", "Initiator", "Authorizer" }, "Administrator");
            ViewBag.PermFamily = new SelectList(new[] { "User Management", "Transfer", "Subscription", "Payment", "Shareholder", "Block", "Certificate" }, "User Management");
            return View();
        }

        // POST: Permissions/Upload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Upload(string role_name, string perm_family, HttpPostedFileBase file)
        {
            if (string.IsNullOrWhiteSpace(role_name) || string.IsNullOrWhiteSpace(perm_family))
            {
                ModelState.AddModelError("", "Please select a role and permission family.");
            }

            if (file == null || file.ContentLength == 0)
            {
                ModelState.AddModelError("", "Please upload a valid Excel file.");
            }
            else if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "Only .xlsx files are supported.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    using (var package = new ExcelPackage(file.InputStream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            ModelState.AddModelError("", "Excel file is empty or invalid.");
                            ViewBag.RoleName = new SelectList(new[] { "Administrator", "Initiator", "Authorizer" }, role_name);
                            ViewBag.PermFamily = new SelectList(new[] { "User Management", "Transfer", "Subscription", "Payment", "Shareholder", "Block", "Certificate" }, perm_family);
                            return View();
                        }

                        // Expected columns: perm_name, perm_controller, perm_action
                        var permissions = new List<Permission>();
                        int rowCount = worksheet.Dimension?.Rows ?? 0;

                        // Start from row 2 to skip headers
                        for (int row = 2; row <= rowCount; row++)
                        {
                            var perm_name = worksheet.Cells[row, 1].Text?.Trim();
                            var perm_controller = worksheet.Cells[row, 2].Text?.Trim();
                            var perm_action = worksheet.Cells[row, 3].Text?.Trim();

                            // Validate required fields
                            if (string.IsNullOrWhiteSpace(perm_name) || string.IsNullOrWhiteSpace(perm_controller) || string.IsNullOrWhiteSpace(perm_action))
                            {
                                ModelState.AddModelError("", $"Invalid data in row {row}: All fields are required.");
                                continue;
                            }

                            // Check for duplicate in the current batch
                            if (permissions.Any(p =>
                                p.role_name == role_name &&
                                p.perm_family == perm_family &&
                                p.perm_controller == perm_controller &&
                                p.perm_action == perm_action))
                            {
                                ModelState.AddModelError("", $"Duplicate permission in row {row}: {perm_controller}/{perm_action}.");
                                continue;
                            }

                            // Check for duplicate in the database
                            if (db.Permissions.Any(p =>
                                p.role_name == role_name &&
                                p.perm_family == perm_family &&
                                p.perm_controller == perm_controller &&
                                p.perm_action == perm_action))
                            {
                                ModelState.AddModelError("", $"Permission already exists in row {row}: {perm_controller}/{perm_action}.");
                                continue;
                            }

                            permissions.Add(new Permission
                            {
                                role_name = role_name,
                                perm_family = perm_family,
                                perm_name = perm_name,
                                perm_controller = perm_controller,
                                perm_action = perm_action
                            });
                        }

                        if (!ModelState.IsValid)
                        {
                            ViewBag.RoleName = new SelectList(new[] { "Administrator", "Initiator", "Authorizer" }, role_name);
                            ViewBag.PermFamily = new SelectList(new[] { "User Management", "Transfer", "Subscription", "Payment", "Shareholder", "Block", "Certificate" }, perm_family);
                            return View();
                        }

                        // Save permissions to database
                        foreach (var permission in permissions)
                        {
                            db.Permissions.Add(permission);
                            db.SaveChanges();

                            // Log audit
                            int userId = Convert.ToInt32(Session["ID"]);
                            AuditLogsController auditLogsController = new AuditLogsController();
                            auditLogsController.RecordLog("upload permission", permission.perm_id, "Permission", userId, Session["BranchName"].ToString());
                        }

                        TempData["Message"] = $"{permissions.Count} permissions uploaded successfully.";
                        return RedirectToAction("Index");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error processing Excel file: {ex.Message}");
                }
            }

            ViewBag.RoleName = new SelectList(new[] { "Administrator", "Initiator", "Authorizer" }, role_name);
            ViewBag.PermFamily = new SelectList(new[] { "User Management", "Transfer", "Subscription", "Payment", "Shareholder", "Block", "Certificate" }, perm_family);
            return View();
        }

        // Existing actions (e.g., Create, Edit, Delete) remain unchanged
        // GET: Permissions/Create
        public ActionResult Create()
        {
            ViewBag.RoleName = new SelectList(new[] { "Administrator", "Initiator", "Authorizer" }, "Administrator");
            return View();
        }

        // POST: Permissions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "role_name,perm_family,perm_name,perm_controller,perm_action")] Permission permission)
        {
            var existingPermission = db.Permissions.FirstOrDefault(p =>
                p.role_name == permission.role_name &&
                p.perm_family == permission.perm_family &&
                p.perm_controller == permission.perm_controller &&
                p.perm_action == permission.perm_action);

            if (existingPermission != null)
            {
                ModelState.AddModelError("", "A permission with the same role, family, controller, and action already exists.");
            }

            if (ModelState.IsValid)
            {
                db.Permissions.Add(permission);
                db.SaveChanges();

                int userId = Convert.ToInt32(Session["ID"]);
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("create permission", permission.perm_id, "Permission", userId, Session["BranchName"].ToString());

                TempData["Message"] = "Permission created successfully.";
                return RedirectToAction("Index");
            }

            ViewBag.RoleName = new SelectList(new[] { "Administrator", "Initiator", "Authorizer" }, permission.role_name);
            return View(permission);
        }

        // GET: Permissions/Details/5
        public ActionResult Details(int id)
        {
            var permission = db.Permissions.Find(id);
            if (permission == null)
            {
                return HttpNotFound();
            }
            return View(permission);
        }

        // GET: Permissions/Edit/5
        public ActionResult Edit(int id)
        {
            var permission = db.Permissions.Find(id);
            if (permission == null)
            {
                return HttpNotFound();
            }
            ViewBag.RoleName = new SelectList(new[] { "Administrator", "Initiator", "Authorizer" }, permission.role_name);
            return View(permission);
        }

        // POST: Permissions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "perm_id,role_name,perm_family,perm_name,perm_controller,perm_action")] Permission permission)
        {
            var existingPermission = db.Permissions.FirstOrDefault(p =>
                p.role_name == permission.role_name &&
                p.perm_family == permission.perm_family &&
                p.perm_controller == permission.perm_controller &&
                p.perm_action == permission.perm_action &&
                p.perm_id != permission.perm_id);

            if (existingPermission != null)
            {
                ModelState.AddModelError("", "A permission with the same role, family, controller, and action already exists.");
            }

            if (ModelState.IsValid)
            {
                db.Entry(permission).State = EntityState.Modified;
                db.SaveChanges();

                int userId = Convert.ToInt32(Session["ID"]);
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("edit permission", permission.perm_id, "Permission", userId, Session["BranchName"].ToString());

                TempData["Message"] = "Permission updated successfully.";
                return RedirectToAction("Index");
            }

            ViewBag.RoleName = new SelectList(new[] { "Administrator", "Initiator", "Authorizer" }, permission.role_name);
            return View(permission);
        }

        // GET: Permissions/Delete/5
        public ActionResult Delete(int id)
        {
            var permission = db.Permissions.Find(id);
            if (permission == null)
            {
                return HttpNotFound();
            }
            return View(permission);
        }

        // POST: Permissions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var permission = db.Permissions.Find(id);
            if (permission != null)
            {
                db.Permissions.Remove(permission);
                db.SaveChanges();

                int userId = Convert.ToInt32(Session["ID"]);
                AuditLogsController auditLogsController = new AuditLogsController();
                auditLogsController.RecordLog("delete permission", id, "Permission", userId, Session["BranchName"].ToString());

                TempData["Message"] = "Permission deleted successfully.";
            }
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
