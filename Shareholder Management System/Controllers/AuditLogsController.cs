using Shareholder_Management_System.Models;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity; 

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

            // Apply filters
            if (!string.IsNullOrEmpty(branch))
            {
                query = query.Where(a => a.PerformerBranch.Contains(branch));
            }
            if (startDate.HasValue)
            {
                query = query.Where(a => a.TransactionDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(a => a.TransactionDate <= endDate.Value);
            }
            if (!string.IsNullOrEmpty(userName))
            {
                query = query.Where(a => a.User.FullName.Contains(userName));
            }
            if (!string.IsNullOrEmpty(tableName))
            {
                query = query.Where(a => a.TableName.Contains(tableName));
            }

            // Return the filtered list to the view
            return View(query.ToList());
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
            switch (actionType.ToLower())
            {
                case "register":
                    return $"{tableName} successfully registered.";
                case "update":
                    return $"{tableName} successfully updated.";
                case "delete":
                    return $"{tableName} successfully deleted.";
                case "approve":
                    return $"{tableName} successfully approved.";
                default:
                    return $"{actionType} action performed.";
            }
        }
    }
}
