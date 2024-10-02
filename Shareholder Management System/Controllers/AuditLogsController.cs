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

        public ActionResult RecordLog()
        {
            var Auditlog = _context.AuditLogs.Include(a => a.User); // Ensure AuditLog has a User navigation property
            return View(Auditlog.ToList());
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
