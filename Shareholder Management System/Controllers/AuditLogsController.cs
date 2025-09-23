using Shareholder_Management_System.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System.Collections.Generic; // Added for List<AuditLog>

namespace Shareholder_Management_System.Controllers
{
    public class AuditLogsController : Controller
    {
        private readonly Shareholder_Management_SystemEntities1 _context;

        public AuditLogsController()
        {
            _context = new Shareholder_Management_SystemEntities1();
        }

        //public ActionResult RecordLog()
        //{
        //    var Auditlog = _context.AuditLogs.Include(a => a.User); // Ensure AuditLog has a User navigation property
        //    return View(auditLogs);
        //}

        public ActionResult RecordLog(string branch, DateTime? startDate, DateTime? endDate, string userName, string tableName)
        {
            var query = _context.AuditLogs.Include(a => a.User).AsQueryable();

            // Always populate dropdowns with all distinct values, capped at 1000 for performance
            ViewBag.Branches = _context.AuditLogs.Select(a => a.PerformerBranch).Distinct().OrderBy(b => b).Take(1000).ToList();
            ViewBag.Users = _context.Users.Select(u => u.FullName).Distinct().OrderBy(u => u).Take(1000).ToList();

            bool hasFilter = !string.IsNullOrEmpty(branch) || startDate.HasValue || endDate.HasValue || !string.IsNullOrEmpty(userName) || !string.IsNullOrEmpty(tableName);

            if (hasFilter)
            {
                if (!string.IsNullOrEmpty(branch))
                {
                    query = query.Where(a => a.PerformerBranch == branch);
                }
                if (startDate.HasValue)
                {
                    query = query.Where(a => a.TransactionDate >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    // Move AddDays(1) out of the query to avoid LINQ to Entities error
                    var endDatePlusOne = endDate.Value.AddDays(1);
                    query = query.Where(a => a.TransactionDate < endDatePlusOne);
                }
                if (!string.IsNullOrEmpty(userName))
                {
                    query = query.Where(a => a.User.FullName == userName);
                }
                if (!string.IsNullOrEmpty(tableName))
                {
                    query = query.Where(a => a.TableName.Contains(tableName));
                }
                return View(query.ToList());
            }
            else
            {
                // No filter: return empty list
                return View(new List<AuditLog>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RecordLog(string actionType, int transactionId, string tableName, int performedBy, string performerBranch)
        {
            string message = GenerateMessage(actionType, tableName);

            var auditLog = new AuditLog
            {
                Message = message,
                ActionType = actionType,
                TransactionID = transactionId,
                TableName = tableName,
                PerformedBy = performedBy,
                PerformerBranch = performerBranch,
                TransactionDate = DateTime.Now
            };

            _context.AuditLogs.Add(auditLog);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        private string GenerateMessage(string actionType, string tableName)
        {
            // Normalize actionType for case-insensitive matching
            var action = (actionType ?? "").Trim().ToLower();
            switch (action)
            {
                case "updating overdue":
                    return $"{tableName} Overdue subs Updated.";
                case "register":
                    return $"{tableName} successfully registered.";
                case "update":
                    return $"{tableName} successfully updated.";
                case "delete":
                    return $"{tableName} successfully deleted.";
                case "approve":
                    return $"{tableName} successfully approved.";
                case "rejection":
                case "reject":
                    return $"{tableName} was rejected.";
                case "approval":
                    return $"{tableName} was approved.";
                default:
                    return $"{actionType} action performed.";
            }
        }
    }
}